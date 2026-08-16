## Why

AcgFotos deja de pensarse como la herramienta de un fotógrafo y pasa a ser una plataforma para muchos.
Eso obliga a resolver dos cosas que hoy no existen: que **cada fotógrafo cobre en su propia cuenta** de
Mercado Pago, y que **la plataforma tenga ingresos** por las ventas que hace posibles.

Las dos están acopladas. Si cada uno cobra en su cuenta, el dinero nunca pasa por la plataforma, así
que no hay dónde retener una comisión — salvo que Mercado Pago la retenga en el origen. Para eso
existe **Split de Pagos**: el pago se reparte automáticamente en la misma transacción entre el
fotógrafo y la plataforma, sin que la plataforma custodie dinero ajeno. Esa distinción es lo que la
mantiene fuera del terreno de un intermediario financiero.

**El agujero que este diseño tapa**: si la comisión se cobrara solo sobre lo que pasa por Mercado
Pago, el fotógrafo tendría un incentivo directo a empujar la transferencia bancaria —donde no pierde
la comisión de MP ni la de la plataforma— y el ingreso de la plataforma se evaporaría. No hace falta
mala fe: cualquiera que haga la cuenta llega ahí. Como los competidores (FullFoto, mifotoar) aceptan
transferencia y hay que igualarlos, el agujero sería estructural.

La solución es conceptual antes que técnica: **la comisión no se cobra por procesar un pago, se cobra
por la venta que la plataforma hizo posible**. Se devenga al confirmarse el pedido, sin importar cómo
se cobre después. Con eso, al fotógrafo le da igual el método de pago y puede ofrecerle a la familia
el que le sirva.

## What Changes

- **Cuenta de cobro por fotógrafo**: cada tenant tiene su propia cuenta receptora de Mercado Pago,
  vinculada mediante autorización OAuth. La plataforma nunca pide ni almacena credenciales tipeadas a
  mano.
- **Split de Pagos**: las preferencias se crean contra la cuenta del fotógrafo con la comisión de la
  plataforma declarada, y Mercado Pago reparte automáticamente. El dinero **no pasa** por la cuenta de
  la plataforma.
- **Cuenta corriente del fotógrafo**: entidad de primera clase que registra comisión devengada,
  comisión ya retenida por el split y saldo pendiente. Es lo que permite cobrar sobre pedidos que se
  pagaron por fuera de Mercado Pago.
- **Liquidación del saldo**: el saldo pendiente se descuenta de las ventas siguientes por Mercado Pago
  o se factura; con visibilidad para el fotógrafo de cuánto debe y por qué.
- **Configuración de la tasa de comisión**, por plataforma y con posibilidad de excepción por tenant
  (planes distintos, acuerdos puntuales, período de prueba sin comisión).
- **Almacenamiento cifrado de los tokens de terceros**, con rotación y camino de revocación.

Depende del change `pagos-mercado-pago`, que aporta los cimientos: el receptor del cobro como
concepto y la comisión devengada por pedido.

## Capabilities

### New Capabilities

- `cuenta-cobro-fotografo`: vinculación de la cuenta de Mercado Pago de cada fotógrafo mediante
  autorización, su ciclo de vida (vinculada, renovada, revocada) y la custodia de las credenciales de
  terceros que eso implica.
- `comision-plataforma`: el devengamiento de la comisión de la plataforma sobre cada pedido, su
  retención automática cuando el cobro pasa por Mercado Pago, y la cuenta corriente que registra lo
  devengado, lo retenido y lo pendiente.

### Modified Capabilities

- `pago-online-mercado-pago`: la preferencia deja de crearse contra una cuenta única y pasa a crearse
  contra la cuenta del fotógrafo del pedido, declarando la comisión de la plataforma.

## Impact

**Backend**

- Entidades nuevas: cuenta de cobro por tenant, tokens de autorización, devengamiento de comisión,
  cuenta corriente y sus movimientos.
- Flujo de autorización OAuth con Mercado Pago: inicio, retorno, renovación y revocación.
- El cliente de Mercado Pago pasa a operar con la credencial del fotógrafo del pedido, no con una
  credencial de la plataforma.
- Cifrado en reposo de credenciales de terceros, con la clave fuera de la base.

**Frontend**

- Pantalla de vinculación de la cuenta de cobro, con su estado y la acción de desvincular.
- Visibilidad de la cuenta corriente para el fotógrafo.
- Administración de la tasa de comisión para la plataforma.

**Dependencia externa que no controlamos**

Split de Pagos exige requisitos previos y aprobación comercial de Mercado Pago. El trámite se inicia
al comenzar el change y corre en paralelo: el código puede escribirse sin la aprobación, lo que no
puede hacerse sin ella es la verificación real contra Mercado Pago, que por eso queda al final del
desglose. **Si la aprobación no llega a tiempo, la capa se despliega dormida** —comisión en cero y un
único receptor—, que es exactamente el estado que deja `pagos-mercado-pago`. Ningún trabajo se
descarta.

**Peso de seguridad**

Este change cambia el perfil de riesgo del sistema. Hasta ahora una filtración de la base exponía
fotos; a partir de acá expone **la capacidad de operar sobre el dinero de N fotógrafos**. El
almacenamiento cifrado, la rotación y la revocación dejan de ser buenas prácticas opcionales y pasan a
ser requisitos.
