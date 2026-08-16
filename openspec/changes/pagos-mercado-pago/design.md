## Context

Ver `proposal.md` § Why para la motivación y `specs/` para los requisitos. Acá solo el estado actual
que condiciona el diseño.

**Lo que ya está resuelto y hay que respetar**:

- `Pedido` es `MultiTenantEntityBase` y su `Total` se congela al confirmar, con
  `PedidoItem.PrecioUnitarioSnapshot` (ADR-07). El precio ya no depende del catálogo vigente al
  momento de pagar — eso blinda gratis la manipulación de precio.
- La sesión de familia es un JWT propio con `sessionType=familia`, `eventoId` y N `participanteId`,
  de 30 minutos (ADR-11). El alcance de datos sale **siempre** de esos claims, nunca de parámetros
  del request — patrón establecido en `FamiliaGaleriaAppService` y `FamiliaPedidoAppService`.
- `AuthorizationEnabled=false` en dev y producción: la matriz de permisos por endpoint no corre. Por
  eso los AppServices admin del vertical llaman `FamiliaSessionGuard.EnsureNoFamiliaSession` como
  defensa en profundidad. Todo endpoint admin nuevo hereda esa obligación.
- El worker de fotos ya establece contexto de tenant fuera de un request HTTP con
  `IAppContext.SetSystemContext` (`FotoProcesamientoWorker`). Ese es el precedente para el webhook.

**La restricción que domina el diseño del webhook**:

```csharp
// AcgFotos.Base.Infrastructure/Data/AcgFotosDbContext.cs:158
// AcgFotos.Core/Infrastructure/CustomDbContext.cs:65
c => c.TenantId == _appContext.TenantId || _appContext.IsAnonymous;
```

El filtro global multi-tenant **se desactiva por completo en requests anónimos**. Es intencional para
login/canje/reset de password, que necesitan buscar antes de saber el tenant. Pero el webhook de
Mercado Pago es necesariamente anónimo, y es el componente que mueve el estado de cobro. Escrito de
la forma natural correría sin aislamiento alguno entre tenants.

Esa expresión **se corrige en el change previo `aislamiento-tenant-anonimo`**, que cierra el default
del filtro y deja el cruce de tenants como declaración explícita por consulta. Este change asume esa
base ya aplicada, pero **no depende de ella para ser seguro**: las defensas de D4 valen igual con la
cláusula presente o ausente. Lo que aporta el orden es una segunda capa independiente — con el filtro
cerrado, un error futuro en el camino del webhook devuelve resultados vacíos en vez de datos de otro
tenant.

## Goals / Non-Goals

**Goals**

- Que el estado de cobro de un pedido solo pueda cambiar por evidencia verificada server-to-server o
  por una acción humana atribuida.
- Que el aislamiento entre tenants en el webhook sea estructural, no una convención que dependa de
  que quien programe se acuerde.
- Que el estado de pago sea auditable: poder responder "por qué este pedido figura pagado" mirando
  datos, no logs.
- Que cada defensa del threat model tenga un test de regresión que falle si alguien la remueve.

**Non-Goals**

- Prender `AuthorizationEnabled=true` ni sembrar el catálogo de permisos de plataforma. Sigue siendo
  un trabajo aparte, ya anotado para Deploy.
- Corregir la cláusula `|| IsAnonymous` del filtro multi-tenant: es el change previo
  `aislamiento-tenant-anonimo`, no se hace acá.
- Conciliación contable, reportes de facturación, o integración con AFIP.
- Reintentos automáticos de cobro, recordatorios de pago pendiente, o expiración de preferencias no
  pagadas.

## Decisions

### D1 — El estado de pago se **deriva** de la bitácora, no se muta suelto

`Pedido.EstadoPago` es un campo persistido, pero su valor es siempre el resultado de plegar las
entradas de `PagoPedido` con una función de precedencia fija:

```
  Reembolsado  ▲  (la devolución es la última palabra sobre un cobro)
  RevisionManual│  (discrepancia detectada: pegajoso y ruidoso a propósito)
  Pagado        │
  AcreditandoMP │
  Iniciado      │
  Rechazado     │  (un rechazo no impide un intento posterior exitoso)
  SinPagar     ▼
```

El campo es una caché denormalizada de ese pliegue, no una variable independiente.

