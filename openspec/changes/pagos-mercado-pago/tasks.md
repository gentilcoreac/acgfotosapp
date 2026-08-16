## 0. Prerrequisito de Alberto — cuenta y credenciales de Mercado Pago

Bloquea los grupos 3 a 5 y 11. No lo puede hacer el asistente: son pasos en el panel de Mercado Pago
con la identidad del negocio.

- [ ] 0.1 Cuenta de Mercado Pago del negocio (la personal sirve para empezar; se puede separar después)
- [ ] 0.2 Crear la aplicación en el panel de desarrolladores — de ahí salen las credenciales
- [ ] 0.3 Obtener las credenciales de **TEST** (sandbox): Access Token y Public Key. Con estas se desarrolla todo
- [ ] 0.4 Crear los usuarios de prueba: hacen falta **dos cuentas distintas**, una vendedora y una compradora — Mercado Pago no permite probar con la misma. Nunca usar tarjetas reales
- [ ] 0.5 Revisar en el panel la comisión y el plazo de acreditación reales para la provincia del negocio, y anotarlos en el documento funcional del grupo 13
- [ ] 0.6 Generar la clave secreta de webhook — **este paso va después del 11.1**, porque necesita la URL del túnel ya levantada
- [ ] 0.7 Credenciales de **PRODUCCIÓN**: recién al momento del deploy real, no antes

## 1. Modelo: separar cobro de fulfillment

- [ ] 1.1 Agregar `EstadoPago` (`SinPagar`/`Iniciado`/`AcreditandoMP`/`Pagado`/`Rechazado`/`Reembolsado`/`RevisionManual`) y `OrigenPago` (`MercadoPago`/`Manual`) en `AcgFotos.Fotos.Domain/Entities`
- [ ] 1.2 Agregar entidad `PagoPedido` (hija de `Pedido`): origen, medio, monto, moneda, estado resultante, referencia externa del proveedor, instante, usuario autor cuando es manual
- [ ] 1.3 Agregar a `Pedido` los campos `EstadoPago` y `ReferenciaPagoPublica`; retirar `MercadoPagoPreferenciaId` si queda cubierto por la bitácora
- [ ] 1.4 Implementar la función de precedencia que pliega la bitácora al `EstadoPago` del pedido (D1), con tests unitarios de cada orden posible incluidas notificaciones fuera de orden
- [ ] 1.5 Generar `ReferenciaPagoPublica` al confirmar el pedido con un generador criptográficamente seguro: 32 bytes en **base64url sin relleno** (alfabeto `A-Za-z0-9-_`, sin `+`, `/` ni `=` — lo exige Mercado Pago, ver D3), con índice único. Test que verifica el alfabeto y el largo ≤ 64
- [ ] 1.6 EF config de `PagoPedido` (`fot_PagosPedido`) con índice único sobre (referencia externa del proveedor, estado resultante) — es el mecanismo de idempotencia de D2
- [ ] 1.7 Migración EF con backfill según el plan de migración de design.md, y `Down` completo
- [ ] 1.8 Remover `EstadoPedido.Pagado` y ajustar todo el código que lo referencia (AppServices, mappers, criterias, lista de impresión, seeds)
- [ ] 1.9 Devengar la comisión de la plataforma al confirmar el pedido, con la tasa vigente del tenant congelada junto al devengamiento — tasa **cero** por defecto (D8c). El concepto existe desde ahora para que encenderlo después sea cambiar un número, no migrar pedidos históricos
- [ ] 1.10 Aplicar la migración a la base de dev con backup previo y autorización explícita de Alberto

## 1b. Contacto de quien compra y trazabilidad del QR

