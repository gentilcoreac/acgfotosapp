## Context

Ver `proposal.md` § Why para la motivación y el agujero de la transferencia. Acá el estado del que se
parte y las restricciones.

**Depende de `pagos-mercado-pago`**, que deja tres cimientos: el receptor del cobro resuelto por un
servicio que recibe el tenant (D8b), la comisión devengada por pedido con tasa configurable, y el
webhook con aislamiento de tenant explícito (D4).

**Requisitos externos, verificados en la documentación de Split de Pagos 1:1**:

| Requisito | Sobre quién recae |
|---|---|
| Cuenta de Mercado Pago con verificación de identidad **nivel 6 (KYC 6)** | La plataforma, no cada fotógrafo |
| App móvil de Mercado Pago instalada para gestionar los cobros | La plataforma |
| Autorización OAuth completada | Cada fotógrafo vinculado |
| Aprobación del equipo comercial | **No aplica al 1:1** — es requisito del 1:N (ver D9) |

Disponible en Argentina, Brasil, Chile, Colombia, México, Perú y Uruguay. Admite Checkout Pro,
Checkout API y Bricks; nosotros usamos Checkout Pro (D13 de `pagos-mercado-pago`).

El KYC 6 es el único requisito que puede demorar y **no depende de escribir código**: es un trámite de
identidad del negocio. Por eso arranca en paralelo desde la tarea 0.1 y el resto del change se diseña
para desplegarse sin él (D7).

**El perfil de riesgo del sistema cambia acá.** Hasta este change, una filtración de la base expone
fotos. A partir de acá expone la capacidad de operar sobre el dinero de N fotógrafos. Todo lo que
sigue está diseñado con esa premisa.

## Vista general

Ver `pagos-mercado-pago` § Vista general para el circuito base. Acá solo el delta: qué se agrega y
dónde. La arquitectura en capas **no cambia** —el puerto `IPasarelaPagos` es el mismo—; lo que cambia
es de dónde sale la credencial y a dónde va el dinero.

### El delta, en una línea

```
  ANTES (pagos-mercado-pago)              DESPUÉS (este change)
  ──────────────────────────              ─────────────────────────────────────
  ResolutorDeCredencial(tenant)           ResolutorDeCredencial(tenant)
    └─> lee configuración                   └─> lee la credencial VINCULADA
        (una cuenta, la de Alberto)             del tenant, cifrada en la base

  preferencia sin comisión                preferencia con marketplace_fee
    └─> todo va a una cuenta                └─> neto al fotógrafo + comisión
                                                a la plataforma, repartido por MP
```

### Vinculación de la cuenta del fotógrafo

```
 FOTÓGRAFO           PLATAFORMA                          MERCADO PAGO
     │                    │                                    │
     │ "Vincular cuenta"  │                                    │
     ├───────────────────>│ genera state (un solo uso, del tenant)
     │                    │ genera code_verifier + code_challenge
     │<── redirección ────┤                                    │
     │                                                         │
     ├── auth.mercadopago.com/authorization ──────────────────>│
     │     client_id · response_type=code · platform_id=mp     │
     │     redirect_uri · state · scope=offline_access         │
     │     code_challenge · code_challenge_method=S256         │
     │                                                         │
     │ aprueba                                                 │
     │<───────────────── code (vive 10 min) + state ───────────┤
     │                    │                                    │
     ├───────────────────>│ valida state: existe · no usado ·  │
     │                    │   no vencido · del tenant correcto │
     │                    │ lo consume                         │
     │                    ├── POST /oauth/token ──────────────>│
     │                    │     + code_verifier                │
     │                    │<── access_token (180 d) ───────────┤
     │                    │    refresh_token · user_id         │
     │                    │ cifra y guarda (clave fuera de BD) │
     │<─── vinculada ─────┤                                    │

 CICLO DE VIDA
   renovación proactiva ──> persiste TODO lo que devuelve, incluido el refresh nuevo
   webhook mp-connect  ──> desvinculación ⇒ vinculación caída, en el momento
   fallo definitivo    ──> vinculación caída + aviso + sin pago online en sus eventos
```

### El dinero, con split

