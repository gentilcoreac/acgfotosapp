## Regla de verificación

Buena parte de lo que este plan da por cierto sobre Mercado Pago viene de su asistente o de leer su
documentación, no de haberlo comprobado. Ya falló dos veces en cosas estructurales: dijo que Split de
Pagos exigía aprobación comercial —el 1:1 es autoservicio— y que los pagos de prueba no disparan
notificaciones, cuando la documentación instruye exactamente lo contrario. Las tareas marcadas con
**⚠ verificar** abren su grupo: se comprueba el dato y recién después se construye encima. Es un
chequeo acotado, no una auditoría — lo que no depende del dato sigue avanzando.

## 0. Prerrequisito externo — arrancar ya, corre en paralelo

- [ ] 0.1 Llevar la cuenta de Mercado Pago de la plataforma a **verificación de identidad nivel 6 (KYC 6)**, que es el requisito documentado de Split de Pagos 1:1, e instalar la app móvil para gestionar los cobros. Es el único prerrequisito que puede demorar; el 1:1 **no** requiere aprobación del equipo comercial (eso es del 1:N)
- [ ] 0.2 Anotar en el documento funcional el estado de la habilitación y de qué depende (bloquea solo el grupo 8)
- [ ] 0.3 Relevar de FullFoto y mifotoar dos cosas en el mismo viaje: qué comisión cobran (insumo de la tasa propia) y **qué política de devolución le ofrecen a la familia** (insumo de la Open Question de devoluciones). No se encontraron publicadas: hay que mirarlas desde el flujo de compra o preguntando
- [ ] 0.4 Relevar la comisión real de Mercado Pago, el plazo de acreditación y las retenciones provinciales aplicables: determinan el remanente del que se descuenta la comisión de la plataforma
- [ ] 0.5 Configurar y autorizar el MCP server oficial de Mercado Pago; usarlo para crear la aplicación, obtener credenciales, configurar los webhooks y generar los usuarios de prueba (marketplace, vendedor y comprador) que necesita el grupo 8. Sirve además como fuente de consulta durante toda la implementación de los dos changes, no solo para el alta
- [ ] 0.6 **Habilitar PKCE en los detalles de la aplicación** en el panel: sin ese paso Mercado Pago no exige el `code_challenge` y la protección queda decorativa aunque el código la mande (D3)
- [ ] 0.7 Suscribir los tópicos de webhook que este change necesita además de `payment`: `mp-connect` (vinculación/desvinculación OAuth) y los de contracargos y reclamos

## 1. Modelo: cuenta de cobro del fotógrafo

- [ ] 1.1 Entidad de cuenta de cobro por tenant: estado de vinculación, identificación de la cuenta en Mercado Pago, momentos de vinculación y última renovación
- [ ] 1.2 Almacenamiento de credenciales vinculadas, cifradas con clave fuera de la base (D5), **indexadas por (tenant, proveedor)** y no solo por tenant: es la tabla del cifrado en reposo y la rotación, la que menos conviene migrar con datos productivos adentro (D15 de `pagos-mercado-pago`)
- [ ] 1.3 Entidad del valor de un solo uso de la vinculación: valor, tenant, vencimiento, consumido (D4)
- [ ] 1.4 Migración EF con `Down` completo
- [ ] 1.5 Verificar por consulta directa a la base que las credenciales no son legibles ni utilizables

## 2. Flujo de autorización

- [ ] 2.1 Inicio de la vinculación: generar el valor de un solo uso (`state`), asociarlo al tenant y redirigir a la autorización de Mercado Pago con `client_id`, `response_type=code`, `platform_id=mp`, `redirect_uri` estática registrada, **`scope=offline_access`** y **PKCE** (`code_challenge` + `code_challenge_method=S256`), guardando el `code_verifier` junto al `state` (D3)
- [ ] 2.1b Test que verifica que la URL de autorización incluye `offline_access` y el `code_challenge`. Sin `offline_access` no hay renovación posible y todas las vinculaciones caen juntas a los 180 días — un olvido que se descubre medio año después (D6)
- [ ] 2.2 Retorno de la autorización: validar el `state` (existe, no consumido, no vencido, del tenant correcto), consumirlo, e intercambiar el `code` —que vive 10 minutos— por las credenciales enviando el `code_verifier`
- [ ] 2.3 Rechazar el retorno cuando el fotógrafo cancela la autorización, con mensaje que explique la consecuencia
- [ ] 2.4 Rechazar la vinculación si esa cuenta de Mercado Pago ya está vinculada a otro tenant
- [ ] 2.5 Desvinculación: eliminar credenciales, conservar historial de cobros y saldo, advertir si hay saldo pendiente
- [ ] 2.6 `FamiliaSessionGuard` en todos los métodos del AppService de vinculación
- [ ] 2.7 Verificar que ninguna respuesta de estado de vinculación incluye la credencial

