## Purpose

Define cómo la plataforma cobra por las ventas que hace posibles: la comisión se devenga sobre el
pedido —no sobre el pago— para que el método de cobro que elija la familia no altere lo que la
plataforma percibe, y la cuenta corriente registra lo devengado, lo ya retenido y lo pendiente.

## ADDED Requirements

### Requirement: La comisión se devenga sobre el pedido, no sobre el método de pago

Al confirmarse un pedido, el sistema SHALL registrar la comisión que le corresponde a la plataforma,
calculada sobre el total del pedido con la tasa vigente para ese tenant en ese momento. El
devengamiento NO SHALL depender del método con el que se cobre después.

#### Scenario: Pedido que se paga por Mercado Pago

- **WHEN** se confirma un pedido y luego se paga por Mercado Pago
- **THEN** la comisión devengada es la misma que si se hubiera pagado por otro método

#### Scenario: Pedido que se paga por transferencia

- **WHEN** se confirma un pedido y el fotógrafo registra el cobro por transferencia
- **THEN** la comisión queda devengada igual y pasa a integrar el saldo pendiente del fotógrafo

#### Scenario: La tasa cambia después de confirmar

- **WHEN** la tasa de comisión cambia después de que un pedido se confirmó
- **THEN** ese pedido conserva la comisión calculada con la tasa vigente al confirmarse

#### Scenario: Pedido cancelado sin haberse cobrado

- **WHEN** un pedido se cancela sin haberse cobrado
- **THEN** su comisión devengada se revierte y deja de integrar el saldo

#### Scenario: Pedido cobrado que después se cancela

- **WHEN** un pedido ya cobrado se cancela y se le devuelve el dinero a la familia
- **THEN** su comisión devengada se revierte, porque la venta se deshizo

#### Scenario: Pago reembolsado sobre un pedido que sigue vigente

- **WHEN** se reembolsa el pago de un pedido que no se cancela
- **THEN** la comisión NO se revierte y pasa a integrar el saldo pendiente del fotógrafo, igual que si
  ese pedido se hubiera cobrado por transferencia

#### Scenario: Contracargo sobre un pedido vigente

- **WHEN** llega un contracargo por un pago acreditado y el pedido no se cancela
- **THEN** la comisión NO se revierte y pasa a integrar el saldo pendiente del fotógrafo

#### Scenario: El proveedor devuelve la comisión retenida

- **WHEN** el proveedor reintegra por su cuenta la comisión ya retenida de un pedido que sigue vigente
- **THEN** el devengamiento se conserva y esa comisión vuelve a figurar como pendiente, sin que el
  total devengado del pedido cambie

### Requirement: La comisión se retiene en el origen cuando el cobro pasa por Mercado Pago

Cuando un pedido se cobra por Mercado Pago, la comisión de la plataforma SHALL declararse en la
transacción para que el proveedor la reparta automáticamente. El dinero de la comisión NO SHALL pasar
por la cuenta del fotógrafo ni requerir una transferencia posterior.

#### Scenario: Cobro acreditado con split

- **WHEN** se acredita un pago de Mercado Pago sobre un pedido con comisión devengada
- **THEN** el fotógrafo recibe el neto y la plataforma recibe su comisión, sin movimientos adicionales

#### Scenario: Registro de lo retenido

- **WHEN** el split reparte un pago acreditado
- **THEN** la cuenta corriente registra la comisión como retenida y deja de contarla como pendiente

#### Scenario: Comisión declarada como importe

- **WHEN** se declara la comisión de la plataforma en la transacción del proveedor
- **THEN** viaja como un importe en la moneda de la operación, calculado por el sistema, y el cliente
  no interviene en ese cálculo por ningún camino

#### Scenario: Comisión que superaría el monto de la operación

- **WHEN** la comisión calculada resultara mayor al monto total de la operación
- **THEN** el sistema lo rechaza antes de llamar al proveedor, con un error propio que explica el
  motivo

### Requirement: El cobro se verifica contra la cuenta en la que efectivamente entró

Al procesar un cobro del proveedor, el sistema SHALL verificar que la cuenta receptora informada
corresponde a la cuenta vinculada del tenant dueño del pedido. Si no corresponde, el pedido NO SHALL
acreditarse: SHALL quedar señalado para revisión manual, con ambas identificaciones registradas.

#### Scenario: Cobro acreditado en una cuenta que no es la del dueño del pedido

- **WHEN** llega un cobro verificado cuya cuenta receptora no es la vinculada al tenant del pedido
- **THEN** el pedido queda en revisión manual, no acreditado, y la bitácora registra la cuenta
  informada y la esperada

