## Purpose

Separa el estado de cobro de un pedido del estado de fulfillment (impresión y entrega), que hoy
conviven en un único campo, y conserva una bitácora inmutable de todo intento de cobro —automático o
manual— para que el fotógrafo pueda conciliar qué se cobró, cuándo, por qué medio y quién lo
registró.

## ADDED Requirements

### Requirement: El pedido expone cobro y fulfillment como dimensiones independientes

Un pedido SHALL tener dos estados independientes: uno de fulfillment (si se imprimió y entregó) y uno
de pago (si se cobró). Cambiar uno NO SHALL alterar el otro. Toda combinación de ambos SHALL ser
representable, incluidos "entregado sin cobrar" y "cobrado sin imprimir".

#### Scenario: Un pedido cobrado todavía sin imprimir

- **WHEN** un pedido con fulfillment `Pendiente` recibe un cobro acreditado
- **THEN** su estado de pago pasa a `Pagado` y su estado de fulfillment sigue en `Pendiente`

#### Scenario: Un pedido se entrega y se cobra en efectivo en el mismo acto

- **WHEN** el fotógrafo marca el pedido como `Entregado` y registra el cobro en efectivo
- **THEN** el pedido queda con fulfillment `Entregado` y estado de pago `Pagado`, y ambos cambios
  quedan registrados por separado

#### Scenario: Avanzar el fulfillment no inventa un cobro

- **WHEN** el fotógrafo marca como `Impreso` un pedido con estado de pago `SinPagar`
- **THEN** el estado de pago sigue en `SinPagar` y no se crea ninguna entrada en la bitácora de pagos

### Requirement: El pedido registra los datos de contacto de quien compra

Confirmar un pedido SHALL requerir nombre, teléfono y correo electrónico válido de quien compra. El
pedido SHALL conservar una copia de esos datos: correcciones posteriores sobre el participante NO
SHALL alterar los pedidos ya confirmados.

#### Scenario: Confirmación sin correo

- **WHEN** se intenta confirmar un pedido sin correo
- **THEN** la confirmación se rechaza indicando que el correo es necesario

#### Scenario: Los datos del pedido no cambian después

- **WHEN** el contacto del participante se corrige después de que un pedido suyo fue confirmado
- **THEN** el pedido confirmado sigue mostrando los datos con los que se hizo

### Requirement: Los datos de contacto guardados no se revelan a quien tiene el código

El sistema SHALL poder usar los datos de contacto cargados en el participante sin enviarlos al
cliente. La respuesta al cliente SHALL indicar únicamente que existen datos guardados y una versión
enmascarada que permita reconocerlos, nunca el valor completo. Quien compra SHALL poder confirmar el
uso de los datos guardados o reemplazarlos por otros.

#### Scenario: Confirmación de los datos guardados

- **WHEN** quien compra acepta los datos guardados y confirma el pedido
- **THEN** el pedido queda con los datos del participante, y el cliente nunca recibió sus valores
  completos

#### Scenario: Lo que ve quien abre la sesión

- **WHEN** una sesión de familia consulta los datos de contacto disponibles para el pedido
- **THEN** recibe solo una versión enmascarada, suficiente para reconocerlos y no para leerlos

#### Scenario: Reemplazo de los datos

- **WHEN** quien compra elige ingresar datos distintos
- **THEN** el pedido queda con los datos ingresados, y tampoco en ese camino se revelaron los guardados

#### Scenario: Participante sin datos cargados

- **WHEN** el participante de la sesión no tiene datos de contacto
- **THEN** el formulario se presenta vacío y debe completarse para poder confirmar

### Requirement: El fotógrafo puede cargar el contacto de cada participante

El fotógrafo SHALL poder registrar nombre de contacto, teléfono y correo por participante al
administrar sus grupos. Esos datos SHALL ser opcionales. El nombre del participante —la persona
fotografiada— SHALL mantenerse como un dato distinto del nombre de contacto.

#### Scenario: Carga del contacto

- **WHEN** el fotógrafo carga el contacto de un participante
- **THEN** queda asociado a ese participante y se ofrecerá al confirmar sus pedidos

#### Scenario: Participante sin contacto

- **WHEN** el fotógrafo no conoce el contacto de un participante y lo deja vacío
- **THEN** el participante se guarda igual y sus pedidos piden los datos a quien compra

### Requirement: El contacto del participante se completa si está vacío, y nunca se pisa