## 3. Renovación y caída de la vinculación

- [ ] 3.1 **⚠ verificar** la vigencia de 180 días y que el refresh encadena. Renovación proactiva con margen amplio antes del vencimiento, sin intervención del fotógrafo y con reintentos. Al renovar se persiste **también el `refresh_token` nuevo**, que Mercado Pago rota en cada canje: guardar solo el access token rompe la cadena en la renovación siguiente (D6)
- [ ] 3.2 Marcar la vinculación como caída cuando la renovación falla de forma definitiva
- [ ] 3.3 Avisar al fotógrafo de la caída y dejar de ofrecer pago online en sus eventos (D6)
- [ ] 3.3b **⚠ verificar** que el tópico existe y notifica desconexiones. Suscribir y procesar el tópico de webhook **`mp-connect`**, que notifica conexiones y desconexiones OAuth: una revocación desde el panel del fotógrafo marca la vinculación como caída en el momento, sin esperar a que falle una renovación meses después (D6)
- [ ] 3.3c Test: una notificación de desconexión verificada deja la vinculación caída y sus eventos sin pago online
- [ ] 3.4 Verificar que la credencial no aparece en logs por ningún camino de renovación ni de error

## 4. Comisión devengada

- [ ] 4.1 Tasa de comisión general de la plataforma, configurable
- [ ] 4.2 Tasa propia por tenant, opcional, incluida tasa cero
- [ ] 4.3 Devengar la comisión al confirmar el pedido, con la tasa vigente congelada junto al devengamiento (D1)
- [ ] 4.4 Revertir el devengamiento **solo cuando se cancela el pedido**, esté cobrado o no. Un pago reembolsado sobre un pedido que sigue vigente **no** revierte nada: la comisión permanece devengada y pasa a saldo pendiente (D13). La implementación obvia —notificación de reembolso, revertir— es justamente el agujero
- [ ] 4.5 Tests: la comisión de un pedido no cambia si la tasa cambia después

## 5. Cuenta corriente

- [ ] 5.1 Entidad de movimiento: tipo (devengado, retenido, revertido, liquidado), monto, pedido de origen, momento, autor cuando corresponde
- [ ] 5.2 Los movimientos son inmutables: no se editan ni se borran
- [ ] 5.3 Saldo derivado del pliegue de los movimientos, con caché denormalizada reconstruible (D2)
- [ ] 5.4 Camino explícito para recalcular el saldo desde los movimientos y compararlo con la caché
- [ ] 5.5 Consulta scopeada por tenant; visión consolidada solo para la administración de la plataforma
- [ ] 5.6 Liquidación contra cobros posteriores por Mercado Pago
- [ ] 5.7 Liquidación registrada manualmente, con autor y momento

## 6. Split en el cobro

- [ ] 6.1 Interruptor de marketplace en configuración; apagado, el sistema opera como lo deja `pagos-mercado-pago` (D7)
- [ ] 6.2 La preferencia se crea con la credencial vinculada del fotógrafo dueño del evento (D8)
- [ ] 6.3 **⚠ verificar** que es monto absoluto y no porcentaje, y con cuántos decimales. Declarar la comisión de la plataforma en la preferencia como `marketplace_fee`: un **monto absoluto** en la moneda de la operación, no un porcentaje (D10). El nombre del parámetro depende del producto de Mercado Pago y solo aparece en el adaptador
- [ ] 6.3b Validar antes de enviar que la comisión no supere el monto de la operación; Mercado Pago la rechaza con `invalid_marketplace_fee` y conviene que el error sea nuestro y explicado, no un rechazo opaco del proveedor (D10)
- [ ] 6.4 No ofrecer pago online cuando el fotógrafo no tiene cuenta vinculada o la tiene caída
- [ ] 6.5 Al acreditarse un cobro con split, registrar la comisión como retenida en la cuenta corriente
- [ ] 6.6 La verificación server-to-server del webhook usa la credencial del tenant dueño del pedido, resuelta tras establecer el contexto de sistema
- [ ] 6.7 Contrastar el `user_id` que informa la notificación contra la cuenta vinculada del tenant dueño del pedido; si no coinciden, el pedido va a `RevisionManual` con ambos identificadores en la bitácora y **no** se acredita (D12)

