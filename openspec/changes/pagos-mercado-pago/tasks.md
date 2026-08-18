## Regla de verificación

Lo que este plan da por cierto sobre Mercado Pago sale de su documentación, de su asistente o de leer
el SDK — no de haberlo comprobado. Esas fuentes ya fallaron dos veces en cosas estructurales: el
asistente afirmó que Split de Pagos exigía aprobación comercial (el 1:1 es autoservicio) y que los
pagos de prueba no disparan notificaciones, cuando la documentación instruye lo contrario. El grupo 3b
es la forma institucionalizada de esta regla; las tareas sueltas marcadas con **⚠ verificar** abren su
grupo con el mismo criterio: se comprueba el dato y recién después se construye encima. Chequeo
acotado, no auditoría — lo que no depende del dato sigue avanzando.

## 0. Prerrequisito de Alberto — cuenta y credenciales de Mercado Pago

Bloquea los grupos 3 a 5 y 11. No lo puede hacer el asistente: son pasos en el panel de Mercado Pago
con la identidad del negocio.

- [ ] 0.1 Cuenta de Mercado Pago del negocio (la personal sirve para empezar; se puede separar después)
- [ ] 0.2 Crear la aplicación en el panel de desarrolladores — de ahí salen las credenciales
- [ ] 0.3 Obtener las credenciales de **TEST** (sandbox): Access Token y Public Key. Con estas se desarrolla todo
- [ ] 0.4 **⚠ verificar** si efectivamente hacen falta dos cuentas y si los pagos entre usuarios de prueba disparan notificaciones, o si hay que usar el simulador del panel: de eso depende qué puede probarse sin dinero real. Crear los usuarios de prueba: hacen falta **dos cuentas distintas**, una vendedora y una compradora — Mercado Pago no permite probar con la misma. Nunca usar tarjetas reales
- [ ] 0.5 Revisar en el panel la comisión y el plazo de acreditación reales para la provincia del negocio, incluidas las retenciones aplicables, y anotarlos en el documento funcional del grupo 13. Es el mismo relevamiento que la 0.4 de `marketplace-split-pagos` usa para dimensionar el remanente sobre el que se descuenta la comisión de la plataforma: se hace una vez y sirve a los dos changes
- [ ] 0.6 Generar la clave secreta de webhook — **este paso va después del 11.1**, porque necesita la URL del túnel ya levantada
- [ ] 0.7 Credenciales de **PRODUCCIÓN**: recién al momento del deploy real, no antes

## 1. Modelo: separar cobro de fulfillment