```
                        FAMILIA paga  $ 10.000
                              │
                              ▼
                    ╔═════════════════════╗
                    ║    Mercado Pago     ║  descuenta primero su comisión
                    ╚══════════┬══════════╝
                               │
                 ┌─────────────┴─────────────┐
                 ▼                           ▼
        CUENTA DEL FOTÓGRAFO          CUENTA DE LA PLATAFORMA
        Total − comisión MP           marketplace_fee
             − marketplace_fee        (monto absoluto, ≤ total)

   El dinero NUNCA pasa por la cuenta de la plataforma antes de repartirse:
   la preferencia se crea con el token del FOTÓGRAFO y Mercado Pago reparte.
```

### Por qué hace falta igual una cuenta corriente

```
   Pedido confirmado ──> comisión DEVENGADA siempre, sin importar cómo se cobre
                              │
              ┌───────────────┴───────────────┐
              ▼                               ▼
      cobrado por MP                  cobrado por transferencia
      el split la RETIENE             nadie la retiene
              │                               │
              ▼                               ▼
      saldo pendiente: 0            saldo pendiente: la comisión
                                    └─> se liquida contra ventas
                                        siguientes por MP, o se factura

   Sin esto, el método de cobro que más le conviene al fotógrafo sería el
   que deja a la plataforma en cero. Ese es el agujero que tapa D1.
```

### Controles nuevos sobre el circuito base

| Control | Qué detecta | Decisión |
|---|---|---|
| `user_id` de la notificación vs. cuenta vinculada del tenant | Dinero acreditado en la cuenta equivocada | D12 |
| `marketplace_fee` ≤ total, validado antes de enviar | Comisión mal calculada, rechazada de forma opaca | D10 |
| Reporte de ventas de Split vs. bitácora y movimientos | Cobros que ocurrieron y el sistema nunca registró | D11 |
| Webhook `mp-connect` | Revocación que de otro modo se descubre meses después | D6 |

## Goals / Non-Goals

**Goals**

- Que cada fotógrafo cobre en su cuenta sin que la plataforma custodie dinero ajeno.
- Que el ingreso de la plataforma no dependa del método de pago que elija la familia.
- Que las credenciales de terceros estén protegidas a la altura de lo que habilitan.
- Que la capa pueda desplegarse dormida si el KYC 6 de la cuenta de la plataforma se demora.

**Non-Goals**

- Facturación electrónica, integración con AFIP o emisión de comprobantes.
- Planes, precios escalonados o gestión comercial de la relación con cada fotógrafo.
- Onboarding autogestionado de fotógrafos nuevos (alta de tenant, verificación de identidad).
- Reportes de negocio consolidados más allá del saldo por tenant.
- Métodos de cobro que no sean Mercado Pago o el registro manual ya existente.

## Decisions

### D1 — La comisión se devenga al confirmar el pedido, no al cobrar

El devengamiento ocurre cuando se confirma el pedido, con la tasa vigente en ese momento, y queda
congelado —mismo criterio que ADR-07 usa para el precio—.

**Por qué**: es lo que tapa el agujero de la transferencia. Si la comisión naciera del pago, el
método de cobro que más le conviene al fotógrafo (transferencia, donde no pierde ni la comisión de
Mercado Pago ni la de la plataforma) sería el que deja a la plataforma en cero. Ese incentivo no
requiere mala fe para activarse: cualquiera que haga la cuenta llega ahí.

Devengando sobre el pedido, al fotógrafo le da igual el método y puede ofrecerle a la familia el que
le sirva. La plataforma cobra por la venta que hizo posible, no por mover dinero — que además es la
descripción correcta de lo que aporta.

**Consecuencia**: hace falta una cuenta corriente. Es el costo de esta decisión y se asume.

### D2 — El saldo se deriva de movimientos inmutables

La cuenta corriente es un libro de movimientos —devengado, retenido, revertido, liquidado— y el saldo
es su pliegue. No hay un campo "saldo" que se sume y se reste.

**Por qué**: es el mismo criterio de D1 de `pagos-mercado-pago` para el estado de pago, por las mismas
razones. Un saldo que se muta es un saldo que en algún momento va a discrepar de sus movimientos, y
cuando eso pasa con dinero, no hay forma de saber cuál de los dos tiene razón. Con el pliegue, la
pregunta "¿por qué debo esto?" siempre tiene respuesta.

Se admite un saldo persistido como caché denormalizada, siempre que sea reconstruible y exista un
camino que lo recalcule.