## 6b. Conciliación contra el proveedor

- [ ] 6b.1 Descarga del **reporte de ventas de Split de Pagos** de Mercado Pago (CSV/JSON) por vendedor vinculado, con sus importes y comisiones (D11)
- [ ] 6b.2 Camino para contrastar ese reporte contra la bitácora de cobros y los movimientos de cuenta corriente, señalando las diferencias en ambos sentidos: lo que el proveedor registró y nosotros no, y viceversa
- [ ] 6b.3 Procedimiento escrito en `docs/06-cobros.md`: cada cuánto se concilia, qué se compara y qué hacer ante una diferencia
- [ ] 6b.4 Anotar en `docs/05-notas-abiertas.md` la conciliación automática periódica como trabajo posterior — acá el alcance es poder hacerla, no automatizarla (D11)

## 7. Frontend

- [ ] 7.1 Pantalla de cuenta de cobro: estado de vinculación, acción de vincular, acción de desvincular con su advertencia
- [ ] 7.2 Aviso visible cuando la vinculación está caída, con el camino para re-autorizar
- [ ] 7.3 Cuenta corriente del fotógrafo: saldo pendiente y detalle de movimientos con el pedido que originó cada uno
- [ ] 7.4 Administración de la plataforma: tasa general, excepciones por tenant, visión consolidada de saldos
- [ ] 7.5 En el flujo de la familia, ocultar el pago online cuando el fotógrafo no puede cobrar online

## 8. Verificación contra Mercado Pago — depende de la habilitación (0.1)

- [ ] 8.0 **⚠ verificar primero, antes de planificar el resto del grupo**: qué se puede probar con credenciales de prueba del usuario productivo y qué no. En particular si las notificaciones llegan y si su firma valida igual que en producción, o si hay que usar el simulador del panel. El asistente afirmó que los pagos de prueba no notifican y la documentación dice lo contrario. De esto depende si algo de este grupo necesita dinero real; si lo necesita, el monto tiene piso: con montos mínimos el `marketplace_fee` no es representable y la prueba no mide el reparto
- [ ] 8.1 Vinculación real de una cuenta de prueba mediante el flujo de autorización completo, con los usuarios de prueba de 0.5
- [ ] 8.2 Cobro real con split: verificar que el fotógrafo recibe el neto y la plataforma su comisión
- [ ] 8.3 Verificar el orden de descuentos: primero la comisión de Mercado Pago, después la de la plataforma
- [ ] 8.4 Renovación real de credenciales, verificando que el `refresh_token` devuelto es nuevo y que la cadena sigue funcionando en una segunda renovación consecutiva (D6)
- [ ] 8.5 Revocación desde el panel de Mercado Pago: verificar que llega la notificación `mp-connect` y que la vinculación se marca como caída en el momento, sin esperar al fallo de una renovación
- [ ] 8.6 Si la habilitación no llegó, dejar constancia y desplegar con el interruptor apagado (D7)
- [ ] 8.7 **Reembolso de un pago con split**, total y parcial: verificar qué pasa con la comisión ya retenida —si Mercado Pago la devuelve sola, de qué cuenta sale, y qué informa la notificación—. Es la pregunta abierta que no pudo responderse por documentación y de la que depende cómo se contabiliza la reversión
- [ ] 8.8 **Contracargo sobre un pago con split**: quién absorbe la comisión de la plataforma y qué eventos llegan. **No es ejecutable a voluntad** —lo inicia el titular de la tarjeta ante su banco—, así que no entra en la tanda de pruebas: se resuelve preguntándole a Mercado Pago, y el primero real se maneja a mano y queda en la bitácora
- [ ] 8.9 Verificar el manifest de la firma con un `data.id` alfanumérico (tópico `mp-connect`), que es donde la falta de normalización del SDK podría hacer fallar la validación (3b.5 de `pagos-mercado-pago`)
- [ ] 8.10 Volcar el resultado de 8.2 a 8.9 al documento funcional, incluidos los que confirmen lo esperado

