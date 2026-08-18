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

## Vista general

Las decisiones de abajo se entienden mejor con el mapa a la vista. Nada de esta sección es una
decisión nueva: es el resumen de las que siguen.

### Dónde vive cada cosa

```
 ┌──────────────────────────────────────────────────────────────────────────────┐
 │ FRONTEND · Angular                                                           │
 │   familia :  /carrito → paso de pago    ·  /pago/retorno → "verificando…"    │
 │   admin   :  estado de pago · bitácora · cobro manual                        │
 └───────────────────────────────────────┬──────────────────────────────────────┘
                                         │
 ┌───────────────────────────────────────┴──────────────────────────────────────┐
 │ CONTROLLERS · AcgFotos.Fotos.Controllers                                     │
 │   FamiliaPago       [AllowFamiliaSession]   iniciar pago · consultar estado  │
 │   Notificaciones    [AllowAnonymous]        webhook de Mercado Pago          │
 │   Pedido            [FamiliaSessionGuard]   admin y cobro manual             │
 └───────────────────────────────────────┬──────────────────────────────────────┘
                                         │
 ┌───────────────────────────────────────┴──────────────────────────────────────┐
 │ APPLICATION · AcgFotos.Fotos.Application                                     │
 │                                                                              │
 │   ProcesadorDeCobro   ← ÚNICO camino de acreditación. Todo lo que acredita   │
 │                         pasa por acá: webhook, barrido y cobro manual        │
 │                                                                              │
 │   ┌──────────────────────────────────────────────────────────────────────┐   │
 │   │ IPasarelaPagos   ← el puerto: lenguaje de dominio, sin tipos de MP    │   │
 │   │   CrearEnlaceDePago · ConsultarCobro · ValidarNotificacion            │   │
 │   └──────────────────────────────────────────────────────────────────────┘   │
 └───────────────────────────────────────┬──────────────────────────────────────┘
                                         │
 ┌───────────────────────────────────────┴──────────────────────────────────────┐
 │ INFRASTRUCTURE · AcgFotos.Fotos.Infrastructure                               │
 │   PasarelaMercadoPago  → SDK oficial · RequestOptions construido por llamada │
 │   ResolutorDeCredencial(tenant)  ·  IHttpClient sobre IHttpClientFactory     │
 │   BarridoDeConciliacion (worker, con contexto de sistema por tenant)         │
 └──────────────────┬────────────────────────────────────┬──────────────────────┘
                    │                                    │
        ╔═══════════╧═══════════╗            ┌───────────┴───────────┐
        ║     Mercado Pago      ║            │      PostgreSQL       │
        ╚═══════════════════════╝            │  fot_Pedidos          │
                                             │  fot_PagosPedido      │
                                             │    └ la bitácora      │
                                             └───────────────────────┘
```

La línea que importa es `IPasarelaPagos`: arriba de ella no existe Mercado Pago, existe "cobrar".

### El circuito, de punta a punta

```
 FAMILIA          ANGULAR                 API                      MERCADO PAGO
    │                │                     │                            │
    │ confirma       │                     │                            │
    ├───────────────>├────────────────────>│ EstadoPedido = Pendiente   │
    │                │                     │ EstadoPago   = SinPagar    │
    │                │                     │ + ReferenciaPagoPublica    │
    │                │                     │ + comisión devengada       │
    │                │                     │ + comprobante (encolado)   │
    │                │                     │                            │
    │ "Pagar ahora"  │                     │                            │
    ├───────────────>├────────────────────>│ ① crear preferencia ──────>│
    │                │                     │   monto = Total almacenado │
    │                │                     │   external_reference = ref │
    │                │                     │   X-Idempotency-Key        │
    │                │                     │<──────────── URL de pago ──┤
    │                │<─── URL de pago ────┤ bitácora: Iniciado         │
    │<─ redirección ─┤                     │                            │
    │                                      │                            │
    │ paga en el checkout de Mercado Pago ───────────────────────────>  │
    │                                      │                            │
    │                                      │<── ② notificación firmada ─┤
    │                                      │  ③ validar x-signature     │
    │                                      │  ④ consultar el pago ─────>│
    │                                      │<───── ESTADO REAL ─────────┤
    │                                      │  ⑤ ref → (TenantId, PedidoId)
    │                                      │  ⑥ SetSystemContext        │
    │                                      │  ⑦ verificar monto+moneda  │
    │                                      │  ⑧ bitácora → Pagado       │
    │                                      │                            │
    │<──── vuelve al back_url ─────────────│                            │
    │                │ "verificando tu pago"                            │
    │                ├──── consultar ─────>│ devuelve el estado REAL    │
    │                │<─── estado ─────────┤ (el retorno no cambió nada)│


 SI ② NUNCA LLEGA  ─  la red que no depende de que nos avisen:

    BarridoDeConciliacion ──> pedidos trabados en Iniciado / AcreditandoMP
              └──> ④ consultar el pago ──> mismo ⑦ y ⑧, misma idempotencia
```