### D3 — Vinculación por autorización, nunca por credencial tipeada

El fotógrafo vincula su cuenta autorizando a la plataforma en Mercado Pago. La plataforma no pide ni
acepta credenciales copiadas del panel de desarrolladores.

**Por qué, más allá de la comodidad**:

| | Credencial tipeada | Autorización |
|---|---|---|
| Lo que termina guardando la plataforma | Credencial de poder total sobre la cuenta | Credencial acotada a lo que el marketplace necesita |
| Cómo corta el fotógrafo la relación | Pidiéndole a la plataforma que la borre | Revocando desde su panel, sin intermediarios |
| Si la base se filtra | Credenciales sin vencimiento | Credenciales que vencen y rotan |
| Qué tiene que hacer un fotógrafo no técnico | Navegar un panel de desarrolladores | Un clic y aprobar |

Y el argumento que cierra la discusión: **Split de Pagos lo exige**. Mercado Pago necesita constancia
de que el vendedor autorizó al marketplace; no alcanza con poseer su credencial.

**El flujo concreto, tal como lo documenta Mercado Pago**:

```
  ① El fotógrafo va a auth.mercadopago.com/authorization
       client_id · response_type=code · platform_id=mp · redirect_uri
       state · scope=offline_access · code_challenge + code_challenge_method=S256
  ② Aprueba en Mercado Pago
  ③ Vuelve al redirect_uri con code (vive 10 minutos) + state
  ④ POST /oauth/token  grant_type=authorization_code + code_verifier
       → access_token (180 días) · refresh_token · user_id · public_key · live_mode
```

Tres parámetros que parecen opcionales y no lo son:

- **`scope=offline_access`** decide si la vinculación se puede renovar. La documentación de Split de
  Pagos dice que sin él no hay `refresh_token` utilizable; la documentación general de OAuth muestra
  ejemplos sin `scope` que igual devuelven `refresh_token`. **Se manda siempre**, y el motivo es la
  asimetría: si es necesario y lo omitimos, todas las vinculaciones caen juntas a los seis meses y nos
  enteramos seis meses después; si no es necesario, mandarlo no cuesta nada. Ver D6.
- **`code_challenge` / `code_verifier` (PKCE, S256)** es lo que Mercado Pago recomienda y lo que evita
  que un `code` interceptado en el retorno sirva sin el verificador. Complementa al `state` de D4: el
  `state` ata el retorno a nuestra solicitud, PKCE ata el canje a quien la inició. **Requiere
  habilitar PKCE en los detalles de la aplicación** en el panel de Mercado Pago: sin ese paso, MP no
  exige el `code_challenge` y la defensa queda decorativa aunque el código la mande.
- **`redirect_uri` estática y registrada**, idéntica a la del panel. Lo que varía por intento viaja en
  el `state`, nunca en la URL.

Todo esto lo implementa el `OAuthClient` del SDK oficial —`GetAuthorizationURLAsync`,
`CreateOAuthCredentialAsync`, `RefreshOAuthCredentialAsync`—, que es una de las razones de haber
adoptado el SDK en `pagos-mercado-pago` D8. Se usa a través del mismo puerto `IPasarelaPagos`.

### D4 — El retorno de la autorización se ata a un valor de un solo uso

Al iniciar la vinculación se genera un valor no adivinable, asociado al tenant y con vencimiento
corto. El retorno solo se acepta si trae ese valor, no fue usado y no expiró.

**Qué previene**: que un atacante induzca a un fotógrafo a completar un flujo de autorización que
termine vinculando **la cuenta del atacante** al tenant de la víctima —o la del fotógrafo al tenant del
atacante—. Sin esa atadura, el retorno es un endpoint que acepta vincular lo que le manden.

Es el mismo tipo de defensa que el `state` de cualquier flujo de autorización, y la razón por la que
existe en el estándar.

### D5 — Las credenciales de terceros se cifran con clave fuera de la base

Cifrado en reposo, con la clave en el almacén de secretos del entorno, nunca en la base ni en la
configuración versionada.

**Por qué no alcanza con proteger la base**: si la clave vive en la misma base, cifrar no agrega nada
contra el escenario que importa —que alguien obtenga una copia de la base—. Separar la clave del dato
es lo que convierte una filtración de base en algo inútil para el atacante.