- [ ] 1b.1 Contacto opcional en `Participante` (nombre de contacto, teléfono, correo): campos, migración y ABM de grupos/participantes. `Participante.Nombre` —la persona fotografiada— se mantiene como dato distinto del nombre de contacto
- [ ] 1b.2 `Pedido.Email` obligatorio junto a nombre y teléfono, con validación en la confirmación; el pedido conserva copia de los tres
- [ ] 1b.3 Endpoint de la sesión de familia que informa **solo** si hay contacto guardado y su versión enmascarada — nunca los valores completos (D12b-bis)
- [ ] 1b.4 Confirmación con la bandera "usar los datos guardados": el servidor los toma del participante y el cliente nunca los recibió
- [ ] 1b.5 Completar el contacto del participante solo si estaba vacío; si ya tenía, el pedido guarda lo suyo y la ficha no se toca (D12b-ter)
- [ ] 1b.5b En el detalle del pedido, señalar cuando los datos usados difieren de los cargados en el participante, para que el fotógrafo decida si actualiza la ficha
- [ ] 1b.6 Front: paso de confirmación que muestra la pista enmascarada con acción "Cambiar", y el motivo a la vista ("para enviarte el comprobante y seguir tu pedido")
- [ ] 1b.7 `Pedido.CodigoAccesoId`: registrar con qué código se abrió la sesión que originó el pedido; sumar el claim correspondiente al token de familia
- [ ] 1b.8 Corregir la atribución del pedido en sesiones multi-participante: hoy `FamiliaPedidoAppService` toma `FamiliaParticipanteIds.First()` arbitrariamente y en una sesión de dos hermanos el pedido se atribuye a uno solo
- [ ] 1b.9 Backfill de los pedidos existentes en dev: quedan sin contacto completo, y la obligatoriedad aplica solo a los nuevos
- [ ] 1b.10 Test: la respuesta de la sesión de familia no contiene el correo ni el teléfono completos por ningún camino
- [ ] 1b.11 Test: confirmar con "usar los guardados" produce un pedido con los datos correctos sin que el cliente los haya enviado

## 1c. Comprobante por correo

- [ ] 1c.1 Envío **asincrónico** del comprobante al confirmar el pedido: se encola, no se manda dentro de la transacción de confirmación
- [ ] 1c.2 Un fallo de envío no impide ni revierte la compra; se reintenta sin intervención
- [ ] 1c.3 Contenido: detalle del pedido y enlace de seguimiento armado con la referencia opaca (reusa el mecanismo de D6, no agrega uno nuevo)
- [ ] 1c.4 El enlace no da acceso a fotos ni a otros datos del evento: solo estado del pedido
- [ ] 1c.5 Test: con el servicio de correo caído, el pedido se confirma igual y el envío queda pendiente de reintento

## 2. Cobro manual y admin de pedidos (backend)

- [ ] 2.1 Agregar a `PedidoAppService` la operación de registrar cobro manual (medio, monto, confirmación forzada), con `FamiliaSessionGuard.EnsureNoFamiliaSession` al inicio
- [ ] 2.2 Devolver advertencia y exigir confirmación explícita cuando el pedido ya figura pagado o tiene un pago de MP en curso
- [ ] 2.3 Verificar que `CambiarEstadoAsync` (fulfillment) ya no infiere ni toca estado de pago
- [ ] 2.4 Agregar `EstadoPago` al criterio de búsqueda, al header y al detalle de pedidos, incluida la bitácora en el detalle
- [ ] 2.5 Endpoint admin para registrar el cobro manual, con su ruta en `PedidoController`

## 3. Infraestructura de Mercado Pago

- [ ] 3.1 Sección `Fotos:MercadoPago` en `appsettings.json` con lo no sensible: `Enabled`, moneda, ventana de frescura de firma, URLs de retorno. Sin secretos
- [ ] 3.2 Leer Access Token y secreto de webhook desde user-secrets/variables de entorno, como credenciales independientes. **No** en `appsettings.Development.json`, aunque la guía de Mercado Pago lo sugiera (D9)
- [ ] 3.3 Validación al arranque: con `Enabled=true` y credenciales ausentes, fallar de forma explícita
- [ ] 3.4 Servicio de resolución de credenciales que recibe el tenant y hoy lee configuración (D8b) — ningún call site toma el token de una constante global
- [ ] 3.5 Cliente HTTP tipado (`IHttpClientFactory`) con timeout y reintentos acotados: crear preferencia y consultar pago. El token viaja en el header de cada request, nunca en estado compartido
- [ ] 3.6 Verificar que ni el Access Token ni el secreto aparecen en logs ni en respuestas de error, incluido el camino de excepción del cliente HTTP
- [ ] 3.7 Documentar en `docs/credenciales-dev.md` qué secretos hacen falta y cómo cargarlos, sin valores