**Por qué**: idempotencia y monotonía dejan de ser código defensivo y pasan a ser propiedades del
modelo. Reprocesar la misma bitácora da el mismo resultado; una notificación tardía agrega una
entrada de menor precedencia y el pliegue sigue devolviendo lo mismo. No hay que razonar sobre
"¿puede este estado retroceder?" en cada camino de código: no puede, por construcción.

**Alternativa descartada**: mutar `EstadoPago` en cada notificación con `if` de transición válida.
Es lo habitual y es donde aparecen los bugs de orden — cada camino nuevo tiene que acordarse de la
regla, y el bug se ve recién con notificaciones fuera de orden, que en dev casi nunca pasan.

`Rechazado` va deliberadamente por debajo de `Iniciado`: un intento rechazado no debe bloquear el
siguiente. `RevisionManual` va alto y no lo baja ninguna notificación posterior — una discrepancia de
monto tiene que quedar visible hasta que un humano la resuelva.

### D2 — Idempotencia por restricción de base, no por chequeo previo

`PagoPedido` lleva un índice único sobre (identificador de pago del proveedor, estado resultante). Una
notificación repetida que intenta escribir la misma transición viola la restricción; el handler
traduce esa violación a "ya procesada" y responde 200.

**Por qué**: el chequeo "¿ya existe?" seguido de "entonces inserto" es una condición de carrera con
dos notificaciones concurrentes — y Mercado Pago reintenta, así que la concurrencia es el caso
esperado, no el raro. La restricción única es el único punto donde la carrera se resuelve de verdad.

### D3 — Referencia de pago opaca, y es la única llave del webhook

`Pedido` gana `ReferenciaPagoPublica`: 32 bytes de un generador criptográficamente seguro,
codificados en URL-safe base64, único, generado al confirmar el pedido. Es lo que viaja como
`external_reference` a Mercado Pago.

**Por qué no el `PedidoId`**: lo ve quien paga. Un id secuencial filtra volumen de negocio (cuántos
pedidos lleva el estudio, a qué ritmo crecen) y convierte cualquier endpoint que acepte esa
referencia en algo enumerable. Cuesta lo mismo hacerlo opaco.

**Formato, con una trampa**: Mercado Pago acepta en `external_reference` hasta 64 caracteres y solo
letras, números, guion y guion bajo. Eso obliga a **base64url sin relleno**: el base64 estándar usa
`+` y `/`, que están prohibidos, y el `=` de relleno también. 32 bytes dan 43 caracteres en base64url,
que entra cómodo. Escrito explícito porque implementarlo con `Convert.ToBase64String` compila,
funciona en los tests y falla contra Mercado Pago.

**Doble función**: es también la capacidad que le permite a la familia consultar el estado de su pago
después de que venció su sesión de 30 minutos (ver D6).

### D4 — El webhook establece contexto de tenant antes de tocar datos, y no puede saltearse

```
   POST /api/fotos/pagos/notificaciones   [AllowAnonymous]
        │
        ├─ ① rechazar si trae cabecera Authorization
        │     (MP nunca manda una; si viene, es confusión de contexto)
        │
        ├─ ② verificar firma HMAC + ventana de frescura
        │     ↓ falla → 200 vacío, sin tocar datos
        │
        ├─ ③ GET /v1/payments/{id} contra MP con credencial propia
        │     ↓ la respuesta de ACÁ es la fuente de verdad
        │
        ├─ ④ resolver external_reference → (TenantId, PedidoId)
        │     ÚNICA consulta cross-tenant. Proyección a dos long.
        │     Físicamente no puede devolver otra cosa.
        │
        ├─ ⑤ appContext.SetSystemContext(tenantId, ...)
        │     IsAnonymous pasa a false → filtro global REACTIVADO
        │
        └─ ⑥ procesar  →  TenantScopeGuard.EnsureTenantScoped(appContext)
                          lanza si IsAnonymous sigue true
```

El paso ⑥ es lo que convierte la convención en garantía: el AppService que escribe la bitácora
**empieza** afirmando que hay contexto de tenant establecido, igual que los AppServices admin
empiezan con `FamiliaSessionGuard.EnsureNoFamiliaSession`. Si alguien refactoriza y pierde el paso
⑤, revienta con una excepción en vez de correr silenciosamente sin aislamiento.