- [ ] 1.1 Agregar `EstadoPago` (`SinPagar`/`Iniciado`/`AcreditandoMP`/`Pagado`/`Rechazado`/`Reembolsado`/`RevisionManual`) y `OrigenPago` (`MercadoPago`/`Manual`) en `AcgFotos.Fotos.Domain/Entities`. `OrigenPago` **es** la dimensión "quién procesó el cobro": un segundo proveedor es un miembro más de este enum, no una migración (D15)
- [ ] 1.2 Agregar entidad `PagoPedido` (hija de `Pedido`): `OrigenPago`, `MedioPago`, monto, moneda, estado resultante, referencia externa del proveedor, instante, usuario autor cuando es manual — **las dos dimensiones en columnas separadas**
- [ ] 1.2b Dejar de mezclar en `MedioPago` lo que hoy mezcla (`Efectivo`/`MercadoPago`): `MercadoPago` es un origen, no un medio. Con Mercado Pago la familia paga con tarjeta, en Rapipago o con saldo en cuenta — mismo origen, medios distintos. `MedioPago` queda solo para el cómo (D15)
- [ ] 1.3 Agregar a `Pedido` los campos `EstadoPago` y `ReferenciaPagoPublica`; retirar `MercadoPagoPreferenciaId` si queda cubierto por la bitácora
- [ ] 1.4 Implementar la función de precedencia que pliega la bitácora al `EstadoPago` del pedido (D1), con tests unitarios de cada orden posible incluidas notificaciones fuera de orden
- [ ] 1.5 Generar `ReferenciaPagoPublica` al confirmar el pedido con un generador criptográficamente seguro: 32 bytes en **base64url sin relleno** (alfabeto `A-Za-z0-9-_`, sin `+`, `/` ni `=`), con índice único. Test que verifica el alfabeto y el largo. El límite de 64 caracteres es un supuesto no verificado en la documentación de Mercado Pago (D3): se respeta igual y se confirma en 3b.2
- [ ] 1.6 EF config de `PagoPedido` (`fot_PagosPedido`) con índice único sobre (referencia externa del proveedor, estado resultante) — es el mecanismo de idempotencia de D2
- [ ] 1.7 Migración EF con backfill según el plan de migración de design.md, y `Down` completo
- [ ] 1.8 Remover `EstadoPedido.Pagado` y ajustar todo el código que lo referencia (AppServices, mappers, criterias, lista de impresión, seeds)
- [ ] 1.9 Devengar la comisión de la plataforma al confirmar el pedido, con la tasa vigente del tenant congelada junto al devengamiento — tasa **cero** por defecto (D8c). El concepto existe desde ahora para que encenderlo después sea cambiar un número, no migrar pedidos históricos
- [ ] 1.9b Base de cálculo y redondeo de la comisión según D8c: sobre el `Total` congelado, en la moneda del pedido, a dos decimales **hacia abajo**, guardando importe y tasa juntos. Tests del redondeo con totales que dan más de dos decimales
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
- [ ] 3.5 Agregar el paquete `mercadopago-sdk` y definir el puerto `IPasarelaPagos` en `AcgFotos.Fotos.Application`, en lenguaje de dominio (crear enlace de pago, consultar cobro, validar notificación). Ningún tipo del SDK cruza hacia Application ni Domain
- [ ] 3.5b Adaptador del puerto en `AcgFotos.Fotos.Infrastructure` sobre el SDK: la credencial del tenant viaja en un `RequestOptions` **construido por llamada** —nunca compartido ni cacheado, porque el SDK lo muta (D8)— y `MercadoPagoConfig.AccessToken` no se asigna jamás
- [ ] 3.5c **⚠ verificar en el código del SDK** que el `HttpClient` por defecto es estático con timeout fijo de 30s y que `RequestOptions` se muta por llamada: las dos afirmaciones sostienen a 3.5b y 3.5d, y son comprobables leyendo la fuente. `IHttpClient` propio sobre `IHttpClientFactory` registrado en `MercadoPagoConfig.HttpClient`, con timeout y reintentos acotados nuestros (D8)
- [ ] 3.5d **Test de arquitectura** que falla si algún archivo de la solución asigna `MercadoPagoConfig.AccessToken`. Es la defensa que reemplaza a "acordarse siempre de pasar la credencial" (D8)
- [ ] 3.6 Verificar que ni el Access Token ni el secreto aparecen en logs ni en respuestas de error, incluido el camino de excepción del adaptador
- [ ] 3.7 Documentar en `docs/credenciales-dev.md` qué secretos hacen falta y cómo cargarlos, sin valores
- [ ] 3.8 Registro del módulo Autofac del vertical: puerto, adaptador, `IHttpClient` y serializador se configuran una sola vez al arranque; nada de configuración global tocada en tiempo de request

## 3b. Verificación en sandbox previa al handler

Bloquea el grupo 5. Son preguntas que la documentación de Mercado Pago no responde y que, contestadas
tarde, obligan a reescribir el núcleo del webhook.

- [ ] 3b.1 **Con qué secreto se firma la notificación**: confirmar con credenciales de prueba que la firma valida contra el secreto de la **aplicación** aunque la preferencia se haya creado con el token de un vendedor vía OAuth. De esto depende que el paso ② de D4 pueda ocurrir antes del ④; si hubiera un secreto por vendedor, hay que invertir ese orden y avisar antes de seguir
- [ ] 3b.2 Confirmar el límite real de largo y el alfabeto aceptado en `external_reference` mandando una referencia de 43 caracteres base64url y verificando que vuelve intacta en la consulta del pago (D3)
- [ ] 3b.3 Confirmar que `notification_url` de la preferencia tiene precedencia sobre la URL configurada en el panel, para no depender de la configuración de la cuenta de cada fotógrafo
- [ ] 3b.4 Medir la latencia real de `GET /v1/payments/{id}` en sandbox y fijar el timeout del paso ③ con margen frente al presupuesto de 22 segundos (D4)
- [ ] 3b.5 Determinar si el manifest de la firma requiere `data.id` en minúsculas: el `Normalize` del SDK solo hace `Trim`, así que si Mercado Pago firma en minúsculas su propio SDK falla con ids alfanuméricos. Se prueba con un tópico cuyo `data.id` traiga letras; la corrección, si hace falta, es minusculizar el argumento antes de pasarlo (D5)
- [ ] 3b.6 Verificar si `POST /checkout/preferences` respeta `X-Idempotency-Key` mandando dos veces la misma clave y comprobando si devuelve la misma preferencia o crea otra (D14). Cualquiera sea el resultado, la deduplicación propia queda
- [ ] 3b.7 Observar la cadencia real de reintentos devolviendo 5xx a propósito y anotando los intervalos, para calibrar cada cuánto corre el barrido de conciliación (D4)
- [ ] 3b.8 Dejar registrado en el documento funcional el resultado de cada verificación de este grupo, incluidas las que confirmen lo esperado — es lo que evita volver a preguntarlo dentro de seis meses