## 4. Inicio de pago (familia)

- [ ] 4.1 AppService de pago: resolver el pedido **solo** por los claims de la sesión de familia, nunca por dato del cliente
- [ ] 4.2 Construir la preferencia con el `Total` almacenado, moneda de configuración, `external_reference` = referencia opaca, y cuotas con interés a cargo de quien paga (D10)
- [ ] 4.3 Enviar `X-Idempotency-Key` en la creación de la preferencia y reusar la preferencia vigente en vez de crear una nueva por cada click
- [ ] 4.4 Registrar el intento en la bitácora (`Iniciado`) y devolver únicamente la URL de pago
- [ ] 4.5 Rechazar el inicio de pago si el pedido ya está `Pagado`, y si `Enabled=false` responder como no disponible
- [ ] 4.6 Endpoint `[AllowFamiliaSession]` en un controller nuevo de pagos de familia

## 5. Webhook de notificaciones (núcleo de seguridad)

- [ ] 5.1 Endpoint anónimo de notificaciones que rechaza todo request con cabecera `Authorization` (T12)
- [ ] 5.2 Verificación de firma HMAC con comparación en tiempo constante (T3)
- [ ] 5.3 Rechazo por ventana de frescura sobre el instante firmado (T4)
- [ ] 5.4 Consulta server-to-server `GET /v1/payments/{id}` como única fuente de verdad del estado (T5)
- [ ] 5.5 Resolución `ReferenciaPagoPublica` → `(TenantId, PedidoId)` como proyección a un record de dos `long`, única consulta cross-tenant (D4 ④)
- [ ] 5.6 `SetSystemContext(tenantId, ...)` inmediatamente después de resolver, siguiendo el patrón de `FotoProcesamientoWorker` (D4 ⑤)
- [ ] 5.7 `TenantScopeGuard.EnsureTenantScoped(appContext)` al inicio del AppService de procesamiento, que lanza si el contexto sigue anónimo (D4 ⑥)
- [ ] 5.8 Verificación de monto y moneda contra el pedido; discrepancia → `RevisionManual` con ambos montos en la bitácora (T6)
- [ ] 5.9 Escritura idempotente de la bitácora: traducir la violación de índice único a "ya procesada" y responder 200 (D2)
- [ ] 5.10 Manejo de `pending`/`in_process` → `AcreditandoMP`, y de devolución/contracargo → `Reembolsado`
- [ ] 5.13 Pago acreditado sobre un pedido `Cancelado` → `RevisionManual` sin reactivar el pedido (D12)
- [ ] 5.11 Ante fallo de la consulta a Mercado Pago, responder con error para que el proveedor reintente — nunca descartar la notificación
- [ ] 5.12 Persistir solo lo necesario para conciliar; no volcar el cuerpo completo de la respuesta ni datos personales de quien paga (T14)

## 6. Consulta de estado de pago

- [ ] 6.1 Consulta con sesión de familia vigente, resuelta por claims
- [ ] 6.2 Consulta por referencia opaca para cuando la sesión ya venció: devuelve **solo** el estado de pago, sin acceso a fotos ni datos del evento (D6)
- [ ] 6.3 Respuesta indistinguible entre referencia inexistente y ajena

## 7. Rate limiting

- [ ] 7.1 Política `pago` por IP en `RateLimitingHelper`, siguiendo el patrón de `CanjePolicy`, aplicada a inicio de pago y consulta por referencia
- [ ] 7.2 Excluir el webhook del limitador global para que los reintentos legítimos de Mercado Pago no reciban 429 (D11)
- [ ] 7.3 Documentar los valores nuevos en el comentario de `RateLimiting` de `appsettings.json`

## 8. Frontend — familia