Nunca se registran en logs, nunca viajan al cliente, y el estado de vinculación que ve el fotógrafo
expone datos identificatorios de la cuenta, jamás la credencial.

### D6 — La renovación es proactiva y la caída es visible

Las credenciales se renuevan antes de vencer, sin intervención. Si la renovación falla de forma
definitiva —típicamente porque el fotógrafo revocó desde Mercado Pago— la vinculación se marca como
caída, se le informa, y los eventos afectados dejan de ofrecer pago online.

La credencial vive **180 días**, y acá hay una corrección importante respecto de lo que este diseño
afirmaba antes. La renovación **sí existe**: `POST /oauth/token` con `grant_type=refresh_token`
devuelve un `access_token` nuevo sin volver a molestar al fotógrafo. La documentación de Split agrega
que también devuelve un `refresh_token` nuevo —es decir, que rota—, aunque eso no aparece confirmado
en la documentación general de OAuth.

**Por eso la regla de implementación no depende de cuál versión sea cierta**: la renovación persiste
*todo* lo que devuelva el proveedor. Si rota, la cadena sigue viva; si no rota, se reescribe el mismo
valor y no pasa nada. Guardar solo el `access_token` es el único camino que falla, y falla recién en
la renovación siguiente, seis meses después.

Lo que no tiene vuelta es **dejarla vencer**: pasados los 180 días sin renovar, hay que rehacer el
flujo de autorización completo. Y la renovación solo está disponible si en la autorización inicial se
pidió `scope=offline_access` (D3) — un parámetro omitido el primer día condena a re-vincular a todos
los fotógrafos cada seis meses, y se descubre recién seis meses después.

De ahí que la renovación sea **proactiva y con margen**: se dispara bastante antes del vencimiento,
con reintentos, y no una vez el último día. Seis meses es tiempo de sobra para que el olvido sea el
modo de falla probable, así que tiene que correr sola.

**La revocación se entera por notificación, no por el fallo de una renovación.** Mercado Pago publica
el tópico de webhook **`mp-connect`**, que avisa conexiones y desconexiones OAuth. Sin eso, un
fotógrafo que revoca desde su panel queda con la vinculación aparentemente sana hasta la próxima
renovación —que puede tardar meses— o hasta que una familia no pueda pagar. Con `mp-connect`, la
vinculación se marca caída en el momento.

Las dos señales conviven y no son redundantes: `mp-connect` cubre la revocación explícita; el fallo
de renovación cubre todo lo demás (cuenta dada de baja, aplicación deshabilitada, cambios del lado de
Mercado Pago que no generan notificación).

**Por qué importa el aviso**: una vinculación caída en silencio se descubre cuando una familia no
puede pagar. El costo lo paga el fotógrafo en ventas perdidas y la plataforma en confianza.

### D7 — La capa se despliega dormida mientras no esté habilitada

Con el marketplace deshabilitado, el sistema opera como lo deja `pagos-mercado-pago`: un receptor,
comisión en cero, sin split. Encenderlo es configuración, no despliegue de código nuevo.

**Por qué se diseña así**: lo que habilita el split del lado de Mercado Pago no lo controlamos ni
sabemos cuánto tarda. Un diseño que solo funciona habilitado convierte una demora ajena en un bloqueo
total. Con el interruptor, la demora es una funcionalidad que todavía no se encendió.

**El interruptor se justifica solo, aunque la habilitación resulte trivial**: es el camino de rollback
si el split se comporta distinto de lo esperado en producción, y permite desplegar todo el código
antes de tener credenciales productivas. La habilitación fue el motivo de que existiera, no la única
razón para conservarlo.

### D8 — El webhook resuelve la credencial por tenant, no por proceso

La verificación server-to-server de una notificación usa la credencial vinculada del tenant dueño del
pedido, resuelta después de establecer el contexto de sistema (D4 de `pagos-mercado-pago`).

Este es el change donde la regla de D8 de aquel change deja de ser precaución y pasa a ser
obligatoria: cada notificación necesita **una credencial distinta** según a quién pertenezca el
pedido, así que la credencial viaja por request en el `RequestOptions` del SDK y
`MercadoPagoConfig.AccessToken` sigue sin asignarse nunca —con el test de arquitectura que lo
garantiza—. Si alguien lo asignara acá "como default", el modo de falla no sería un 401: sería crear
la preferencia de un fotógrafo contra la cuenta de otro.