## 4. Inicio de pago (familia)

- [ ] 4.1 AppService de pago: resolver el pedido **solo** por los claims de la sesión de familia, nunca por dato del cliente
- [ ] 4.2 Construir la preferencia con el `Total` almacenado, moneda de configuración, `external_reference` = referencia opaca, y cuotas con interés a cargo de quien paga (D10)
- [ ] 4.3 `X-Idempotency-Key` **derivada** del pedido y del intento en la creación de la preferencia, de modo que el reintento de la misma operación repita la clave (D14). Una clave aleatoria por llamada compila y no protege de nada — test que verifica que dos intentos de la misma operación producen la misma clave
- [ ] 4.3b Reusar la preferencia vigente del pedido en vez de crear una nueva por cada clic
- [ ] 4.4 Registrar el intento en la bitácora (`Iniciado`) y devolver únicamente la URL de pago
- [ ] 4.5 Rechazar el inicio de pago si el pedido ya está `Pagado`, y si `Enabled=false` responder como no disponible
- [ ] 4.6 Endpoint `[AllowFamiliaSession]` en un controller nuevo de pagos de familia

## 5. Webhook de notificaciones (núcleo de seguridad)

- [ ] 5.1 Endpoint anónimo de notificaciones **con el proveedor en la ruta** (`.../notificaciones/mercadopago`), que rechaza todo request con cabecera `Authorization` (T12, D15). La ruta queda registrada en el panel de Mercado Pago: cambiarla después es coordinar con un sistema ajeno que ya manda notificaciones de dinero
- [ ] 5.2 Validación de firma delegada en `WebhookSignatureValidator` del SDK, con la tolerancia de frescura configurada; `data.id` y `x-request-id` se pasan tal como llegan, sin normalizar, porque son parte del mensaje firmado (T3, T4, D5)
- [ ] 5.3 Rechazo por ventana de frescura sobre el instante firmado (T4)
- [ ] 5.4 Consulta server-to-server `GET /v1/payments/{id}` como única fuente de verdad del estado (T5)
- [ ] 5.5 Resolución `ReferenciaPagoPublica` → `(TenantId, PedidoId)` como proyección a un record de dos `long`, única consulta cross-tenant (D4 ④)
- [ ] 5.6 `SetSystemContext(tenantId, ...)` inmediatamente después de resolver, siguiendo el patrón de `FotoProcesamientoWorker` (D4 ⑤)
- [ ] 5.7 `TenantScopeGuard.EnsureTenantScoped(appContext)` al inicio del AppService de procesamiento, que lanza si el contexto sigue anónimo (D4 ⑥)
- [ ] 5.8 Verificación de monto y moneda contra el pedido; discrepancia → `RevisionManual` con ambos montos en la bitácora (T6)
- [ ] 5.9 Escritura idempotente de la bitácora: traducir la violación de índice único a "ya procesada" y responder 200 (D2)
- [ ] 5.10 **⚠ verificar qué eventos llegan realmente** ante una devolución y ante un contracargo —tópico, estado y si son notificaciones distintas—; la documentación no lo detalla. Manejo de `pending`/`in_process` → `AcreditandoMP`, y de devolución/contracargo → `Reembolsado`
- [ ] 5.13 Pago acreditado sobre un pedido `Cancelado` → `RevisionManual` sin reactivar el pedido (D12)
- [ ] 5.11 Política de respuesta según la tabla de D4: **200** para lo definitivo (firma inválida, fuera de ventana, referencia desconocida, ya procesada) y **5xx** para lo transitorio (MP no responde, falla la base, se agota el timeout). Nunca descartar una notificación por un fallo transitorio
- [ ] 5.11b **⚠ verificar el presupuesto de 22 segundos**, que viene de la documentación y fija todos los timeouts del handler. Timeout explícito en la consulta del paso ③, con margen frente a ese presupuesto; al agotarse se responde 5xx y se procesa en el reintento (D4)
- [ ] 5.12 Persistir solo lo necesario para conciliar; no volcar el cuerpo completo de la respuesta ni datos personales de quien paga (T14)

