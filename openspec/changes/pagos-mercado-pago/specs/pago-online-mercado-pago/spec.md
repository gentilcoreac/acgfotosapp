## Purpose

Permite que una familia pague su pedido en el momento, con tarjeta o los medios que ofrece Mercado
Pago Checkout Pro, y que el sistema acredite ese cobro únicamente cuando lo confirma Mercado Pago por
un canal verificado —nunca por lo que diga el navegador de quien paga.

## ADDED Requirements

### Requirement: La familia obtiene un enlace de pago para su propio pedido

Una sesión de familia SHALL poder iniciar el pago de un pedido y recibir la URL de pago de Mercado
Pago. El pedido a pagar SHALL resolverse exclusivamente a partir de los participantes y el evento
firmados en el token de la sesión; el sistema NO SHALL aceptar del cliente ningún dato que amplíe el
alcance de esa resolución.

#### Scenario: Inicio de pago del propio pedido

- **WHEN** una familia con sesión válida inicia el pago de un pedido suyo con estado de pago
  `SinPagar`
- **THEN** recibe una URL de pago de Mercado Pago y el estado de pago del pedido pasa a `Iniciado`

#### Scenario: Intento de pagar el pedido de otra familia

- **WHEN** una sesión de familia solicita iniciar el pago de un pedido que pertenece a un
  participante fuera de su sesión
- **THEN** la petición es rechazada y no se crea ninguna preferencia de pago en Mercado Pago

#### Scenario: Petición sin sesión de familia

- **WHEN** se solicita iniciar un pago sin sesión de familia válida
- **THEN** la petición es rechazada con 403

#### Scenario: Pedido ya pagado

- **WHEN** una familia inicia el pago de un pedido cuyo estado de pago ya es `Pagado`
- **THEN** el sistema no crea una preferencia nueva e informa que el pedido ya está pagado

### Requirement: El monto a cobrar lo determina el servidor

El monto, la moneda y el detalle de la preferencia de pago SHALL construirse a partir del total del
pedido almacenado. El sistema NO SHALL aceptar un monto, un precio unitario ni un descuento
proveniente del cliente en ningún punto del flujo de pago.

#### Scenario: El cliente intenta imponer un monto

- **WHEN** la petición de inicio de pago incluye un campo de monto
- **THEN** el campo se ignora y la preferencia se crea por el total almacenado del pedido

#### Scenario: Monto de la preferencia

- **WHEN** se crea la preferencia de un pedido cuyo total almacenado es T
- **THEN** la preferencia se crea por exactamente T en la moneda configurada

### Requirement: Cuotas financiadas por quien paga

La preferencia SHALL ofrecer pago en cuotas con el costo de financiación a cargo de quien paga. El
sistema NO SHALL configurar cuotas sin interés ni ninguna otra modalidad que traslade el costo de
financiación al fotógrafo.

#### Scenario: Pago en cuotas

- **WHEN** una familia elige pagar en cuotas en el checkout
- **THEN** el monto acreditado al fotógrafo es el total del pedido, sin descuento por financiación

### Requirement: El pedido se referencia con un identificador opaco

La referencia externa que viaja a Mercado Pago y es visible para quien paga NO SHALL ser el
identificador interno del pedido ni ningún valor secuencial o deducible. SHALL ser un valor opaco,
generado con un generador criptográficamente seguro, único por pedido y sin significado de negocio.

#### Scenario: Referencia visible en el checkout

- **WHEN** se crea la preferencia de un pedido
- **THEN** la referencia externa enviada a Mercado Pago no permite deducir el identificador del
  pedido, la cantidad de pedidos existentes ni el orden en que se crearon

#### Scenario: Dos pedidos distintos

- **WHEN** se crean preferencias para dos pedidos distintos
- **THEN** sus referencias externas son distintas y no guardan relación deducible entre sí

### Requirement: Las notificaciones de pago se autentican antes de procesarse

El sistema SHALL rechazar toda notificación de pago cuya firma no se valide contra el secreto de
webhook configurado. La comparación de firmas SHALL realizarse en tiempo constante. El sistema SHALL
rechazar notificaciones cuyo instante declarado esté fuera de una ventana de frescura configurada.

#### Scenario: Notificación sin firma

- **WHEN** llega una notificación de pago sin cabecera de firma
- **THEN** se rechaza sin consultar ni modificar ningún pedido

#### Scenario: Notificación con firma inválida

- **WHEN** llega una notificación de pago con una firma que no corresponde al secreto configurado
- **THEN** se rechaza sin consultar ni modificar ningún pedido

#### Scenario: Reenvío de una notificación antigua capturada

- **WHEN** se reenvía una notificación con firma válida pero cuyo instante declarado es anterior a la
  ventana de frescura
