## MODIFIED Requirements

### Requirement: La familia obtiene un enlace de pago para su propio pedido

Una sesión de familia SHALL poder iniciar el pago de un pedido y recibir la URL de pago de Mercado
Pago. El pedido a pagar SHALL resolverse exclusivamente a partir de los participantes y el evento
firmados en el token de la sesión; el sistema NO SHALL aceptar del cliente ningún dato que amplíe el
alcance de esa resolución. La preferencia SHALL crearse contra la cuenta de cobro vinculada del
fotógrafo dueño del evento, declarando la comisión de la plataforma correspondiente al pedido.

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

#### Scenario: Cobro en la cuenta del fotógrafo del evento

- **WHEN** una familia paga el pedido de un evento del fotógrafo A
- **THEN** el dinero se acredita en la cuenta vinculada del fotógrafo A, y no en la de ningún otro
  tenant ni en la de la plataforma

#### Scenario: Fotógrafo sin cuenta vinculada

- **WHEN** una familia intenta iniciar el pago de un pedido de un fotógrafo sin cuenta vinculada
- **THEN** el pago online no se ofrece y no se crea ninguna preferencia

### Requirement: El procesamiento de notificaciones respeta el aislamiento entre tenants

El procesamiento de una notificación SHALL operar únicamente sobre datos del tenant dueño del pedido
referenciado. La resolución del tenant a partir de la referencia externa SHALL ser la única consulta
que atraviese tenants, SHALL devolver únicamente los identificadores necesarios para establecer el
contexto, y SHALL ocurrir antes de cualquier otro acceso a datos. La verificación del pago contra
Mercado Pago SHALL realizarse con la credencial vinculada del tenant dueño del pedido.

#### Scenario: Notificación de un pedido de otro tenant

- **WHEN** llega una notificación válida cuya referencia corresponde a un pedido del tenant A
- **THEN** solo se leen y modifican datos del tenant A, y ningún dato de otro tenant resulta accesible
  durante el procesamiento

#### Scenario: Referencia externa desconocida

- **WHEN** llega una notificación válida con una referencia externa que no corresponde a ningún pedido
- **THEN** no se accede a datos de ningún tenant y la notificación se descarta sin revelar ese hecho
  al emisor

#### Scenario: Verificación con la credencial correcta

- **WHEN** se verifica contra Mercado Pago un pago de un pedido del tenant A
- **THEN** la consulta se hace con la credencial vinculada del tenant A, nunca con la de otro tenant
  ni con una credencial compartida entre procesos