- [ ] 8.1 Paso de pago en `/carrito`: elegir entre pagar ahora o coordinar el cobro con el fotógrafo
- [ ] 8.2 Redirección al checkout de Mercado Pago con la URL devuelta por el backend (D13)
- [ ] 8.3 Vista de retorno de pago que muestra "verificando" y consulta el estado real al backend — nunca afirma pago confirmado por haber llegado a la URL (T10)
- [ ] 8.4 Reflejar el estado acreditado cuando la verificación se completa, y el caso de pago en acreditación pendiente
- [ ] 8.5 Ocultar el pago online cuando el módulo está deshabilitado

## 9. Frontend — admin de pedidos

- [ ] 9.1 Estado de pago como columna propia y filtro propio, distinto del estado de fulfillment
- [ ] 9.2 Bitácora de cobros en el detalle del pedido
- [ ] 9.3 Acción de registrar cobro manual (efectivo/transferencia) con el diálogo de confirmación para los casos de conflicto
- [ ] 9.4 Revisar que la lista de impresión y sus filtros sigan correctos tras la separación de estados

## 10. Tests de regresión de seguridad

- [ ] 10.1 IDOR: una sesión de familia no puede iniciar el pago de un pedido de otro participante (T1)
- [ ] 10.2 Monto: un campo de monto en el request se ignora; la preferencia sale por el `Total` almacenado (T2)
- [ ] 10.3 Firma: notificación sin firma y con firma inválida se rechazan sin tocar datos (T3)
- [ ] 10.4 Replay: notificación con firma válida fuera de la ventana de frescura se rechaza (T4)
- [ ] 10.5 Payload mentiroso: notificación firmada que declara aprobado contra una consulta que devuelve rechazado — el pedido no se marca pagado (T5)
- [ ] 10.6 Monto discrepante y moneda distinta → `RevisionManual`, no `Pagado` (T6)
- [ ] 10.7 Idempotencia: la misma notificación tres veces deja una sola acreditación y una sola entrada (T7)
- [ ] 10.8 Concurrencia: dos notificaciones simultáneas del mismo pago producen una sola acreditación (T7)
- [ ] 10.9 Monotonía: notificación tardía de estado anterior no retrocede el estado de pago (T8)
- [ ] 10.10 Cross-tenant: una notificación de un pedido del tenant A no lee ni modifica datos de otro tenant; test que falla si se remueve el `SetSystemContext` (T9)
- [ ] 10.11 Contexto: notificación con cabecera `Authorization` se rechaza (T12)
- [ ] 10.12 Referencia opaca: la consulta por referencia devuelve solo estado de pago y no da acceso a fotos ni al álbum (T11)
- [ ] 10.13 `FamiliaSessionGuard`: una sesión de familia no puede registrar un cobro manual ni consultar el admin de pagos (T16)
- [ ] 10.14 Rate limiting: el inicio de pago excedido responde 429 (T15)
- [ ] 10.15 Secretos: el error del proveedor no incluye la credencial en mensaje ni en log (T13)
- [ ] 10.18 Referencia opaca: el valor generado usa solo el alfabeto que acepta Mercado Pago y no supera los 64 caracteres (D3)
- [ ] 10.19 Contacto guardado: una sesión de familia con un código ajeno no puede leer el teléfono ni el correo de esa familia por ningún endpoint (D12b-bis)
- [ ] 10.16 Migración: los pedidos que estaban en `Pagado` quedan con fulfillment `Pendiente`, pago `Pagado` y su entrada de bitácora
- [ ] 10.17 Pedido cancelado: un pago acreditado lo deja en `RevisionManual` y no lo reactiva (D12)

## 11. Verificación manual end-to-end

- [ ] 11.1 Levantar túnel público (cloudflared) apuntando a la API dev y configurar la URL de notificaciones en el panel de Mercado Pago
- [ ] 11.2 Flujo completo con credenciales de prueba: confirmar pedido → iniciar pago → pagar → notificación → pedido acreditado
- [ ] 11.3 Caso de pago rechazado y reintento exitoso: la bitácora conserva ambos y el estado final es `Pagado`
- [ ] 11.4 Caso de pago pendiente de acreditación: el pedido figura en curso, no cobrado
- [ ] 11.5 Navegar a mano a la URL de retorno exitoso sin pagar y confirmar que el pedido no cambia de estado
- [ ] 11.6 Cobro manual en efectivo desde el admin, incluido el caso con pago de MP en curso
- [ ] 11.7 Bajar el túnel al terminar