**Un orden que hay que respetar y es fácil de romper**: la firma se valida con el secreto de la
**aplicación** (el nuestro), antes de saber de qué tenant es el pedido; la credencial del **vendedor**
se resuelve después, para la consulta server-to-server. Son dos secretos distintos en dos momentos
distintos del mismo request. La tarea 3b.1 de `pagos-mercado-pago` confirma ese supuesto en sandbox
antes de que se escriba el handler, precisamente porque si resultara haber un secreto de webhook por
vendedor, este orden se invierte.

### D9 — El split es 1:1, y el modelo de pedido ya lo garantiza

Cada cobro se reparte entre **un** fotógrafo y la plataforma. La comisión no cuenta como un segundo
vendedor: es la tajada de quien intermedia, no un receptor más.

Esto no hay que construirlo, ya está: `Pedido` es una entidad multi-tenant que cuelga de un
participante, y por lo tanto de un grupo, un evento y un único fotógrafo. No existe forma de armar un
pedido que cruce tenants.

**Por qué se escribe igual**: el modelo 1:N —un pago repartido entre varios vendedores— está
disponible solo para vendedores de cartera asesorada, a través del equipo comercial de Mercado Pago. Y
la funcionalidad que nos llevaría ahí tiene aspecto de mejora de experiencia: permitir que una familia
con hijos en dos escuelas, fotografiadas por fotógrafos distintos, pague todo en un solo pedido. Hoy
son dos códigos, dos álbumes y dos pedidos, y conviene que siga siendo así mientras el 1:1 sea lo que
mantiene la integración en el camino autoservicio.

### D10 — Cómo viaja el split, y qué recibe realmente el fotógrafo

La preferencia de Checkout Pro se crea **con el `access_token` del fotógrafo** obtenido por OAuth, y
lleva `marketplace_fee`. Mercado Pago reparte solo.

Tres propiedades del mecanismo que conviene tener escritas, porque cada una es un error probable:

| | Qué es |
|---|---|
| `marketplace_fee` | Un **monto absoluto** en la moneda de la operación, no un porcentaje. El porcentaje es nuestro; a Mercado Pago le llega el resultado. Por defecto `0` |
| Validación del proveedor | **No puede superar el monto total** de la operación; si lo supera, Mercado Pago rechaza con `invalid_marketplace_fee`. El sistema lo valida antes de enviar, para que el error sea nuestro y explicado, no un rechazo opaco del proveedor |
| Orden de descuento | Mercado Pago cobra **primero** su comisión; la del marketplace sale del remanente. Documentado en la guía de Split, no confirmado por otras fuentes: se verifica con un cobro real (tarea 8.3) antes de comunicarle números a ningún fotógrafo |
| Neto del fotógrafo | `Total − comisión MP − comisión plataforma` |

Con Checkout API el parámetro se llama `application_fee` y con la Orders API vuelve a llamarse
`marketplace_fee`. Como el nombre depende del producto y nosotros usamos Checkout Pro, el adaptador es
el único lugar donde ese nombre aparece.

**La consecuencia que hay que comunicar**: la comisión de la plataforma se calcula sobre el total del
pedido, pero se cobra del remanente. Un fotógrafo que haga la cuenta sobre lo que efectivamente le
acreditaron va a ver un porcentaje mayor al pactado. No es un error del sistema y por eso va explicado
con números en `docs/06-cobros.md`, no descubierto por él.

### D11 — Conciliación con el reporte del proveedor, no solo con nuestros datos

La cuenta corriente de D2 responde "qué le debe cada fotógrafo a la plataforma" con **nuestros**
datos. Falta la otra mitad: comparar contra lo que Mercado Pago dice que efectivamente pasó.

Mercado Pago publica un **reporte de ventas de Split de Pagos** por vendedor vinculado, descargable en
CSV y JSON, con los importes y las comisiones de cada operación. Ese reporte es la fuente externa
contra la cual se contrasta.