### Qué puede acreditar un pedido, y qué no

| Camino | ¿Acredita? | Por qué |
|---|---|---|
| Notificación firmada **+** consulta server-to-server | **Sí** | La firma dice que vale la pena preguntar; la consulta dice qué pasó (D5) |
| Barrido de conciliación | **Sí** | Mismo procesador, mismas reglas. Es el webhook disparado por tiempo (D4) |
| Cobro manual del fotógrafo | **Sí** | Origen `Manual`, con autor e instante. Operación distinta del fulfillment (D7) |
| Llegada al `back_url` del navegador | **Nunca** | Cualquiera escribe esa URL en la barra de direcciones (D6) |
| El cuerpo de la notificación, sin reconsultar | **Nunca** | Una firma válida no prueba nada sobre el estado del pago (D5) |
| Un cambio de estado de fulfillment | **Nunca** | Cobro y entrega son dimensiones independientes (D1) |

### Las dos dimensiones del pedido

```
   EstadoPedido (fulfillment)        EstadoPago (cobro)
   ──────────────────────────        ──────────────────────────────────────
   Pendiente                         SinPagar → Iniciado → AcreditandoMP →
   Impreso                                              → Pagado
   Entregado                                            → Rechazado
   Cancelado                                            → Reembolsado
                                                        → RevisionManual

   Son ortogonales: "entregado sin cobrar" y "cobrado sin imprimir" son
   estados legítimos y frecuentes. EstadoPago es el pliegue de la bitácora,
   no una variable que se muta (D1).
```

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

**Formato — base64url sin relleno**, o sea alfabeto `A-Za-z0-9-_`. 32 bytes dan 43 caracteres.
Escrito explícito porque implementarlo con `Convert.ToBase64String` compila, funciona en los tests y
puede fallar recién contra Mercado Pago: el base64 estándar produce `+`, `/` y `=`, que son
justamente los caracteres que un identificador que viaja por URL no debería llevar.

**Advertencia sobre el origen de esta regla**: la versión anterior de esta decisión afirmaba que
Mercado Pago acepta hasta 64 caracteres y solo letras, números, guion y guion bajo. **Esa
restricción no está documentada por Mercado Pago y no pudo verificarse.** Se conserva base64url igual,
porque es la elección segura bajo cualquiera de las dos hipótesis y no cuesta nada, pero se trata como
supuesto a confirmar en sandbox —no como límite conocido—. La diferencia importa el día que alguien
quiera cambiar el formato apoyándose en una regla que nadie comprobó.

**Doble función**: es también la capacidad que le permite a la familia consultar el estado de su pago
después de que venció su sesión de 30 minutos (ver D6).

### D4 — El webhook establece contexto de tenant antes de tocar datos, y no puede saltearse