El paso ④ devuelve un `record (long TenantId, long PedidoId)`, no una entidad. Es la diferencia entre
"acordate de no leer de más" y "no hay de dónde leer de más".

El paso ① es barato y cubre un caso real: si un token de familia llegara con la notificación,
`IsAnonymous` sería false y `TenantId` saldría del token — el guard ⑥ pasaría y el contexto sería el
equivocado hasta que ⑤ lo pise. Rechazar de entrada elimina la ventana.

### D5 — La notificación autentica al emisor; la API de Mercado Pago dice qué pasó

Firma y contenido cumplen funciones distintas y se tratan distinto:

| | Qué prueba | Qué NO prueba |
|---|---|---|
| Firma HMAC válida | Que lo mandó quien tiene el secreto | Nada sobre el estado del pago |
| `GET /v1/payments/{id}` | El estado, monto y moneda reales | — |

Por eso el flujo consulta siempre, aun con firma válida. La firma decide *si vale la pena preguntar*;
la consulta decide *qué pasó*.

**Alternativa descartada**: confiar en el cuerpo cuando la firma valida, para ahorrar una llamada. Es
el atajo que convierte una filtración del secreto de webhook —un secreto de bajo valor aparente, que
se rota poco y viaja a más lugares que el Access Token— en la capacidad de acreditar pagos ficticios.
La llamada extra es barata; el modo de falla no.

La comparación de firmas usa comparación en tiempo constante. Un `==` sobre strings filtra por
timing, y aunque explotarlo remotamente sea difícil, escribirlo bien no cuesta nada.

### D6 — La vuelta del navegador no acredita, y la consulta de estado sobrevive a la sesión

El `back_url` de Mercado Pago **no cambia estado**. Es una ruta del front que muestra "verificando tu
pago" y consulta el estado real al backend.

Esta es la vulnerabilidad más común de las integraciones de Mercado Pago en producción: tratar la
llegada a `/pago-exitoso` como confirmación. Cualquiera escribe esa URL en la barra de direcciones.

El problema de tiempos: la sesión de familia dura 30 minutos y el round-trip a MP puede excederla
—especialmente en un cupón de pago en efectivo, que puede tardar días—. Opciones evaluadas:

| Opción | Veredicto |
|---|---|
| Extender la sesión de familia | **No.** Alarga la ventana de un token que da acceso a todas las fotos del álbum, para resolver un problema que no es de acceso a fotos |
| Emitir un token nuevo al volver | **No.** Requiere probar quién vuelve, y lo único que trae el navegador es la referencia del pago |
| Consultar el estado con la referencia opaca | **Sí.** Capacidad de mínimo privilegio: 256 bits de entropía, devuelve *solo* el estado de pago, no da acceso a fotos ni a datos del evento |

La consulta por referencia responde igual para una referencia inexistente que para una ajena, y está
limitada por frecuencia.

### D7 — Cobro manual y fulfillment son operaciones distintas

Hoy `PedidoAppService.CambiarEstadoAsync` es la única palanca y el admin puede llevar el pedido a
cualquier estado (ADR-12, producto de cuatro vueltas de feedback — no se re-discute). Eso queda igual
para fulfillment.

El cobro manual es una operación **nueva y separada**: registra medio, monto, autor e instante en la
bitácora. Nunca se infiere un cobro de un cambio de fulfillment, ni al revés.

Cuando hay conflicto —el pedido ya figura pagado, o hay un pago de MP en curso que puede acreditarse
después— la operación no se bloquea: advierte y pide confirmación explícita. El criterio es el mismo
que ya eligió Alberto en ADR-12: el camino guiado es el normal, la excepción tiene que existir y ser
alcanzable, porque quien opera sabe cosas que el sistema no.

### D8 — Cliente HTTP propio contra Mercado Pago, sin el SDK de .NET

Se usan dos operaciones: crear preferencia y consultar pago. Un `HttpClient` tipado vía
`IHttpClientFactory`, con reintentos acotados y timeout explícito.

**El motivo NO es desconfiar del SDK oficial**, sino una propiedad puntual de cómo guarda la
credencial. La guía de arranque de Mercado Pago la configura así:

```csharp
MercadoPagoConfig.AccessToken = builder.Configuration["MercadoPago:AccessToken"];
//    ^^^^^^^^^^^^^^^^^^^^^^ estático: una sola credencial para todo el proceso
```