## 12. Suites y documentación

- [ ] 12.1 Suite de integración del backend en verde
- [ ] 12.2 Suite unit del frontend + lint + build en verde
- [ ] 12.3 ADR nuevo en `docs/04-decisiones.md`: estado de pago derivado de la bitácora y separado del fulfillment (D1)
- [ ] 12.4 ADR nuevo en `docs/04-decisiones.md`: aislamiento de tenant en el webhook — lookup estrecho, contexto de sistema y guard que lo exige (D4)
- [ ] 12.5 ADR nuevo en `docs/04-decisiones.md`: la notificación autentica al emisor, la API dice qué pasó (D5) y el retorno del navegador no acredita (D6)
- [ ] 12.6 Actualizar ADR-08 con lo que efectivamente se implementó, y ADR-12 con la separación cobro/fulfillment
- [ ] 12.7 Tildar los ítems correspondientes de Fase 3 en `docs/03-fases.md`
- [ ] 12.8 Anotar en `docs/05-notas-abiertas.md` las fases posteriores: seña/pago parcial, y cuotas sin interés vía convenio bancario o precio de lista con descuento por transferencia
- [ ] 12.9 Anotar en `docs/05-notas-abiertas.md` las cuentas de comprador con el diseño ya resuelto para que no se re-discuta: identidad **separada de `Usuario`** (que es multi-tenant y modela a quien trabaja en la organización, no a su cliente), cross-tenant por diseño, anclada al correo del pedido — que es lo que permitirá vincular las compras anteriores sin perder historia
- [ ] 12.10 Anotar en `docs/05-notas-abiertas.md` la entregabilidad del correo como decisión de deploy: con el comprobante formando parte de la experiencia de compra, que el mensaje no caiga en spam pasa a ser requisito
- [ ] 12.11 Anotar en `docs/05-notas-abiertas.md` la detección de códigos en circulación: un mismo código canjeado desde muchos orígenes distintos no prueba nada por sí solo, pero avisarle al fotógrafo le permitiría desactivarlo. No previene el uso del código —eso requeriría autenticar familias, que está descartado— solo lo hace visible

## 13. Documento funcional del proceso de cobro

Explicativo y en lenguaje de negocio, no técnico. Es el documento al que se vuelve cuando algo no
cierra y hay que entender qué pasó, sin leer código.

- [ ] 13.1 Crear `docs/06-cobros.md` con el circuito completo contado de punta a punta: qué hace la familia, qué ve, qué recibe el fotógrafo y en qué momento
- [ ] 13.2 Documentar las dos dimensiones del pedido (cobro y fulfillment) con ejemplos de las combinaciones reales: entregado sin cobrar, cobrado sin imprimir, pago en acreditación
- [ ] 13.3 Documentar cada estado de pago: qué significa, cómo se llega, qué hacer cuando aparece — con foco en `AcreditandoMP` y `RevisionManual`, que son los que van a generar preguntas
- [ ] 13.4 Documentar el cobro en efectivo y transferencia: cuándo conviene, cómo se registra, qué queda auditado
- [ ] 13.5 Costos reales: comisión de Mercado Pago para la provincia del negocio, plazo de acreditación elegido, y que las cuotas las financia quien paga (del relevamiento de 0.5)
- [ ] 13.6 Guía de conciliación: cómo responder "¿este pedido está cobrado y por qué?" usando la bitácora
- [ ] 13.7 Qué hacer ante los casos raros: pago sobre pedido cancelado, monto que no coincide, devolución, pago que nunca se acredita
- [ ] 13.8 Requisitos de producción de Mercado Pago: HTTPS con certificado válido en las URLs de webhook y retorno, sin `localhost` ni IP — enlazar con el ítem de dominio del deploy