Cuando quien compra ingresa datos de contacto y el participante no tenía ninguno, el sistema SHALL
guardarlos en el participante. Cuando el participante ya tenía datos, el sistema NO SHALL
reemplazarlos: el pedido conserva los suyos y la ficha del participante queda intacta.

#### Scenario: Participante sin contacto cargado

- **WHEN** quien compra ingresa sus datos y el participante no tenía ninguno
- **THEN** el participante queda con esos datos y el próximo pedido los ofrece

#### Scenario: Compra de una persona distinta a la registrada

- **WHEN** quien compra ingresa datos distintos y el participante ya tenía contacto cargado
- **THEN** el pedido queda con los datos ingresados y el contacto del participante no se modifica

#### Scenario: Divergencia visible para el fotógrafo

- **WHEN** un pedido se confirmó con datos de contacto distintos de los cargados en el participante
- **THEN** el fotógrafo puede advertir esa diferencia al ver el pedido, y decidir por su cuenta si
  actualiza la ficha

### Requirement: El pedido registra el código de acceso con el que se compró

Un pedido SHALL registrar el código de acceso canjeado en la sesión que lo originó, y el participante
al que corresponde. En una sesión con varios participantes, el pedido NO SHALL atribuirse a uno
arbitrario: SHALL quedar asociado al participante correspondiente al código con el que se abrió la
sesión.

#### Scenario: Trazar una compra hasta su QR

- **WHEN** el fotógrafo consulta un pedido
- **THEN** puede identificar con qué código de acceso se abrió la sesión que lo generó

#### Scenario: Sesión con varios participantes

- **WHEN** una familia suma un segundo código a su sesión y confirma un pedido
- **THEN** el pedido queda asociado al código y participante que corresponden, y no a uno elegido
  arbitrariamente entre los de la sesión

### Requirement: Quien compra recibe el comprobante con un enlace de seguimiento

Al confirmarse un pedido, el sistema SHALL enviar al correo registrado un mensaje con el detalle del
pedido y un enlace que permita consultar su estado más adelante. El envío NO SHALL formar parte de la
confirmación del pedido: un fallo al enviar NO SHALL impedir ni revertir la compra.

#### Scenario: Confirmación exitosa

- **WHEN** una familia confirma su pedido
- **THEN** recibe en su correo el detalle y un enlace para consultar el estado

#### Scenario: Fallo del servicio de correo

- **WHEN** el servicio de correo no está disponible al confirmarse un pedido
- **THEN** el pedido queda confirmado igual, y el envío se reintenta sin intervención

#### Scenario: Consulta posterior desde el enlace

- **WHEN** quien compró abre el enlace del correo días después, con su sesión ya vencida
- **THEN** ve el estado actual de su pedido, sin acceso a las fotos ni a otros datos del evento

### Requirement: Un pedido nuevo nace sin cobrar

Al confirmarse, un pedido SHALL quedar con estado de pago `SinPagar`, independientemente del medio de
pago que la familia elija después.

#### Scenario: Confirmación de pedido

- **WHEN** una familia confirma un pedido
- **THEN** el pedido queda con fulfillment `Pendiente` y estado de pago `SinPagar`

### Requirement: Cada pedido registra la comisión de la plataforma

Al confirmarse un pedido, el sistema SHALL registrar la comisión que le corresponde a la plataforma,
calculada sobre el **total congelado del pedido** con la tasa vigente para ese tenant en ese momento,
en la moneda del pedido y redondeada a dos decimales **hacia abajo**. La comisión registrada NO SHALL
recalcularse si la tasa cambia después. El importe registrado SHALL quedar acompañado de la tasa con
la que se calculó.

#### Scenario: Redondeo de la comisión

- **WHEN** el total del pedido y la tasa vigente dan una comisión con más de dos decimales
- **THEN** la comisión registrada se redondea a dos decimales hacia abajo, y la diferencia queda del
  lado del fotógrafo

#### Scenario: Confirmación con tasa cero

- **WHEN** se confirma un pedido de un tenant cuya tasa de comisión es cero
- **THEN** el pedido queda con comisión registrada en cero

#### Scenario: La tasa cambia después de confirmar

- **WHEN** la tasa de comisión del tenant cambia después de confirmarse un pedido
- **THEN** ese pedido conserva la comisión calculada al momento de confirmarse

### Requirement: Bitácora inmutable de intentos de cobro