**Por qué no alcanza con la bitácora**: la bitácora registra lo que nosotros supimos. Una notificación
que nunca llegó, un pago acreditado fuera de banda, o una devolución procesada del lado de Mercado
Pago son huecos que por definición no están en nuestros datos — y solo aparecen comparando contra los
suyos. Una plataforma de cobros sin ese contraste no puede afirmar que sus números son correctos,
solo que son consistentes consigo mismos.

El alcance de este change es **poder hacer la comparación**, no automatizarla: el reporte se descarga
y se contrasta contra la bitácora y los movimientos, con un procedimiento escrito. Automatizar la
conciliación periódica es trabajo posterior, y va a las notas abiertas.

### D12 — La notificación dice en qué cuenta entró el dinero, y eso se contrasta

El payload de una notificación de pago trae `user_id`, que identifica al **vendedor** en cuyo cuenta
se procesó el cobro. Con un solo receptor ese dato no aporta nada; con N fotógrafos vinculados pasa a
ser un control cruzado gratis.

La regla: al procesar una notificación, el `user_id` informado debe coincidir con la cuenta vinculada
del tenant dueño del pedido. Si no coincide, el pedido **no se acredita**: va a `RevisionManual` con
ambos identificadores en la bitácora.

**Qué detecta que ninguna otra defensa detecta**: que el dinero de un pedido haya entrado en la cuenta
equivocada. Las defensas de D8 previenen que eso ocurra —credencial por tenant, resuelta en cada
llamada—, pero previenen sin verificar. Este control verifica el hecho consumado, que es la única
forma de enterarse de un error de configuración, de una vinculación cruzada, o de un bug que las
defensas preventivas no cubrieron.

Es la misma lógica por la que el monto se verifica aunque el sistema lo haya fijado: **lo que se
afirma se comprueba contra la fuente**, sobre todo cuando el costo de comprobarlo es comparar dos
números que ya están en la mano.

### D13 — Reembolsar el pago no revierte la comisión; cancelar el pedido sí

El devengamiento se revierte cuando la **venta** se deshace, no cuando el **dinero** vuelve. Son cosas
distintas y el sistema las trata distinto:

| Situación | Devolución a la familia | Comisión de la plataforma |
|---|---|---|
| Pagado, pedido **no entregado**, se cancela | Total | Se revierte — la venta se deshizo |
| Pagado, pedido **ya entregado**, el fotógrafo devuelve igual | Decisión suya | **No** se revierte — la venta existió |
| Reembolso del pago **sin cancelar** el pedido | Asunto del fotógrafo | **No** se revierte |
| Contracargo | Forzada por el emisor de la tarjeta | **No** se revierte — el pedido sigue vigente |

**Qué previene la tercera fila**: es el agujero de la transferencia con un paso más. El fotógrafo
confirma el pedido, la familia paga por Mercado Pago, el split retiene la comisión, y después el
fotógrafo reembolsa y le pide una transferencia. Si el reembolso revirtiera el devengamiento, la
comisión se evapora y la venta ocurrió igual. Aparecería sola con la implementación obvia —"llega
notificación de reembolso, revertir la comisión"— y por eso se escribe antes de que alguien la
escriba así.

**La frontera entregado / no entregado ya existe**: D7 de `pagos-mercado-pago` separa el estado de
cobro del de fulfillment justamente para poder preguntar esto.

**Lo que no controlamos**: si Mercado Pago devuelve el `marketplace_fee` por su cuenta ante un
reembolso, las últimas tres filas no son ejecutables del lado del proveedor —nos sacan la plata
igual—. No es un problema: la comisión sigue devengada y vuelve a **saldo pendiente**, que se liquida
contra ventas siguientes. Es exactamente para lo que existe la cuenta corriente (D2). Lo que cambia no
es cuánto se cobra sino cuándo, y eso lo mide la tarea 8.7.

## Threat model

Suma al de `pagos-mercado-pago`; no lo reemplaza.