## 5b. Barrido de conciliación

Es la única defensa que no depende de que Mercado Pago nos avise (D4, T19).

- [ ] 5b.1 Proceso periódico que consulta a Mercado Pago el estado de los pedidos que llevan más de una ventana configurable en `Iniciado` o `AcreditandoMP`
- [ ] 5b.2 El resultado se procesa por el **mismo** AppService que una notificación: misma verificación de monto y moneda, misma bitácora, misma idempotencia. No hay un segundo camino de acreditación
- [ ] 5b.3 Corre con contexto de sistema por tenant, siguiendo el patrón de `FotoProcesamientoWorker` (D4 ⑤) y con el mismo `TenantScopeGuard` al inicio
- [ ] 5b.4 Acotar el trabajo por corrida y dejar de barrer un pedido tras una antigüedad configurable, para que un pedido abandonado no se consulte para siempre
- [ ] 5b.5 Test: un pago acreditado en Mercado Pago cuya notificación nunca llegó termina acreditado por el barrido, con una sola entrada de bitácora
- [ ] 5b.6 Test: el barrido sobre un pedido ya acreditado por notificación no duplica nada

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
- [ ] 10.20 Idempotencia saliente: reintentar la misma creación produce la misma clave de idempotencia y no genera un segundo cobro (T17)
- [ ] 10.21 Fallo transitorio: con la consulta a Mercado Pago caída, el webhook responde 5xx —no 200— y el reintento posterior acredita correctamente (T18)
- [ ] 10.22 Multi-tenant en el adaptador: dos llamadas concurrentes de tenants distintos usan cada una su credencial; test que falla si la credencial se comparte entre requests (D8)
- [ ] 10.23 Credencial ausente: sin `RequestOptions` con token, la llamada falla de forma ruidosa y no cae a una credencial por defecto (D8)

## 11. Verificación manual end-to-end

- [ ] 11.1 Levantar túnel público (cloudflared) apuntando a la API dev y configurar la URL de notificaciones en el panel de Mercado Pago
- [ ] 11.2 Flujo completo con credenciales de prueba: confirmar pedido → iniciar pago → pagar → notificación → pedido acreditado
- [ ] 11.3 Caso de pago rechazado y reintento exitoso: la bitácora conserva ambos y el estado final es `Pagado`
- [ ] 11.4 Caso de pago pendiente de acreditación: el pedido figura en curso, no cobrado
- [ ] 11.5 Navegar a mano a la URL de retorno exitoso sin pagar y confirmar que el pedido no cambia de estado
- [ ] 11.6 Cobro manual en efectivo desde el admin, incluido el caso con pago de MP en curso
- [ ] 11.8 Cortar la API mientras hay un pago en curso y confirmar que Mercado Pago reintenta la notificación y el pedido termina acreditado — es la verificación de que el 5xx de 5.11 funciona como red de contención
- [ ] 11.7 Bajar el túnel al terminar

## 12. Suites y documentación