Con una única cuenta funciona. Con credenciales por tenant (D8b) es una condición de carrera con
dinero de por medio: dos requests concurrentes de tenants distintos pisan la misma variable, y el
pedido de uno puede crear su preferencia contra la cuenta del otro. No se reproduce en desarrollo
—hace falta concurrencia real entre tenants— y el código "se ve bien".

Las versiones nuevas del SDK admiten pisar la credencial por request (`RequestOptions`), así que se
puede usar correctamente. Pero entonces la seguridad depende de que nadie toque nunca el estático, en
cada uso, para siempre. Con un `HttpClient` propio la credencial viaja en el header de cada request y
el problema **no puede existir**, en vez de tener que administrarse.

Para dos llamadas REST, eliminar la pregunta sale más barato que gestionarla. La contra honesta:
cambios en la API de Mercado Pago hay que seguirlos a mano.

### D8b — La credencial se resuelve por servicio, no por constante global

El Access Token se obtiene de un servicio de resolución de credenciales que recibe el tenant, y viaja
en cada request al cliente HTTP. **Hoy ese servicio lee la configuración** —hay un solo fotógrafo, una
sola cuenta— y no hay almacenamiento de credenciales por tenant.

**Por qué acá no se guardan credenciales por tenant**: no porque no vayan a existir —van, y en el
change siguiente— sino porque su custodia (cifrado en reposo, vinculación por autorización, rotación,
revocación) es un cuerpo de trabajo con su propio modelo de amenazas. Este change deja la **forma**
correcta; `marketplace-split-pagos` la llena.

Lo caro no es guardar credenciales por tenant: es descubrir tarde que la credencial estaba cableada
como global en veinte lugares. Por eso el servicio de resolución existe desde ahora aunque hoy
devuelva siempre lo mismo.

Esto es lo que vuelve inaceptable el estático de D8: no es una hipótesis lejana, es la dirección
declarada del producto.

### D8c — La comisión de la plataforma se devenga desde el día uno, con tasa cero

Al confirmarse un pedido se registra la comisión que le corresponde a la plataforma, calculada con la
tasa vigente del tenant. **Hoy esa tasa es cero** —hay un solo fotógrafo y es el dueño del negocio—
así que el devengamiento existe y no cobra nada.

**Por qué no agregarlo después**: prender la comisión más adelante sobre un modelo que nunca la tuvo
obliga a decidir qué pasa con los pedidos históricos que jamás la devengaron, y a meter el concepto en
un flujo ya en producción con dinero real circulando. Devengar desde ahora convierte ese día en
cambiar un número.

El detalle de por qué la comisión se devenga sobre el **pedido** y no sobre el **pago** —que es lo que
impide evadirla cobrando por transferencia— vive en `marketplace-split-pagos`, D1. Acá solo se
construye el devengamiento.

### D9 — Secretos fuera de la configuración versionada, y el pago se apaga si faltan

`appsettings.json` lleva solo lo no sensible (habilitado, moneda, ventana de frescura, URLs de
retorno). Access Token y secreto de webhook van por user-secrets en dev y variables de entorno o
secret store en producción, y son **credenciales distintas** — el de webhook viaja a más lugares y se
rota distinto.

Con `Fotos:MercadoPago:Enabled=true` y credenciales ausentes, la aplicación lo señala al arrancar. Un
módulo de pagos que arranca "a medias" y falla recién cuando una familia real intenta pagar es peor
que uno que no arranca.

Con `Enabled=false`, los endpoints de pago responden como no disponibles y el flujo de la familia
ofrece solo coordinar el cobro con el fotógrafo. Ese es el estado por defecto hasta que haya
credenciales productivas.

**Divergencia deliberada respecto de la guía de Mercado Pago**: su ejemplo pone el Access Token en
`appsettings.Development.json`. Ese archivo vive dentro del árbol del repositorio, así que alcanza un
`.gitignore` mal armado o un `git add -A` distraído para publicar una credencial que mueve dinero. Los
user-secrets de .NET viven fuera del repositorio: el error no es que sea improbable, es que no es
posible.

**Requisito operativo confirmado por Mercado Pago**: las URLs de webhook y de retorno en producción
exigen HTTPS con certificado válido, y no aceptan `localhost` ni direcciones IP. Por eso el túnel de
desarrollo no es comodidad sino requisito, y el dominio con HTTPS es prerrequisito del deploy.