- **THEN** se rechaza sin modificar ningún pedido

### Requirement: El contenido de la notificación no es fuente de verdad

Aun con la firma válida, el sistema NO SHALL decidir el estado de un pago con los datos del cuerpo de
la notificación. SHALL consultar el pago contra la API de Mercado Pago con sus propias credenciales y
decidir únicamente con esa respuesta.

#### Scenario: Notificación firmada que declara un estado falso

- **WHEN** llega una notificación con firma válida que declara un pago aprobado, pero la consulta
  directa a Mercado Pago devuelve que el pago fue rechazado
- **THEN** el pedido no se marca como pagado y se registra el resultado real

#### Scenario: Notificación de un pago inexistente

- **WHEN** llega una notificación con firma válida referida a un pago que Mercado Pago no reconoce
- **THEN** no se modifica ningún pedido

### Requirement: El monto acreditado se verifica contra el pedido

Antes de acreditar un cobro, el sistema SHALL verificar que el monto y la moneda informados por
Mercado Pago coinciden con el total y la moneda del pedido referenciado. Ante cualquier discrepancia,
el pedido NO SHALL marcarse como pagado: SHALL quedar señalado para revisión manual, con el hecho
registrado en la bitácora.

#### Scenario: Monto menor al total del pedido

- **WHEN** Mercado Pago informa un pago aprobado por un monto menor al total del pedido
- **THEN** el pedido queda en `RevisionManual`, no en `Pagado`, y la bitácora registra ambos montos

#### Scenario: Moneda distinta

- **WHEN** Mercado Pago informa un pago aprobado en una moneda distinta a la del pedido
- **THEN** el pedido queda en `RevisionManual` y no se acredita el cobro

#### Scenario: Monto exacto

- **WHEN** Mercado Pago informa un pago aprobado por el total exacto del pedido en su moneda
- **THEN** el pedido pasa a estado de pago `Pagado`

#### Scenario: Pago acreditado sobre un pedido cancelado

- **WHEN** se acredita un pago cuyo pedido está en fulfillment `Cancelado`
- **THEN** el estado de pago queda en `RevisionManual`, el pedido sigue `Cancelado` y el hecho queda
  registrado en la bitácora

### Requirement: El procesamiento de notificaciones es idempotente y monótono

El sistema SHALL poder recibir la misma notificación varias veces sin producir efectos duplicados.
Una notificación que llegue fuera de orden NO SHALL retroceder el estado de pago de un pedido a uno
anterior.

#### Scenario: Notificación repetida

- **WHEN** la misma notificación de un pago acreditado se recibe tres veces
- **THEN** el pedido queda pagado una sola vez y la bitácora contiene una sola entrada de
  acreditación

#### Scenario: Notificación tardía de un estado anterior

- **WHEN** un pedido ya está en estado de pago `Pagado` y llega una notificación tardía que
  corresponde al estado anterior `AcreditandoMP`
- **THEN** el estado de pago del pedido permanece en `Pagado`

#### Scenario: Notificaciones concurrentes

- **WHEN** dos notificaciones del mismo pago se procesan simultáneamente
- **THEN** el resultado es el mismo que procesarlas en serie: una sola acreditación

### Requirement: El procesamiento de notificaciones respeta el aislamiento entre tenants

El procesamiento de una notificación SHALL operar únicamente sobre datos del tenant dueño del pedido
referenciado. La resolución del tenant a partir de la referencia externa SHALL ser la única consulta
que atraviese tenants, SHALL devolver únicamente los identificadores necesarios para establecer el
contexto, y SHALL ocurrir antes de cualquier otro acceso a datos.

#### Scenario: Notificación de un pedido de otro tenant

- **WHEN** llega una notificación válida cuya referencia corresponde a un pedido del tenant A
- **THEN** solo se leen y modifican datos del tenant A, y ningún dato de otro tenant resulta accesible
  durante el procesamiento

#### Scenario: Referencia externa desconocida

- **WHEN** llega una notificación válida con una referencia externa que no corresponde a ningún pedido
- **THEN** no se accede a datos de ningún tenant y la notificación se descarta sin revelar ese hecho
  al emisor

### Requirement: El retorno del navegador no acredita el pago

El regreso de quien paga desde el checkout de Mercado Pago hacia la aplicación NO SHALL modificar el
estado de pago de ningún pedido ni presentarse como confirmación de cobro. La aplicación SHALL
mostrar el estado real obtenido del servidor, que solo cambia por la verificación de notificaciones.

#### Scenario: Navegación directa a la URL de retorno exitoso

- **WHEN** alguien navega directamente a la URL de retorno de pago exitoso sin haber pagado
- **THEN** el estado de pago del pedido no cambia y la aplicación muestra el estado real del pedido,
  no un mensaje de pago confirmado