- [ ] 12.1 Suite de integración del backend en verde
- [ ] 12.2 Suite unit del frontend + lint + build en verde
- [ ] 12.3 ADR nuevo en `docs/04-decisiones.md`: estado de pago derivado de la bitácora y separado del fulfillment (D1)
- [ ] 12.4 ADR nuevo en `docs/04-decisiones.md`: aislamiento de tenant en el webhook — lookup estrecho, contexto de sistema y guard que lo exige (D4)
- [ ] 12.5 ADR nuevo en `docs/04-decisiones.md`: la notificación autentica al emisor, la API dice qué pasó (D5) y el retorno del navegador no acredita (D6)
- [ ] 12.5b ADR nuevo en `docs/04-decisiones.md`: SDK oficial de Mercado Pago detrás del puerto `IPasarelaPagos`, con credencial por request y el estático prohibido por test de arquitectura (D8). Dejar asentado el motivo por el que se descartó el cliente propio, para que no se re-discuta
- [ ] 12.6 Actualizar ADR-08 con lo que efectivamente se implementó, y ADR-12 con la separación cobro/fulfillment
- [ ] 12.7 Tildar los ítems correspondientes de Fase 3 en `docs/03-fases.md`
- [ ] 12.8 Anotar en `docs/05-notas-abiertas.md` las fases posteriores: seña/pago parcial, y cuotas sin interés vía convenio bancario o precio de lista con descuento por transferencia
- [ ] 12.9 Anotar en `docs/05-notas-abiertas.md` las cuentas de comprador con el diseño ya resuelto para que no se re-discuta: identidad **separada de `Usuario`** (que es multi-tenant y modela a quien trabaja en la organización, no a su cliente), cross-tenant por diseño, anclada al correo del pedido — que es lo que permitirá vincular las compras anteriores sin perder historia
- [ ] 12.10 Anotar en `docs/05-notas-abiertas.md` la entregabilidad del correo como decisión de deploy: con el comprobante formando parte de la experiencia de compra, que el mensaje no caiga en spam pasa a ser requisito
- [ ] 12.11 Anotar en `docs/05-notas-abiertas.md` la detección de códigos en circulación: un mismo código canjeado desde muchos orígenes distintos no prueba nada por sí solo, pero avisarle al fotógrafo le permitiría desactivarlo. No previene el uso del código —eso requeriría autenticar familias, que está descartado— solo lo hace visible
- [ ] 12.12 Anotar en `docs/05-notas-abiertas.md` qué implica realmente incorporar un segundo proveedor de pagos, para que el día que se plantee nadie lo vuelva a derivar ni concluya que es una reescritura: adaptador nuevo detrás de `IPasarelaPagos`, ruta de webhook propia, mapeo de estados a `EstadoPago`, y credenciales por proveedor. Lo que **no** hay que hacer es una abstracción genérica de proveedores antes de tener el segundo caso a la vista (D15)

## 13. Documento funcional del proceso de cobro

Explicativo y en lenguaje de negocio, no técnico. Es el documento al que se vuelve cuando algo no
cierra y hay que entender qué pasó, sin leer código.

- [ ] 13.1 Crear `docs/06-cobros.md` con el circuito completo contado de punta a punta: qué hace la familia, qué ve, qué recibe el fotógrafo y en qué momento
- [ ] 13.2 Documentar las dos dimensiones del pedido (cobro y fulfillment) con ejemplos de las combinaciones reales: entregado sin cobrar, cobrado sin imprimir, pago en acreditación
- [ ] 13.3 Documentar cada estado de pago: qué significa, cómo se llega, qué hacer cuando aparece — con foco en `AcreditandoMP` y `RevisionManual`, que son los que van a generar preguntas
- [ ] 13.4 Documentar el cobro en efectivo y transferencia: cuándo conviene, cómo se registra, qué queda auditado
- [ ] 13.5 Costos reales: comisión de Mercado Pago para la provincia del negocio, plazo de acreditación elegido, y que las cuotas las financia quien paga (del relevamiento de 0.5)
- [ ] 13.5b Ejemplo numérico del neto que recibe el fotógrafo: `Total − comisión MP − comisión plataforma`, dejando claro que Mercado Pago descuenta primero lo suyo y que la comisión de la plataforma es un **monto** calculado sobre el total, no un porcentaje del neto (D8c). Es el punto donde las cuentas se discuten
- [ ] 13.6 Guía de conciliación: cómo responder "¿este pedido está cobrado y por qué?" usando la bitácora
- [ ] 13.7 Qué hacer ante los casos raros: pago sobre pedido cancelado, monto que no coincide, devolución, pago que nunca se acredita
- [ ] 13.8 Requisitos de producción de Mercado Pago: HTTPS con certificado válido en las URLs de webhook y retorno, sin `localhost` ni IP — enlazar con el ítem de dominio del deploy