### D10 — Cuotas con interés a cargo de quien paga

La preferencia no configura cuotas sin interés ni ninguna modalidad que traslade el costo de
financiación al fotógrafo (decisión comercial de Alberto: costo extra cero en esta etapa). Quien paga
elige cuotas y absorbe el interés; el monto acreditado es el total del pedido.

### D11 — Rate limiting: política propia para iniciar pago, el webhook aparte

Se suma una política `pago` por IP siguiendo el patrón de `CanjePolicy` en `RateLimitingHelper`, para
el inicio de pago y la consulta de estado por referencia.

El webhook necesita tratamiento inverso y **es una trampa fácil de pasar por alto**: el limitador
global particiona los requests anónimos por IP, y todas las notificaciones de Mercado Pago llegan
desde un puñado de IPs suyas. Una ráfaga de reintentos legítimos compartiría balde y se comería 429s.
El webhook se excluye del limitador global; la protección real de ese endpoint es la firma, que
descarta el ruido antes de tocar la base.

### D12b — El correo se captura ahora porque es el único dato irrecuperable

El pedido pasa a exigir correo de quien compra, y el participante gana un correo opcional que el
fotógrafo carga al armar el evento y que se propone prellenado al confirmar.

**Por qué ahora y no cuando haga falta**: casi todo lo que este proyecto va a necesitar más adelante
—datos fiscales del tenant, planes, límites— son columnas que empiezan a llenarse el día que se
agregan. El correo de quien compra no: **no se le puede pedir a una familia que compró hace ocho
meses**. Es el único dato de esta ronda que, si no se captura en el momento de la operación, se pierde
para siempre.

Y sirve para tres cosas que ya están en el plan: el comprobante de esta ronda, la entrega digital de
fotos del backlog de Fase 4, y —si algún día hay cuentas de comprador— es **el ancla que permite
vincular las compras viejas a la cuenta nueva**. Sin correo, ese día la historia arranca en cero.

Obligatorio en el pedido, opcional en el participante: el fotógrafo carga lo que sabe de la lista del
colegio, y quien compra confirma o corrige.

### D12b-bis — El contacto guardado se propone enmascarado: el dato no llega al navegador

El participante gana contacto (nombre del responsable, teléfono, correo) que el fotógrafo carga al
armar el evento, y que se ofrece al confirmar el pedido. **Pero el cliente nunca recibe los valores
completos**: la respuesta trae una versión enmascarada —`j•••@gmail.com`, `••• ••• 4567`— y una
indicación de que hay datos guardados. Si quien compra los acepta, el cliente envía "usar los
guardados" y el servidor los toma del participante; si prefiere otros, los escribe de cero. En ningún
camino se revelan.

**El problema que resuelve**: los códigos se reparten por WhatsApp (decisión 2026-07-16) y las
familias de un curso suelen compartir grupo. Un código sale del control de su familia con facilidad
—se pega en el grupo del curso, se manda al número equivocado, o **se reenvía a propósito** a los
abuelos, que es un uso previsto y no un accidente—. Hoy un código filtrado expone las fotos del
participante; con un prellenado ingenuo expondría además teléfono y correo de una familia con un
menor, que son datos entregados para una transacción, no para mostrarse. Y ver el teléfono de un
tercero ya cargado en un formulario alarma aunque nadie haga nada con él.

**Por qué la máscara alcanza**: lo que el prellenado tiene que resolver es no tipear y que el dato sea
el correcto. Para eso basta reconocer el propio correo, y `j•••@gmail.com` se reconoce. El beneficio
se conserva entero.

**Lo que NO resuelve, y conviene tenerlo claro**: quien posea el código puede igualmente confirmar un
pedido a nombre de esa familia. Esta decisión protege la confidencialidad del contacto, no impide el
uso del código. Impedirlo requeriría autenticar a las familias, que es justamente lo que el producto
decidió no hacer.

### D12b-ter — Tres identidades distintas, y el contacto del participante no se pisa

En una compra intervienen datos de hasta tres personas, y confundirlas trae errores concretos:

| | Qué es | Ejemplo |
|---|---|---|
| `Participante.Nombre` | La persona fotografiada, habitualmente un menor | La chica de la foto |
| Contacto del participante | El adulto responsable, según la lista que tiene el fotógrafo | La madre |
| Contacto del pedido | Quien efectivamente confirmó la compra | La abuela, a quien le reenviaron el link |