| # | Amenaza | Defensa | Decisión |
|---|---|---|---|
| M1 | Filtración de la base con credenciales de N fotógrafos | Cifrado con clave fuera de la base | D5 |
| M2 | Vincular una cuenta ajena a un tenant, o la propia a un tenant ajeno | Valor de un solo uso atado al tenant, con vencimiento | D4 |
| M3 | Reutilizar un retorno de autorización capturado | El valor se consume al usarse | D4 |
| M4 | Credencial de un tenant usada para operar sobre otro | Resolución por tenant en cada llamada, sin estado compartido | D8 |
| M5 | Cobrar en la cuenta equivocada | La preferencia se crea con la credencial del dueño del evento | D8 |
| M6 | Manipular la comisión desde el cliente | La tasa y el cálculo viven en el servidor; el cliente no aporta ninguno | D1 |
| M7 | Evadir la comisión empujando transferencia | El devengamiento no depende del método de pago | D1 |
| M8 | Un fotógrafo consultando el saldo de otro | La cuenta corriente se scopea por tenant como cualquier entidad | — |
| M9 | Alterar el saldo directamente | El saldo se deriva de movimientos inmutables | D2 |
| M10 | Credencial expuesta en logs o respuestas de error | Nunca se registran ni se devuelven | D5 |
| M11 | Interceptar el `code` en el retorno de la autorización y canjearlo | PKCE con `S256`: sin el `code_verifier` el `code` no sirve | D3 |
| M12 | Vinculación revocada por el fotógrafo que sigue figurando activa | Notificación `mp-connect` además del fallo de renovación | D6 |
| M13 | Vinculaciones que caen todas juntas a los 180 días | `scope=offline_access` desde la primera autorización + renovación proactiva con margen | D3, D6 |
| M14 | Cobros que ocurrieron y el sistema nunca registró | Contraste contra el reporte de ventas de Split del proveedor | D11 |
| M15 | Dinero acreditado en la cuenta de un fotógrafo que no es el dueño del pedido | El `user_id` de la notificación se contrasta contra la cuenta vinculada del tenant; si no coincide, `RevisionManual` | D12 |
| M16 | Comisión mayor al total, rechazada de forma opaca por el proveedor | Se valida antes de enviar que no supere el monto de la operación | D10 |

## Risks / Trade-offs

- **El KYC nivel 6 de la cuenta de la plataforma puede demorar y no depende de nosotros** → D7: la
  capa se despliega dormida y el sistema funciona igual con un receptor. Ningún trabajo se descarta.
  Confirmado que el 1:1 no requiere aprobación comercial, el riesgo se reduce a ese trámite, pero el
  interruptor se queda por las razones que D7 agrega.
- **`scope=offline_access` es un parámetro que se pide una sola vez y decide seis meses después** → Si
  se omite, no hay renovación posible y todas las vinculaciones caen juntas al vencer. Se cubre con un
  test que verifica que la URL de autorización lo incluye, y con la verificación en sandbox de que el
  refresh efectivamente encadena (D6).
- **La cuenta corriente es contabilidad, y la contabilidad mal hecha se paga cara** → D2 la vuelve
  reconstruible: ante una discrepancia siempre se puede recalcular desde los movimientos y comparar.
- **Cobrar comisión sobre pedidos pagados por transferencia exige confianza en el dato del pedido** →
  Si un fotógrafo registrara pedidos falsos o los cancelara para evadir, el devengamiento lo sigue
  (las cancelaciones revierten). Detectar abuso sistemático es una capa de negocio posterior, no de
  este change.
- **La comisión se suma a la de Mercado Pago y el fotógrafo pierde más por venta** → Es una decisión
  comercial, no técnica. El sistema la hace visible: el fotógrafo puede ver exactamente cuánto pagó de
  comisión y contra qué ventas.
- **El único canal de pérdida real de la plataforma es el saldo incobrable** → Ninguna de las
  situaciones de D13 hace perder dinero: la comisión se conserva, o se convierte de retenida en por
  cobrar. La plataforma pierde solo cuando no puede cobrar ese saldo — el fotógrafo que lo acumula y
  deja de operar. Cada reembolso posterior a la entrega y cada contracargo lo engordan, así que la
  política de liquidación deja de ser una pregunta teórica y pasa a tener una fuente concreta de
  volumen. Ahí van las defensas, no en el manejo del reembolso.
- **Descalce de caja, que no es pérdida** → Si Mercado Pago debita el `marketplace_fee` de la cuenta
  de la plataforma y el dinero ya se retiró, el saldo queda en negativo hasta la venta siguiente.
- **Guardar credenciales de terceros implica responsabilidad legal sobre ellas** → Fuera del alcance
  técnico, pero conviene que la relación con cada fotógrafo lo contemple por escrito.

## Migration Plan