Todo cambio del estado de pago SHALL registrar una entrada en una bitácora asociada al pedido. Cada
entrada SHALL incluir el origen del cobro (Mercado Pago o registro manual), el medio de pago, el
monto, el estado resultante, la referencia externa cuando exista, el instante y —si fue manual— el
usuario que lo registró. Las entradas de la bitácora NO SHALL modificarse ni eliminarse una vez
escritas.

#### Scenario: Cobro acreditado por Mercado Pago

- **WHEN** se acredita un pago de Mercado Pago para un pedido
- **THEN** se agrega una entrada con origen Mercado Pago, el monto acreditado, el identificador del
  pago y el instante de acreditación

#### Scenario: Varios intentos sobre el mismo pedido

- **WHEN** una familia intenta pagar, el pago se rechaza, y vuelve a intentar con éxito
- **THEN** la bitácora conserva las dos entradas en orden, y el estado de pago del pedido refleja el
  resultado final `Pagado`

#### Scenario: Corrección posterior del estado de pago

- **WHEN** el fotógrafo corrige manualmente el estado de pago de un pedido ya registrado
- **THEN** se agrega una entrada nueva con el cambio, y las entradas anteriores permanecen intactas

### Requirement: Registro manual de cobro en efectivo o transferencia

El fotógrafo SHALL poder registrar un cobro manual sobre un pedido, indicando el medio (efectivo o
transferencia). El registro SHALL quedar atribuido al usuario que lo hizo y al instante en que
ocurrió. Registrar un cobro manual SHALL ser una operación distinta de cambiar el estado de
fulfillment.

#### Scenario: Cobro en efectivo al entregar

- **WHEN** el fotógrafo registra un cobro en efectivo sobre un pedido con estado de pago `SinPagar`
- **THEN** el estado de pago pasa a `Pagado`, y la bitácora registra origen manual, medio efectivo,
  el usuario autor y el instante

#### Scenario: Cobro manual sobre un pedido ya pagado online

- **WHEN** el fotógrafo intenta registrar un cobro manual sobre un pedido cuyo estado de pago ya es
  `Pagado` por Mercado Pago
- **THEN** el sistema advierte que el pedido ya figura cobrado e indica el origen del cobro previo,
  y exige una confirmación explícita antes de registrar el cobro adicional

#### Scenario: Cobro manual con un pago de Mercado Pago en curso

- **WHEN** el fotógrafo intenta registrar un cobro manual sobre un pedido con estado de pago
  `Iniciado` o `AcreditandoMP`
- **THEN** el sistema advierte que hay un pago en curso que podría acreditarse después, y exige una
  confirmación explícita antes de registrar el cobro

#### Scenario: Una sesión de familia no puede registrar cobros

- **WHEN** una petición con sesión de familia intenta registrar un cobro manual
- **THEN** la petición es rechazada con 403 y no se modifica ningún pedido

### Requirement: El estado de pago es visible y filtrable en el admin de pedidos

El listado de pedidos SHALL mostrar el estado de pago como dato propio, distinto del estado de
fulfillment, y SHALL permitir filtrar por él. El detalle del pedido SHALL mostrar la bitácora de
cobros.

#### Scenario: Filtrar los pedidos que faltan cobrar

- **WHEN** el fotógrafo filtra el listado por estado de pago `SinPagar`
- **THEN** el listado devuelve únicamente los pedidos sin cobro registrado, sin importar su estado de
  fulfillment

#### Scenario: Ver el historial de cobro de un pedido

- **WHEN** el fotógrafo abre el detalle de un pedido con dos intentos de cobro
- **THEN** ve ambas entradas con su origen, medio, monto e instante

## REMOVED Requirements

### Requirement: El estado `Pagado` del pedido

**Reason**: El valor `Pagado` del estado de pedido mezclaba la dimensión de cobro con la de
fulfillment. Con pagos online aparecen estados de cobro (acreditando, rechazado, reembolsado) que no
tienen equivalente en fulfillment, y con cobro en efectivo el pedido tenía que saltar directamente a
`Impreso` para poder expresar su avance, perdiendo la información de cobro.

**Migration**: El estado de pedido conserva `Pendiente`, `Impreso`, `Entregado` y `Cancelado`. La
información de cobro pasa al estado de pago. Los pedidos existentes en estado `Pagado` migran a
fulfillment `Pendiente` con estado de pago `Pagado` y una entrada de bitácora de origen manual que
declara la migración.