## 9. Tests de regresión de seguridad

- [ ] 9.1 Credenciales cifradas: no son legibles ni utilizables desde una consulta directa a la base (M1)
- [ ] 9.2 Valor de un solo uso inválido, vencido o de otro tenant: el retorno no vincula nada (M2)
- [ ] 9.3 Reutilización de un retorno ya consumido: se rechaza (M3)
- [ ] 9.4 La preferencia de un pedido del tenant A se crea con la credencial de A, nunca con otra (M4, M5)
- [ ] 9.5 Un monto o una tasa de comisión enviados por el cliente se ignoran (M6)
- [ ] 9.6 Un pedido cobrado por transferencia devenga la misma comisión que uno cobrado por Mercado Pago (M7)
- [ ] 9.7 Un fotógrafo no accede a la cuenta corriente de otro tenant (M8)
- [ ] 9.8 El saldo recalculado desde los movimientos coincide con el informado (M9)
- [ ] 9.9 Ningún log ni respuesta de error contiene una credencial vinculada (M10)
- [ ] 9.10 Con el interruptor de marketplace apagado, el comportamiento es idéntico al de `pagos-mercado-pago`
- [ ] 9.11 PKCE: un `code` canjeado sin el `code_verifier` correcto no produce credenciales (M11)
- [ ] 9.12 Revocación notificada: la vinculación queda caída y sus eventos dejan de ofrecer pago online (M12)
- [ ] 9.13 La URL de autorización incluye `scope=offline_access`; test que falla si alguien lo saca (M13)
- [ ] 9.14 Concurrencia multi-tenant: dos cobros simultáneos de tenants distintos usan cada uno su credencial vinculada, y ninguno cae a una credencial por defecto (M4, M5)
- [ ] 9.15 Cuenta receptora cruzada: una notificación cuyo `user_id` no corresponde a la cuenta vinculada del tenant del pedido deja el pedido en `RevisionManual` y no lo acredita (M15)
- [ ] 9.16 Comisión mayor al total: se rechaza antes de llamar al proveedor, con un error propio (M16)

## 10. Suites y documentación

- [ ] 10.1 Suite de integración del backend en verde
- [ ] 10.2 Suite unit del frontend + lint + build en verde
- [ ] 10.3 ADR en `docs/04-decisiones.md`: estrategia de monetización — comisión sobre el pedido y no sobre el pago, con el agujero de la transferencia que la motiva y los modelos descartados (suscripción pura, comisión solo sobre Mercado Pago, y cobrar todo la plataforma y liquidar, que la volvería intermediario financiero)
- [ ] 10.4 ADR en `docs/04-decisiones.md`: vinculación por autorización y custodia de credenciales de terceros, incluidos `offline_access` y PKCE como parte no negociable del flujo (D3)
- [ ] 10.4b En `docs/06-cobros.md`, el ejemplo numérico del neto del fotógrafo: Mercado Pago descuenta primero su comisión y la de la plataforma sale del remanente, así que el porcentaje sobre lo acreditado se ve mayor al pactado sobre el total (D10)
- [ ] 10.5 Ampliar el documento funcional `docs/06-cobros.md` con la parte de plataforma: qué cobra, cómo se retiene, cómo se lee la cuenta corriente y qué pasa con las ventas cobradas por fuera de Mercado Pago
- [ ] 10.6 Actualizar `docs/00-vision.md` y `docs/03-fases.md`: AcgFotos deja de ser la herramienta de un fotógrafo y pasa a ser una plataforma multi-fotógrafo con ingresos propios
- [ ] 10.7 Anotar en `docs/05-notas-abiertas.md` lo que queda fuera: facturación electrónica, planes comerciales, onboarding autogestionado de fotógrafos y detección de abuso