1. Verificar los requisitos reales de Split de Pagos 1:1 y completar lo que pidan —verificación de
   identidad de la cuenta, creación de la aplicación—. Corre en paralelo a todo lo demás.
2. Modelo y migración: cuenta de cobro, credenciales cifradas, movimientos de cuenta corriente.
3. Flujo de autorización completo, verificado contra el ambiente de pruebas de Mercado Pago.
4. Split en la creación de preferencias, detrás del interruptor de marketplace.
5. Cuenta corriente y liquidación.
6. Verificación real del split — **requiere la aprobación**; si no llegó, se posterga solo este paso.
7. **Rollback**: apagar el interruptor de marketplace devuelve el sistema al comportamiento de
   `pagos-mercado-pago`. Las credenciales vinculadas quedan y no se usan. Los movimientos de cuenta
   corriente se conservan.

## Open Questions

- **Tasa de comisión**: el número es una decisión comercial que no bloquea el diseño — la tasa es
  configurable y admite excepciones por tenant desde el día uno. Conviene definirla mirando lo que
  cobran FullFoto y mifotoar, que ya está anotado como ítem de relevamiento en
  `docs/05-notas-abiertas.md`.
- **Comisión real de Mercado Pago, plazo de acreditación y retenciones provinciales**: Mercado Pago
  descuenta primero, así que definen qué remanente queda y con eso qué recibe realmente el fotógrafo
  (D10). Son insumo directo de la tasa y de cómo se le explica. Relevamiento en la tarea 0.4.
- **Segundo proveedor de pagos**: no es un objetivo de este change y no se diseña para él. Lo que se
  conserva es la costura —el puerto `IPasarelaPagos`, con el modelo de cobro, la bitácora y la cuenta
  corriente escritos sin nombrar proveedor—, de modo que incorporar otro sea agregar un adaptador y no
  rehacer el dominio. Y como la comisión se devenga sobre el pedido (D1), el ingreso de la plataforma
  no depende de que el proveedor soporte split: uno que no lo soportara se salda por cuenta corriente.
  Deliberadamente **no** se construye una abstracción genérica de proveedores: esa abstracción sale
  mal cuando se diseña sin el segundo caso a la vista.
- **Qué política de devolución se le ofrece a la familia**: la postura por defecto es la más
  restrictiva que la ley permita —una impresión de la foto de un chico determinado es un producto a
  medida, y esos suelen quedar fuera de la garantía de devolución—, pero hay que confirmarlo con
  contador o abogado contra el derecho de arrepentimiento (art. 34, Ley 24.240) y relevar qué admiten
  FullFoto y mifotoar (tarea 0.3). **La política es independiente de la capacidad**: aunque no se
  ofrezca devolución va a haber contracargos, que son involuntarios, y fotógrafos que decidan devolver
  por su cuenta. El sistema tiene que soportar la reversión aunque la política no la ofrezca — acá se
  define qué se promete, no qué se implementa.
- **Política de liquidación del saldo pendiente**: cada cuánto se reclama, si hay un mínimo para
  facturar, qué pasa si un fotógrafo deja de operar con saldo. Son reglas de negocio que se van a
  aclarar con el segundo fotógrafo real; el modelo de movimientos las soporta sin cambios.
- **Qué pasa con la comisión ya retenida ante un reembolso o un contracargo** — la pregunta abierta
  más importante de este change, y la única que no pudo responderse por documentación ni consultando
  al asistente de Mercado Pago. Si un pago con split se reembolsa total o parcialmente, ¿Mercado Pago
  devuelve automáticamente el `marketplace_fee` y de qué cuenta lo saca? ¿Y ante un contracargo, quién
  lo absorbe? Del resultado depende si la reversión de la comisión es un movimiento que registramos o
  uno que además tenemos que ejecutar. **Se determina con pruebas reales en sandbox** (tareas 8.7 y
  8.8) antes de definir la política de liquidación, porque un reembolso mal contabilizado deja a la
  plataforma cobrando comisión sobre una venta que se deshizo.
- **Cadencia de reintentos, idempotencia en preferencias y normalización del manifest de la firma**:
  heredadas de `pagos-mercado-pago`; ver sus Open Questions. Acá solo agrega que el manifest con
  `data.id` alfanumérico es un caso que este change sí ejercita, por el tópico `mp-connect`.
