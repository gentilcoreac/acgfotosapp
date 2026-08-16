## Purpose

Permite que cada fotógrafo vincule su propia cuenta de Mercado Pago para recibir el dinero de sus
ventas, mediante una autorización que él otorga y puede revocar, y define cómo la plataforma custodia
las credenciales de terceros que esa autorización produce.

## ADDED Requirements

### Requirement: El fotógrafo vincula su cuenta autorizando, no tipeando credenciales

El fotógrafo SHALL poder vincular su cuenta de Mercado Pago mediante un flujo de autorización con el
proveedor. La plataforma NO SHALL solicitar, aceptar ni almacenar credenciales que el fotógrafo haya
copiado a mano desde el panel del proveedor.

#### Scenario: Vinculación exitosa

- **WHEN** el fotógrafo inicia la vinculación, autoriza en Mercado Pago y regresa
- **THEN** su cuenta queda vinculada y la plataforma puede crear cobros que se acreditan en ella

#### Scenario: El fotógrafo rechaza la autorización

- **WHEN** el fotógrafo llega a la pantalla de autorización y la cancela
- **THEN** vuelve a la plataforma sin cuenta vinculada y con un mensaje que explica que el cobro
  online no estará disponible hasta vincularla

#### Scenario: Sin cuenta vinculada

- **WHEN** un evento de un fotógrafo sin cuenta vinculada recibe un pedido
- **THEN** la familia no ve la opción de pago online y solo puede coordinar el cobro con el fotógrafo

### Requirement: El retorno de la autorización se valida contra manipulación

El retorno del flujo de autorización SHALL validarse contra un valor de un solo uso, no adivinable,
generado por la plataforma al iniciarlo y asociado al tenant que lo inició. Un retorno cuyo valor no
coincida, ya haya sido usado, o haya expirado, SHALL rechazarse sin vincular ninguna cuenta.

#### Scenario: Retorno con valor inválido

- **WHEN** llega un retorno de autorización cuyo valor de un solo uso no corresponde a ninguna
  vinculación iniciada
- **THEN** se rechaza y no se vincula ninguna cuenta

#### Scenario: Reutilización del valor

- **WHEN** se reenvía un retorno de autorización cuyo valor ya fue consumido
- **THEN** se rechaza y no se modifica ninguna vinculación

#### Scenario: Intento de vincular una cuenta a un tenant ajeno

- **WHEN** llega un retorno cuyo valor fue emitido para el tenant A, en un contexto del tenant B
- **THEN** se rechaza y no se vincula ninguna cuenta

### Requirement: Las credenciales de terceros se custodian cifradas

Las credenciales obtenidas de la autorización SHALL almacenarse cifradas, con la clave de cifrado
fuera de la base de datos. NO SHALL exponerse al cliente, NO SHALL registrarse en logs ni en
respuestas de error, y NO SHALL ser legibles por una consulta directa a la base.

#### Scenario: Inspección de la base

- **WHEN** se consulta directamente la tabla que guarda las credenciales vinculadas
- **THEN** los valores están cifrados y no permiten operar contra el proveedor

#### Scenario: Error al operar contra el proveedor

- **WHEN** una llamada al proveedor con la credencial de un fotógrafo falla y el error se registra
- **THEN** ni el registro ni la respuesta contienen la credencial

#### Scenario: Consulta del estado de vinculación

- **WHEN** el fotógrafo consulta el estado de su cuenta vinculada
- **THEN** recibe el estado y los datos identificatorios de la cuenta, nunca la credencial

### Requirement: Las credenciales se renuevan antes de vencer

La plataforma SHALL renovar las credenciales vinculadas antes de su vencimiento, sin intervención del
fotógrafo. Si la renovación falla de forma definitiva, la vinculación SHALL marcarse como caída y el
fotógrafo SHALL ser notificado de que necesita volver a autorizar.

#### Scenario: Renovación exitosa

- **WHEN** una credencial vinculada se aproxima a su vencimiento
- **THEN** se renueva sin que el fotógrafo intervenga y los cobros siguen funcionando

#### Scenario: Renovación fallida de forma definitiva

- **WHEN** la renovación falla porque el fotógrafo revocó la autorización desde el proveedor
- **THEN** la vinculación queda marcada como caída, se le informa al fotógrafo, y los eventos afectados
  dejan de ofrecer pago online

### Requirement: El fotógrafo puede desvincular su cuenta

El fotógrafo SHALL poder desvincular su cuenta de cobro desde la plataforma. Al desvincular, las
credenciales almacenadas SHALL eliminarse. La desvinculación NO SHALL borrar el historial de cobros ni
el saldo de la cuenta corriente.

#### Scenario: Desvinculación

- **WHEN** el fotógrafo desvincula su cuenta
- **THEN** las credenciales se eliminan, el pago online deja de ofrecerse, y el historial de cobros y
  el saldo pendiente se conservan

#### Scenario: Desvinculación con saldo pendiente

- **WHEN** el fotógrafo desvincula su cuenta teniendo saldo pendiente
- **THEN** se le advierte que el saldo sigue vigente y cómo se va a liquidar

### Requirement: Una cuenta vinculada pertenece a un solo tenant

Una cuenta de cobro del proveedor NO SHALL quedar vinculada a más de un tenant simultáneamente.

#### Scenario: Intento de vincular una cuenta ya vinculada

- **WHEN** un fotógrafo intenta vincular una cuenta que ya está vinculada a otro tenant
- **THEN** la vinculación se rechaza con un mensaje que explica el motivo