El contacto del pedido es un **snapshot**: registra lo que pasó, aunque no coincida con la ficha.

**Por qué el contacto del participante se completa pero no se pisa**: el sistema no puede distinguir
"me corrigieron un dato mal cargado" de "compró otra persona" — desde adentro son idénticos. Si
cualquier compra sobrescribiera la ficha, el día que compra la abuela o el padre que no vive en la
casa, el contacto de la familia queda reemplazado en silencio.

Entonces: si el participante no tenía contacto, el primer pedido lo llena (no hay nada que perder). Si
ya tenía, la ficha queda intacta y la divergencia se muestra en el detalle del pedido. La corrección
legítima sigue siendo posible, pero la decide el fotógrafo, que es quien tiene el contexto para
saber cuál de los dos casos es.

### D12c — El comprobante se envía fuera de la confirmación del pedido

El correo con el detalle y el enlace de seguimiento se encola; no se envía dentro de la operación que
confirma el pedido.

**Por qué**: sería absurdo perder una venta porque el servidor de correo tuvo un problema. La compra
es el hecho de negocio; el aviso es una consecuencia. Atarlos en la misma transacción hace que la
disponibilidad del correo sea un requisito para vender.

El enlace de seguimiento no agrega mecanismo: es la referencia opaca de D3 servida por el endpoint de
consulta de D6. Lo que cambia es que llega por correo en vez de depender de que no se cierre la
pestaña.

**Consecuencia de infraestructura**: el correo deja de ser accesorio y pasa a ser parte del producto,
así que su entregabilidad —que el mensaje no caiga en spam— es una decisión del deploy, no un detalle
de configuración.

### D13 — La familia va al checkout por redirección, no por el Wallet Brick

El backend devuelve la URL de pago y el front redirige. La alternativa que documenta Mercado Pago como
camino principal es el **Wallet Brick**: el backend devuelve el identificador de preferencia y Angular
renderiza el botón oficial cargando `@mercadopago/sdk-js`.

Las dos son Checkout Pro y las dos son válidas. Acá **no hay riesgo de credencial**: el Brick usa la
Public Key, que está hecha para ser pública. La diferencia es de otra índole:

| | Wallet Brick | Redirección |
|---|---|---|
| Reconocimiento | Botón oficial de Mercado Pago | Botón propio |
| JavaScript de terceros | Sí, en la app que muestra las fotos | No |
| CSP | Hay que habilitar el origen de Mercado Pago | Sin cambios |
| Ejemplos oficiales | Es el camino documentado | Soportado, menos material |

Se elige la redirección por simplicidad y por no sumar JavaScript de terceros a una aplicación que
muestra fotos de menores. Es reversible: cambiar a Brick después toca una pantalla, no el backend
—que en ambos casos crea la misma preferencia—.

## Threat model

Cada fila tiene su requirement en `specs/pago-online-mercado-pago/spec.md` y su test de regresión en
`tasks.md`.

| # | Amenaza | Defensa | Decisión |
|---|---|---|---|
| T1 | Pedir el enlace de pago de un pedido ajeno (IDOR) | El pedido se resuelve solo por los claims firmados de la sesión | — |
| T2 | Manipular el monto a cobrar | El monto sale del `Total` almacenado; el cliente no aporta montos | ADR-07 |
| T3 | Falsificar una notificación de pago | HMAC verificado en tiempo constante | D5 |
| T4 | Reenviar una notificación válida capturada | Ventana de frescura sobre el instante firmado | D5 |
| T5 | Notificación firmada con contenido mentiroso | La verdad se consulta server-to-server | D5 |
| T6 | Pagar de menos y quedar acreditado | Verificación de monto y moneda; discrepancia → revisión manual | D1 |
| T7 | Duplicar la acreditación con reintentos | Restricción única en la bitácora | D2 |
| T8 | Retroceder el estado con una notificación tardía | El estado es un pliegue con precedencia | D1 |
| T9 | Tocar pedidos de otro tenant desde el webhook | Lookup estrecho → contexto de sistema → guard que lo exige | D4 |
| T10 | Hacer pasar la vuelta del navegador por pago hecho | El retorno no muta estado; el front consulta al backend | D6 |
| T11 | Enumerar pedidos por la referencia de pago | Referencia opaca de 256 bits; respuestas indistinguibles | D3 |
| T12 | Confundir el contexto mandando un JWT al webhook | Se rechaza toda notificación con cabecera Authorization | D4 |
| T13 | Filtrar el Access Token por logs o errores | Secretos fuera de config versionada, nunca en logs ni respuestas | D9 |
| T14 | Acumular datos personales de quien paga | Solo se conserva lo necesario para conciliar | — |
| T15 | Abusar del endpoint de inicio de pago | Política de frecuencia propia por IP | D11 |
| T16 | Una sesión de familia operando el admin de pagos | `FamiliaSessionGuard` en el AppService admin | ADR-11 |

