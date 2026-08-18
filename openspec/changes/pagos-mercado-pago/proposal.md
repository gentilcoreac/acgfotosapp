## Why

Hoy un pedido confirmado es un compromiso sin cobro: la familia elige fotos y el fotógrafo cobra al
entregar (ADR-08 lo dejó explícitamente para Fase 3). Eso obliga a coordinar cobro y entrega en el
mismo acto — justo la fricción que el proyecto quiere eliminar. Fase 3 agrega el pago online con
Mercado Pago Checkout Pro para que la familia pueda pagar en el momento de pedir, sin sacarle al
fotógrafo la opción de cobrar en efectivo o por transferencia (que no tiene comisión).

Un módulo de pagos es la superficie más sensible de la aplicación: un webhook público que mueve el
estado de cobro, un token que mueve plata real, y un flujo donde el atacante controla el navegador y
la vuelta del pago. Por eso este change trata la seguridad como requisito funcional, no como
revisión posterior: cada defensa es un requirement verificable con su test de regresión.

Además salda una deuda de modelado que hoy ya duele y con pagos online se vuelve insostenible:
`EstadoPedido` mezcla dos dimensiones independientes — si la familia pagó, y si el pedido se imprimió
y entregó. El propio código lo comenta ("con pago en efectivo el pedido salta directo a Impreso"),
que es la forma de decir que se pierde información de cobro para poder expresar la de fulfillment.
Con MP aparecen estados de pago que no tienen ningún equivalente en fulfillment (acreditando,
rechazado, reembolsado) y el enum único deja de alcanzar.

## What Changes

- **BREAKING (modelo)**: `EstadoPedido` deja de representar el cobro y queda solo para fulfillment
  (`Pendiente`/`Impreso`/`Entregado`/`Cancelado`); se elimina el miembro `Pagado`. Nace `EstadoPago`
  en `Pedido` (`SinPagar`/`Iniciado`/`AcreditandoMP`/`Pagado`/`Rechazado`/`Reembolsado`/
  `RevisionManual`). Migración EF con backfill de los pedidos existentes.
- Nace `PagoPedido`, entidad hija **inmutable** de `Pedido`: bitácora de intentos de cobro. Cada
  notificación verificada de Mercado Pago y cada marcado manual del fotógrafo deja una fila con
  origen, monto, estado, referencia externa, quién y cuándo. Es el rastro de conciliación.
- Pago online por **Checkout Pro**: endpoint de familia que crea la preferencia con el monto leído de
  la base (nunca del cliente) y devuelve la URL de pago; **cuotas con interés a cargo de la familia**
  (costo extra cero para el fotógrafo).
- Webhook público de Mercado Pago con firma HMAC verificada, anti-replay, idempotente, y —clave— que
  **nunca decide con el cuerpo de la notificación**: siempre reconsulta el pago server-to-server y
  decide con esa respuesta. La respuesta al proveedor distingue lo definitivo (200) de lo transitorio
  (5xx), para que el reintento de Mercado Pago funcione como garantía de entrega en vez de perderse.
- Idempotencia en las llamadas **salientes** con `X-Idempotency-Key` derivada del pedido y el intento,
  para que un reintento tras un timeout no genere un segundo cobro.
- **Aislamiento multi-tenant explícito en el webhook**: el filtro global de tenant se apaga en
  requests anónimos (hallazgo confirmado en código, ver design.md), así que el webhook resuelve el
  tenant por una referencia opaca y establece contexto de sistema antes de tocar cualquier dato.
- Marcado manual de cobro (efectivo/transferencia) desde el admin de pedidos, separado del cambio de
  estado de fulfillment, con auditoría de autor y momento. Hoy `CambiarEstadoAsync` no registra ni
  `MedioPago` ni `PagadoEn`.
- Front: paso de pago en el carrito (pagar ahora / coordinar con el fotógrafo), pantalla de retorno
  que **no asume que volver del checkout significa pago aprobado**, y la columna/filtro de estado de
  pago en el admin de pedidos.
- Nuevos ADRs en `docs/04-decisiones.md` por las decisiones estructurales; `docs/03-fases.md` y
  `docs/05-notas-abiertas.md` actualizados.

Fuera de alcance explícito (no diseñar acá): seña o pago parcial, cuotas sin interés subsidiadas por
el fotógrafo o por convenio bancario, y cualquier medio de pago que no sea Mercado Pago.

## Capabilities

### New Capabilities

- `estado-pago-pedido`: separación de la dimensión de cobro respecto de la de fulfillment en el
  pedido, bitácora inmutable de intentos de pago, y el registro manual de cobros en efectivo o
  transferencia con su auditoría.
- `pago-online-mercado-pago`: creación de la preferencia de Checkout Pro para un pedido de la sesión
  de familia, recepción y verificación de las notificaciones de pago, consulta del estado de pago por
  parte de la familia, y todas las defensas de seguridad del flujo (firma, anti-replay,
  idempotencia, verificación server-to-server, aislamiento de tenant, protección del secreto).

### Modified Capabilities

Ninguna: las capabilities existentes (`marca-agua`, `opciones-publicacion`, `regeneracion-derivados`,
`visor-fotos`) no cambian de comportamiento.

## Impact

**Backend**

- `AcgFotos.Fotos.Domain`: `EstadoPedido` (breaking), `Pedido`, nuevas `EstadoPago`, `PagoPedido`,
  `OrigenPago`; nuevo repositorio para la resolución de referencia de pago.
- `AcgFotos.Fotos.Application`: `FamiliaPedidoAppService` y `PedidoAppService` (cambio de estado y
  cobro manual pasan a ser operaciones distintas); nuevos servicios de pago y de verificación de
  notificaciones; puerto `IPasarelaPagos` en lenguaje de dominio, con su adaptador sobre el SDK
  oficial en `AcgFotos.Fotos.Infrastructure`.
- `AcgFotos.Fotos.Controllers`: nuevos endpoints de familia (`[AllowFamiliaSession]`), nuevo endpoint
  admin (`FamiliaSessionGuard`), y el webhook anónimo.
- `AcgFotos.Core/Security/RateLimitingHelper`: política nueva para el inicio de pago y tratamiento
  del webhook.
- `AcgFotos.Base.SqlMigrations`: migración de estados + `fot_PagosPedido`.
- Configuración: sección `MercadoPago` en `appsettings.json` **sin secretos**; Access Token y
  webhook secret por user-secrets en dev y variables de entorno/secret store en producción.

**Frontend**

- `features/familia`: paso de pago en `/carrito`, nueva vista de retorno de pago, consulta de estado.
- `features/fotos/pedidos`: estado de pago como dimensión propia (columna, filtro, detalle) y acción
  de registrar cobro manual.

**Dependencias**

- Paquete oficial `mercadopago-sdk` (3.5.0, net8.0+), usado **detrás de un puerto propio**
  `IPasarelaPagos` para que el dominio no vea tipos de terceros (D8). Aporta la validación de firma de
  webhook, el flujo OAuth que necesita el change siguiente, y credencial por request vía
  `RequestOptions` —lo que lo vuelve seguro en multi-tenant—. Un test de arquitectura prohíbe asignar
  el estático `MercadoPagoConfig.AccessToken`.
- Túnel HTTP público (ngrok/cloudflared) para recibir el webhook en desarrollo.

**Riesgo asumido**

Cambio breaking en un enum persistido, con migración de datos. Los pedidos de dev existentes se
backfillean; no hay datos productivos todavía, lo que hace este el momento más barato para el cambio.