```
   POST /api/fotos/pagos/notificaciones   [AllowAnonymous]
        │
        ├─ ① rechazar si trae cabecera Authorization
        │     (MP nunca manda una; si viene, es confusión de contexto)
        │
        ├─ ② validar firma HMAC + ventana de frescura
        │     ↓ falla → 200 vacío, sin tocar datos (ver política abajo)
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

**La respuesta al proveedor tiene dos clases, no una.** Mercado Pago espera 200 o 201 dentro de **22
segundos**; sin eso reintenta con intervalos crecientes. Ese reintento es un mecanismo que conviene
usar a propósito:

| Situación | Respuesta | Por qué |
|---|---|---|
| Firma inválida, fuera de ventana, o cabecera `Authorization` presente | **200** vacío | No es una notificación legítima; reintentarla cada 15 minutos para siempre no arregla nada |
| Referencia externa desconocida | **200** vacío | No hay nada que procesar, y un 5xx le revelaría al emisor que la referencia no existe |
| Ya procesada (violación del índice único de D2) | **200** | Terminó bien: es el resultado idempotente correcto |
| Fallo transitorio —Mercado Pago no responde, la base falla, timeout— | **5xx** | El reintento del proveedor **es** nuestra cola de reintentos. Responder 200 acá tira a la basura la única garantía de entrega que hay |

Decir "200 siempre" es cómodo y está mal: convierte cualquier indisponibilidad momentánea en un cobro
que nunca se acredita y que nadie sabe que se perdió.

**Pero el reintento no alcanza como única red, y esto corrige un supuesto anterior.** La cadencia real
de reintentos no está clara: una fuente indica cada 15 minutos de forma indefinida, y otra un backoff
creciente del estilo 15 min → 30 min → 6 h → 48 h → 96 h. La diferencia importa: bajo la segunda, un
5xx puede volver **dos días después**, y hasta entonces hay un cobro real que el sistema no registró.

Se diseña para el peor de los dos casos con un **barrido de conciliación**: un proceso periódico
consulta a Mercado Pago el estado de los pedidos que llevan demasiado tiempo en `Iniciado` o
`AcreditandoMP` y los resuelve por el mismo camino que una notificación —misma verificación de monto,
misma bitácora, misma idempotencia—. No es un mecanismo nuevo: es el flujo del webhook disparado por
tiempo en vez de por notificación.

**Por qué se hace igual aunque los reintentos resulten ser cada 15 minutos**: una notificación puede
no llegar nunca —configuración mal cargada, un despliegue con la URL vieja, un tópico no suscripto— y
en ese escenario ningún reintento salva nada. El barrido es la única defensa que no depende de que el
proveedor nos avise.

**El presupuesto de 22 segundos condiciona el paso ③.** La consulta server-to-server ocurre dentro
del request, así que lleva un timeout explícito holgadamente menor a ese presupuesto; si se agota, no
se descarta la notificación: se responde 5xx y se procesa en el reintento. Si en el piloto se ve que
la latencia real de Mercado Pago hace de esto el caso común —y no el raro—, la salida es encolar tras
validar la firma; mientras tanto, procesar sincrónico mantiene el flujo en un solo lugar y sin una
cola que mantener.

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

**La validación de la firma se delega en el SDK** (`WebhookSignatureValidator.Validate`), que ya
implementa el contrato exacto de Mercado Pago: cabecera `x-signature: ts=…,v1=…`, HMAC-SHA256 con el
secreto de la aplicación sobre el manifest `id:{data.id};request-id:{x-request-id};ts:{ts};`,
tolerancia configurable para el anti-replay y comparación en tiempo constante. Un `==` sobre strings
filtra por timing; que eso lo garantice el SDK y no una función nuestra es una razón más de D8.

El `data.id` y el `x-request-id` que entran al manifest se toman del query string y de la cabecera del
request: son parte del mensaje firmado, no parámetros nuestros.

**Con una salvedad verificada en el código del SDK**: su `Normalize` solo hace `Trim`, y su
documentación dice que el valor entra al manifest *exactly as received*. Hay indicios de que Mercado
Pago espera que un `data.id` con alfanuméricos en mayúscula se pase a minúsculas antes de firmar —en
cuyo caso su propio SDK no cumpliría esa regla—. Para el tópico `payment` es indistinto, porque el id
es numérico. Deja de serlo para `mp-connect` y cualquier tópico con identificadores alfanuméricos, que
es terreno de `marketplace-split-pagos`. Como el `dataId` lo pasamos nosotros como argumento, la
corrección es de una línea; lo que hay que saber es cuál de las dos formas firma Mercado Pago, y eso
se determina en sandbox (tarea 3b.5), no por lectura.

**Supuesto sobre el secreto, con consecuencias**: se asume **un** secreto de webhook por aplicación,
compartido por todos los vendedores vinculados. Es lo que documenta Mercado Pago y lo que vuelve
viable validar la firma antes de saber de qué tenant es el pedido —el paso ② ocurre antes del ④—. Si
en la verificación de sandbox resultara haber un secreto por vendedor, el orden de D4 no se sostiene y
hay que resolver el tenant antes de validar. Ver Open Questions.

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

### D8 — El SDK oficial de Mercado Pago, detrás de un puerto propio

Se usa el paquete oficial `mercadopago-sdk` (3.5.0, net8.0+) a través de un puerto del vertical,
`IPasarelaPagos`: la interfaz vive en `AcgFotos.Fotos.Application` en lenguaje de dominio —crear
enlace de pago, consultar cobro, validar notificación— y el adaptador que la implementa con el SDK
vive en `AcgFotos.Fotos.Infrastructure`. Ni el dominio ni los AppServices ven un tipo de Mercado Pago.

**Se evaluó escribir un `HttpClient` propio y se descartó.** El motivo que lo justificaba —que el SDK
guarda la credencial en un estático de proceso, y con credenciales por tenant eso es una condición de
carrera con dinero de por medio— **es incorrecto**. Verificado en el código fuente del SDK: todos los
métodos aceptan `RequestOptions`, el header `Authorization` se arma siempre desde
`requestOptions.AccessToken`, y el estático es únicamente el *fallback* cuando ese valor viene vacío.
No es una capacidad reciente: está desde la línea 2.x.

La carrera descrita solo existe si el código **asigna** el estático en cada request. Usando
`RequestOptions` nadie lo asigna nunca, y la carrera no tiene dónde ocurrir. El riesgo real es otro y
es de omisión: un call site que se olvide de pasar `RequestOptions` cae al fallback en silencio.

**Ese riesgo se cierra con una regla verificable, no con disciplina**: `MercadoPagoConfig.AccessToken`
no se asigna nunca, y un test de arquitectura falla si alguien lo hace. Con el estático en `null`, un
olvido produce `Bearer ` → 401 → falla ruidosa e inmediata. Nunca un cobro contra la cuenta
equivocada. Es la misma clase de defensa que el test que impide que `Base` referencie al vertical:
convierte "hay que acordarse para siempre" en "el build se rompe".

**Y el argumento de "son dos llamadas REST" no sobrevive al alcance real de la plataforma.** Lo que
el SDK ya trae resuelto y mantenido:

| Pieza del SDK | Qué evita escribir |
|---|---|
| `WebhookSignatureValidator` | HMAC-SHA256 sobre `id:{data.id};request-id:{x-request-id};ts:{ts};`, ventana de tolerancia y comparación en tiempo constante — exactamente lo que pide D5 |
| `OAuthClient` | URL de autorización, canje del code, refresh del token — casi todo `marketplace-split-pagos` |
| `PreferenceClient` / `PaymentClient` | Checkout Pro y consulta de pago, con `MarketplaceFee` incluido |
| `OrderClient` | La Orders API, si algún día se migra a Bricks (también soporta `marketplace_fee`) |
| `IdempotentRequest` | El `X-Idempotency-Key` de D14 |

**Las contras que se aceptan, y cómo se contienen**:

| Contra | Contención |
|---|---|
| Queda estado global mutable (`AccessToken`, `HttpClient`, `Serializer`, `RetryStrategy`) | El test de arquitectura sobre `AccessToken`; el resto se configura una sola vez en el módulo Autofac |
| No integra `IHttpClientFactory`; su `DefaultHttpClient` usa un `HttpClient` estático con timeout fijo de 30s y no admite timeout por request | Se inyecta un `IHttpClient` propio —la interfaz tiene un solo método— que usa el factory, con timeout y política de reintentos nuestros |
| `BuildRequestOptions` **muta** el `RequestOptions` que recibe, escribiéndole el token resuelto | Se construye uno nuevo por llamada; nunca se comparte ni se cachea una instancia |
| Dependencia de terceros en el camino del dinero | El puerto acota la superficie: reemplazar el adaptador no toca dominio, aplicación ni tests de negocio |

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

**Base de cálculo y redondeo, fijados acá para que no se decidan por accidente en el código**:

| | Definición |
|---|---|
| Base | El `Total` congelado del pedido (ADR-07), sin envío ni recargos de financiación |
| Cálculo | `Total × tasa`, redondeado a **2 decimales hacia abajo** — el resto queda del lado del fotógrafo, que es el criterio conservador cuando el redondeo es a favor de quien intermedia |
| Momento | Al confirmar el pedido, con la tasa vigente congelada junto al importe |
| Moneda | La del pedido; no hay conversión |

**Y un detalle de Mercado Pago que cambia lo que el fotógrafo efectivamente recibe**: el parámetro que
transporta la comisión —`marketplace_fee` en la preferencia de Checkout Pro— es un **monto absoluto**,
no un porcentaje. Además Mercado Pago descuenta **primero su propia comisión** y la del marketplace
sale del remanente. O sea que el neto del fotógrafo es `Total − comisión MP − comisión plataforma`, y
la comisión de la plataforma no es un porcentaje del neto sino del total.

Escribirlo importa porque es exactamente el punto donde un fotógrafo va a reclamar que las cuentas no
le cierran. El neto esperado se documenta con un ejemplo numérico en `docs/06-cobros.md`, no se deja a
que cada uno lo deduzca.

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

### D14 — Idempotencia también hacia afuera, con clave derivada y no aleatoria

D2 resuelve la idempotencia de lo que **entra** (notificaciones repetidas). Falta la de lo que
**sale**: toda creación contra Mercado Pago viaja con `X-Idempotency-Key`.

**El caso que cubre**: la llamada sale, Mercado Pago la procesa, y la respuesta se pierde por timeout
o corte de red. Para nosotros es indistinguible de "no llegó", así que se reintenta — y sin clave de
idempotencia, el reintento crea un segundo recurso. Con dos llamadas y reintentos acotados la ventana
es chica, pero el modo de falla es cobrar dos veces, que es el peor que tiene este módulo.

**La clave se deriva, no se sortea**: se construye a partir de la referencia opaca del pedido y del
intento, de forma que el mismo intento reintentado produzca la misma clave. Una clave aleatoria por
llamada compila, pasa los tests y **no protege de nada**: cada reintento traería una clave nueva y
Mercado Pago lo trataría como una operación distinta. Es el error clásico de este parámetro.

En el SDK esto se expresa implementando `IdempotentRequest`; el header solo se autogenera con un GUID
cuando el request es nulo, así que dejarlo librado al default sería justamente la versión que no
protege.

**La defensa que no depende de Mercado Pago va primero.** Hay indicios de que `X-Idempotency-Key`
está documentado para `/v1/payments` pero **no** para `/checkout/preferences`, que es justamente el
endpoint que usamos. Si eso se confirma, el header no serviría de nada ahí. Por eso el orden de las
defensas es:

1. **Deduplicación propia**: un pedido tiene a lo sumo una preferencia vigente; iniciar el pago de un
   pedido que ya la tiene devuelve la existente en vez de crear otra. Esto es nuestro, funciona
   soporte Mercado Pago la idempotencia o no, y cubre tanto el reintento automático como a la persona
   que toca el botón tres veces.
2. **`X-Idempotency-Key` derivada**, que se manda igual: si el endpoint la respeta, suma; si la
   ignora, no cuesta nada.

Invertir ese orden —confiar en el header y agregar la deduplicación después— es apoyar la defensa
principal en una capacidad del proveedor que no está confirmada.

### D15 — Se deja abierta la costura del proveedor, sin abstraer proveedores

Mercado Pago es el proveedor de esta etapa y el diseño no construye nada para un segundo. Pero hay
tres decisiones que hoy no cuestan nada y después se pagan con una migración sobre datos con dinero
real, así que se toman ahora:

| Decisión | Ahora | Si se posterga |
|---|---|---|
| `OrigenPago` en `PagoPedido` **es** la dimensión "quién procesó", separada de `MedioPago` "cómo se pagó" | Una columna con dos valores | Migrar la tabla del dinero y decidir qué backfillear en las filas históricas |
| La ruta del webhook lleva el proveedor (`.../notificaciones/mercadopago`) | Un segmento en la ruta | Coordinar el cambio de URL con el panel de Mercado Pago, que ya está mandando notificaciones |
| Las credenciales se indexan por (tenant, proveedor) | Una columna en el índice único | Migrar la tabla del cifrado en reposo y la rotación, con credenciales productivas adentro |

**Por qué el enum actual está mal y conviene arreglarlo de paso**: hoy `MedioPago` vale
`Efectivo`/`MercadoPago`, que son dimensiones distintas. Con Mercado Pago se paga con tarjeta, en
Rapipago o con saldo en cuenta: mismo origen, tres medios. Mezclarlos hace que la pregunta "¿cuánto
se cobró con tarjeta?" no tenga respuesta.

**La ruta por proveedor es la única de las tres que además es una decisión de seguridad.** Cada
proveedor firma distinto —Mercado Pago manda `x-signature`, otros mandan su propia cabecera con su
propio manifest—. Un endpoint único tendría que inspeccionar el request para deducir quién lo envió y
recién ahí elegir cómo autenticarlo: estaría usando entrada no confiable para decidir cómo verificar
la confianza. Con una ruta por proveedor, el endpoint sabe qué va a validar antes de mirar nada.

**Lo que deliberadamente NO se hace**: registro de estrategias, vocabulario normalizado de estados
entre proveedores, o selección de proveedor por configuración. Una abstracción de proveedores
diseñada sin el segundo caso a la vista sale mal, y sale peor que no tenerla: hay que desarmarla antes
de poder usarla. Acá se deja la **costura** —el puerto `IPasarelaPagos`, el discriminador, la ruta— y
la abstracción, si algún día hace falta, la dicta el segundo proveedor real.

Vale la pena notar que la decisión que más independencia aporta ya estaba tomada por otro motivo:
como la comisión se devenga sobre el **pedido** y no sobre el pago (D8c), el ingreso de la plataforma
no depende de que el proveedor soporte split. Uno que no lo soportara se salda por cuenta corriente.

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
| T17 | Duplicar un cobro porque se reintentó una llamada saliente cuya respuesta se perdió | Una sola preferencia vigente por pedido, más `X-Idempotency-Key` derivada | D14 |
| T18 | Perder una acreditación al responder 200 ante un fallo transitorio | Fallo transitorio devuelve 5xx y el proveedor reintenta | D4 |
| T19 | Un cobro real que nunca se registra porque la notificación no llegó o su reintento tarda días | Barrido de conciliación sobre pedidos trabados en `Iniciado`/`AcreditandoMP` | D4 |

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
- **El SDK oficial es una dependencia de terceros en el camino del dinero, con estado global mutable**
  → Acotada por el puerto `IPasarelaPagos` y por el test de arquitectura que prohíbe asignar
  `MercadoPagoConfig.AccessToken`. A cambio, la validación de firma, el flujo OAuth y el seguimiento
  de los cambios de API dejan de ser código nuestro. Aceptado en D8.
- **El procesamiento sincrónico del webhook vive dentro de un presupuesto de 22 segundos que no
  controlamos** → Timeout explícito por debajo de ese techo y 5xx ante agotamiento, para caer en el
  reintento del proveedor. Si el piloto muestra que es el caso común, la salida conocida es encolar
  tras validar la firma (D4).
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
- **Con qué secreto se firma la notificación cuando la preferencia se creó con el token de otro
  vendedor (flujo OAuth)** — la única pregunta abierta que puede cambiar el diseño, no un número. La
  documentación dice que el secreto de webhook es **por aplicación**, y D4/D5 dependen de eso: validan
  la firma en el paso ② *antes* de saber de qué tenant es el pedido (paso ④). Si resultara existir un
  secreto por vendedor, ese orden no se sostiene y habría que resolver el tenant antes de validar,
  invirtiendo dos pasos que hoy están ordenados por seguridad. **Se verifica con credenciales de
  prueba antes de escribir el handler**, no después: descubrirlo tarde reescribe el núcleo del change.
  Hoy no bloquea nada, porque con un solo fotógrafo la aplicación y el vendedor son la misma cuenta;
  la pregunta existe por `marketplace-split-pagos`.
- **La restricción de formato de `external_reference`** (largo y alfabeto) no está documentada por
  Mercado Pago. Base64url se adopta igual por ser seguro bajo cualquier hipótesis, pero conviene
  confirmar el límite real en la misma corrida de sandbox (D3).
- **Cadencia real de reintentos del webhook**: hay fuentes en conflicto (cada 15 minutos indefinido
  vs. backoff hasta 96 horas). No bloquea: el diseño ya asume el peor caso con el barrido de
  conciliación de D4. Confirmarlo solo ajusta cada cuánto corre el barrido.
- **Si `X-Idempotency-Key` aplica a `/checkout/preferences`** o solo a `/v1/payments`. No bloquea: la
  defensa principal es la deduplicación propia (D14), y el header se manda igual.
- **Si el manifest de la firma requiere `data.id` en minúsculas**. Solo afecta a tópicos con
  identificadores alfanuméricos, o sea a `marketplace-split-pagos`. El SDK no normaliza (D5).

**Nota sobre el origen de estas preguntas**: varias surgieron de consultar al asistente de
Mercado Pago, que está en beta y ya se equivocó en un punto verificable —afirmó que el manifest de la
firma no lleva `;`, cuando el propio SDK de Mercado Pago lo arma con `;`—. Sus respuestas entran acá
como indicios a verificar, nunca como fuente. Donde contradijo lo leído en la documentación, se eligió
la opción que es correcta bajo ambas hipótesis.