## Risks / Trade-offs

- **Este change se apoya en un cambio de plataforma que toca el login** → El orden es deliberado:
  `aislamiento-tenant-anonimo` va primero, con su propio commit y su propia corrida de suite, para
  que un fallo en los flujos de autenticación no se diagnostique mezclado con el módulo de pagos. Si
  ese change se pospusiera, este sigue siendo seguro por D4, solo pierde la segunda capa.
- **Cambio breaking en un enum persistido** → Migración con backfill explícito y `Down` completo. Se
  hace ahora porque no hay datos productivos; en seis meses el mismo cambio cuesta mucho más.
- **La verificación server-to-server agrega una llamada por notificación y una dependencia de
  disponibilidad de MP** → Si la consulta falla, la notificación **no se descarta**: se responde con
  error para que Mercado Pago reintente, que es exactamente para lo que reintenta.
- **Sin SDK, los cambios de la API de Mercado Pago hay que seguirlos a mano** → Superficie chica (dos
  llamadas) y contrato estable. Aceptado en D8.
- **La referencia opaca es una capacidad portadora**: quien la tenga puede consultar el estado de ese
  pago → Mitigado por mínimo privilegio (devuelve solo el estado de pago) y entropía suficiente para
  descartar adivinación.
- **El túnel de desarrollo expone la API local a internet mientras está levantado** → Se levanta solo
  para probar y se baja; nunca contra la base de producción.
- **`RevisionManual` es un estado que requiere intervención humana y hoy no hay alerta activa** → Se
  ve en el listado con filtro propio. Si en uso real aparece seguido, ameritará una notificación.

## Migration Plan

1. Migración EF: agrega `EstadoPago` y `ReferenciaPagoPublica` a `fot_Pedidos`, crea
   `fot_PagosPedido` con su índice único, y **backfillea** antes de retirar el valor viejo:
   - pedidos en `Pagado` → fulfillment `Pendiente`, pago `Pagado`, y una entrada de bitácora de
     origen manual que declara la migración;
   - el resto → pago `SinPagar`, fulfillment sin cambios;
   - `ReferenciaPagoPublica` se genera para todos los pedidos existentes.
2. Recién después, remover `EstadoPedido.Pagado` y ajustar el código que lo referencia.
3. Front y backend se despliegan juntos: el estado de pago es un campo nuevo en los DTO y el listado
   de pedidos cambia de forma.
4. `Fotos:MercadoPago:Enabled=false` hasta tener credenciales productivas verificadas. El módulo
   entra apagado.
5. **Rollback**: la migración tiene `Down` completo. Como el paso 1 no destruye información —traduce
   `Pagado` a dos campos— la vuelta atrás es reconstruible. Una vez que haya cobros reales
   registrados, el rollback deja de ser gratis: a partir de ahí se corrige hacia adelante.

### D12 — Un pago acreditado sobre un pedido cancelado queda en revisión manual

Si se acredita un pago cuyo pedido está `Cancelado`, el estado de pago pasa a `RevisionManual` y el
pedido **no se reactiva**. Hay plata cobrada de algo que el fotógrafo dio de baja: devolver, reactivar
o hablar con la familia son decisiones que dependen de por qué se canceló, y el sistema no lo sabe.

**Alternativa descartada**: acreditar y devolver el pedido a fulfillment `Pendiente`. Es más
automático, pero hace que el sistema revierta por su cuenta una baja que decidió una persona.

## Open Questions

- **Ventana de frescura de la firma**: se arranca en 5 minutos. El número correcto sale de ver la
  latencia real de las notificaciones en el piloto; ajustarlo es cambiar una clave de configuración,
  no toca diseño.