#### Scenario: Retorno antes de que llegue la notificación

- **WHEN** quien paga vuelve a la aplicación antes de que se procese la notificación de Mercado Pago
- **THEN** la aplicación indica que el pago se está verificando y refleja el estado acreditado cuando
  la verificación se completa

### Requirement: La familia puede consultar el estado de pago de su pedido

Una familia SHALL poder consultar el estado de pago de su pedido. La consulta SHALL resolver el
pedido por la sesión de familia o, cuando esa sesión ya expiró, por la referencia opaca del pago, y
SHALL devolver únicamente el estado de pago y los datos mínimos para mostrarlo.

#### Scenario: Consulta con sesión vigente

- **WHEN** una familia con sesión vigente consulta el estado de pago de su pedido
- **THEN** recibe el estado de pago actual del pedido

#### Scenario: Consulta tras vencer la sesión de familia

- **WHEN** la sesión de familia venció durante el pago y se consulta el estado con la referencia
  opaca del pago
- **THEN** recibe el estado de pago del pedido, sin acceso a las fotos, al álbum ni a otros datos del
  evento

#### Scenario: Consulta con una referencia inexistente

- **WHEN** se consulta el estado con una referencia que no corresponde a ningún pago
- **THEN** la respuesta no permite distinguir una referencia inexistente de una ajena

### Requirement: Un pedido no acreditado no se presenta como cobrado

Un pedido cuyo pago esté `Iniciado` o `AcreditandoMP` NO SHALL figurar como cobrado en ninguna vista
ni exportación. El sistema SHALL distinguir explícitamente el pago en curso del pago acreditado.

#### Scenario: Pago en acreditación pendiente

- **WHEN** un pago queda pendiente de acreditación en Mercado Pago
- **THEN** el pedido figura con pago en curso, no como pagado, y el fotógrafo puede distinguirlo del
  resto

### Requirement: Devoluciones y contracargos se registran

El sistema SHALL registrar las notificaciones de devolución y contracargo, actualizar el estado de
pago del pedido a `Reembolsado` y dejar constancia en la bitácora. NO SHALL descartarlas
silenciosamente.

#### Scenario: Devolución de un pago acreditado

- **WHEN** llega una notificación verificada de devolución de un pago ya acreditado
- **THEN** el estado de pago del pedido pasa a `Reembolsado` y la bitácora registra la devolución con
  su monto e instante

### Requirement: Las credenciales de Mercado Pago no se exponen

Las credenciales de Mercado Pago SHALL leerse de una fuente de secretos externa a los archivos de
configuración versionados. NO SHALL enviarse al cliente, NO SHALL registrarse en logs ni respuestas
de error, y el secreto de webhook SHALL ser independiente de la credencial de acceso a la API.

#### Scenario: Configuración versionada

- **WHEN** se inspecciona la configuración versionada del repositorio
- **THEN** no contiene la credencial de acceso ni el secreto de webhook, solo las claves de
  configuración no sensibles

#### Scenario: Error del proveedor de pagos

- **WHEN** la llamada a Mercado Pago falla y el error se registra o se devuelve al cliente
- **THEN** ni el mensaje ni el registro contienen la credencial de acceso

#### Scenario: Arranque sin credenciales configuradas

- **WHEN** el pago online está habilitado pero faltan las credenciales
- **THEN** el sistema lo señala explícitamente en el arranque en lugar de fallar recién al primer
  intento de pago

### Requirement: Los datos personales de quien paga no se almacenan de más

El sistema SHALL registrar de la respuesta de Mercado Pago únicamente los datos necesarios para
conciliar el cobro. NO SHALL almacenar ni registrar en logs el cuerpo completo de las respuestas ni
los datos personales de quien paga que no sean necesarios para la conciliación.

#### Scenario: Procesamiento de una notificación

- **WHEN** se procesa una notificación de pago que incluye datos personales de quien pagó
- **THEN** la bitácora conserva identificador de pago, monto, medio, estado e instante, y no el
  cuerpo completo de la respuesta

### Requirement: El inicio de pago está limitado por frecuencia

El endpoint de inicio de pago SHALL estar sujeto a un límite de frecuencia por origen, independiente
del límite global de la aplicación. La recepción de notificaciones SHALL admitir la frecuencia de
reintentos del proveedor sin rechazarlas por límite de frecuencia.

#### Scenario: Solicitudes repetidas de inicio de pago

- **WHEN** un mismo origen solicita iniciar pagos por encima del límite configurado
- **THEN** las solicitudes excedentes se rechazan con 429

#### Scenario: Reintentos del proveedor

- **WHEN** Mercado Pago reintenta notificaciones de varios pagos en ráfaga desde sus servidores
- **THEN** las notificaciones se procesan sin ser rechazadas por límite de frecuencia