#### Scenario: Cobro en la cuenta correcta

- **WHEN** la cuenta receptora informada coincide con la vinculada al tenant del pedido
- **THEN** el cobro se acredita normalmente

### Requirement: Los cobros se concilian contra los registros del proveedor

La plataforma SHALL poder contrastar sus propios registros de cobro y de comisión contra el reporte
que emite el proveedor por cada vendedor vinculado, y SHALL señalar las diferencias en ambos
sentidos: operaciones que el proveedor registró y el sistema no, y operaciones que el sistema
registró y el proveedor no.

#### Scenario: Cobro que el sistema no registró

- **WHEN** el reporte del proveedor incluye un cobro acreditado que no tiene entrada en la bitácora
- **THEN** la diferencia queda señalada para revisión, con los datos necesarios para identificar la
  operación

#### Scenario: Conciliación sin diferencias

- **WHEN** los cobros y comisiones del reporte del proveedor coinciden con los registros del sistema
- **THEN** la conciliación se cierra sin diferencias y queda constancia del período conciliado

### Requirement: La cuenta corriente refleja devengado, retenido y pendiente

Cada tenant SHALL tener una cuenta corriente que registre, como movimientos individuales, la comisión
devengada por cada pedido, la retenida por cada cobro con split, las reversiones, y las liquidaciones.
El saldo pendiente SHALL ser derivable de esos movimientos.

#### Scenario: Consulta del saldo

- **WHEN** el fotógrafo consulta su cuenta corriente
- **THEN** ve el saldo pendiente y el detalle de los movimientos que lo componen, con el pedido que
  originó cada uno

#### Scenario: Ventas mixtas

- **WHEN** el fotógrafo vende diez pedidos, seis cobrados por Mercado Pago y cuatro por transferencia
- **THEN** la comisión de los seis figura retenida y la de los cuatro figura pendiente

#### Scenario: Reconstrucción del saldo

- **WHEN** se recalcula el saldo a partir de los movimientos registrados
- **THEN** coincide con el saldo informado

### Requirement: El saldo pendiente se liquida de forma visible

El saldo pendiente SHALL poder liquidarse descontándolo de cobros posteriores por Mercado Pago o
registrando una liquidación directa. Toda liquidación SHALL quedar como movimiento con su origen,
monto, momento y autor.

#### Scenario: Liquidación contra ventas siguientes

- **WHEN** un fotógrafo con saldo pendiente cobra un pedido nuevo por Mercado Pago
- **THEN** el saldo pendiente se reduce y el movimiento queda registrado

#### Scenario: Liquidación registrada manualmente

- **WHEN** la plataforma registra que el fotógrafo saldó su cuenta por fuera del sistema
- **THEN** el saldo se reduce y queda un movimiento con el autor y el momento

#### Scenario: El fotógrafo entiende qué debe

- **WHEN** el fotógrafo consulta un saldo pendiente
- **THEN** puede identificar qué pedidos lo originaron y por qué no se retuvo en el momento del cobro

### Requirement: La tasa de comisión es configurable y admite excepciones por tenant

La plataforma SHALL tener una tasa de comisión general y SHALL poder definir una tasa distinta para un
tenant puntual, incluida una tasa de cero. La tasa aplicable a un pedido SHALL quedar registrada con
el devengamiento.

#### Scenario: Tenant con tasa propia

- **WHEN** un tenant tiene una tasa propia definida y confirma un pedido
- **THEN** la comisión se calcula con su tasa, no con la general

#### Scenario: Período sin comisión

- **WHEN** un tenant tiene tasa cero
- **THEN** sus pedidos no devengan comisión y sus cobros por Mercado Pago no declaran comisión de
  plataforma

#### Scenario: Sin tasa propia

- **WHEN** un tenant no tiene tasa propia definida
- **THEN** se aplica la tasa general de la plataforma

### Requirement: El fotógrafo solo ve su propia cuenta corriente

Un fotógrafo SHALL acceder únicamente a los movimientos y el saldo de su propio tenant. La visión
consolidada de todos los tenants SHALL estar restringida a la administración de la plataforma.

#### Scenario: Consulta de un fotógrafo

- **WHEN** un fotógrafo consulta la cuenta corriente
- **THEN** obtiene únicamente movimientos de su tenant

#### Scenario: Consulta consolidada

- **WHEN** la administración de la plataforma consulta la visión consolidada
- **THEN** obtiene el saldo por tenant de todos los tenants

