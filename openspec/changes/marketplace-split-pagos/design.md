## Context

Ver `proposal.md` § Why para la motivación y el agujero de la transferencia. Acá el estado del que se
parte y las restricciones.

**Depende de `pagos-mercado-pago`**, que deja tres cimientos: el receptor del cobro resuelto por un
servicio que recibe el tenant (D8b), la comisión devengada por pedido con tasa configurable, y el
webhook con aislamiento de tenant explícito (D4).

**Restricción externa que no controlamos**: Split de Pagos exige requisitos previos y aprobación
comercial de Mercado Pago. El trámite se inicia al comenzar y corre en paralelo. El código se escribe
sin la aprobación; lo que la necesita es la verificación real, que por eso va al final del desglose.

**El perfil de riesgo del sistema cambia acá.** Hasta este change, una filtración de la base expone
fotos. A partir de acá expone la capacidad de operar sobre el dinero de N fotógrafos. Todo lo que
sigue está diseñado con esa premisa.

## Goals / Non-Goals

**Goals**

- Que cada fotógrafo cobre en su cuenta sin que la plataforma custodie dinero ajeno.
- Que el ingreso de la plataforma no dependa del método de pago que elija la familia.
- Que las credenciales de terceros estén protegidas a la altura de lo que habilitan.
- Que la capa pueda desplegarse dormida si la aprobación de Mercado Pago se demora.

**Non-Goals**

- Facturación electrónica, integración con AFIP o emisión de comprobantes.
- Planes, precios escalonados o gestión comercial de la relación con cada fotógrafo.
- Onboarding autogestionado de fotógrafos nuevos (alta de tenant, verificación de identidad).
- Reportes de negocio consolidados más allá del saldo por tenant.
- Métodos de cobro que no sean Mercado Pago o el registro manual ya existente.

## Decisions

### D1 — La comisión se devenga al confirmar el pedido, no al cobrar

El devengamiento ocurre cuando se confirma el pedido, con la tasa vigente en ese momento, y queda
congelado —mismo criterio que ADR-07 usa para el precio—.

**Por qué**: es lo que tapa el agujero de la transferencia. Si la comisión naciera del pago, el
método de cobro que más le conviene al fotógrafo (transferencia, donde no pierde ni la comisión de
Mercado Pago ni la de la plataforma) sería el que deja a la plataforma en cero. Ese incentivo no
requiere mala fe para activarse: cualquiera que haga la cuenta llega ahí.

Devengando sobre el pedido, al fotógrafo le da igual el método y puede ofrecerle a la familia el que
le sirva. La plataforma cobra por la venta que hizo posible, no por mover dinero — que además es la
descripción correcta de lo que aporta.

**Consecuencia**: hace falta una cuenta corriente. Es el costo de esta decisión y se asume.

### D2 — El saldo se deriva de movimientos inmutables

La cuenta corriente es un libro de movimientos —devengado, retenido, revertido, liquidado— y el saldo
es su pliegue. No hay un campo "saldo" que se sume y se reste.

**Por qué**: es el mismo criterio de D1 de `pagos-mercado-pago` para el estado de pago, por las mismas
razones. Un saldo que se muta es un saldo que en algún momento va a discrepar de sus movimientos, y
cuando eso pasa con dinero, no hay forma de saber cuál de los dos tiene razón. Con el pliegue, la
pregunta "¿por qué debo esto?" siempre tiene respuesta.

Se admite un saldo persistido como caché denormalizada, siempre que sea reconstruible y exista un
camino que lo recalcule.

### D3 — Vinculación por autorización, nunca por credencial tipeada

El fotógrafo vincula su cuenta autorizando a la plataforma en Mercado Pago. La plataforma no pide ni
acepta credenciales copiadas del panel de desarrolladores.

**Por qué, más allá de la comodidad**:

| | Credencial tipeada | Autorización |
|---|---|---|
| Lo que termina guardando la plataforma | Credencial de poder total sobre la cuenta | Credencial acotada a lo que el marketplace necesita |
| Cómo corta el fotógrafo la relación | Pidiéndole a la plataforma que la borre | Revocando desde su panel, sin intermediarios |
| Si la base se filtra | Credenciales sin vencimiento | Credenciales que vencen y rotan |
| Qué tiene que hacer un fotógrafo no técnico | Navegar un panel de desarrolladores | Un clic y aprobar |

Y el argumento que cierra la discusión: **Split de Pagos lo exige**. Mercado Pago necesita constancia
de que el vendedor autorizó al marketplace; no alcanza con poseer su credencial.

### D4 — El retorno de la autorización se ata a un valor de un solo uso

Al iniciar la vinculación se genera un valor no adivinable, asociado al tenant y con vencimiento
corto. El retorno solo se acepta si trae ese valor, no fue usado y no expiró.

**Qué previene**: que un atacante induzca a un fotógrafo a completar un flujo de autorización que
termine vinculando **la cuenta del atacante** al tenant de la víctima —o la del fotógrafo al tenant del
atacante—. Sin esa atadura, el retorno es un endpoint que acepta vincular lo que le manden.

Es el mismo tipo de defensa que el `state` de cualquier flujo de autorización, y la razón por la que
existe en el estándar.

### D5 — Las credenciales de terceros se cifran con clave fuera de la base

Cifrado en reposo, con la clave en el almacén de secretos del entorno, nunca en la base ni en la
configuración versionada.

**Por qué no alcanza con proteger la base**: si la clave vive en la misma base, cifrar no agrega nada
contra el escenario que importa —que alguien obtenga una copia de la base—. Separar la clave del dato
es lo que convierte una filtración de base en algo inútil para el atacante.

Nunca se registran en logs, nunca viajan al cliente, y el estado de vinculación que ve el fotógrafo
expone datos identificatorios de la cuenta, jamás la credencial.

### D6 — La renovación es proactiva y la caída es visible

Las credenciales se renuevan antes de vencer, sin intervención. Si la renovación falla de forma
definitiva —típicamente porque el fotógrafo revocó desde Mercado Pago— la vinculación se marca como
caída, se le informa, y los eventos afectados dejan de ofrecer pago online.

**Por qué importa el aviso**: una vinculación caída en silencio se descubre cuando una familia no
puede pagar. El costo lo paga el fotógrafo en ventas perdidas y la plataforma en confianza.

### D7 — La capa se despliega dormida si la aprobación no llega

Con el marketplace deshabilitado, el sistema opera como lo deja `pagos-mercado-pago`: un receptor,
comisión en cero, sin split. Encenderlo es configuración, no despliegue de código nuevo.

**Por qué se diseña así**: la aprobación comercial de Mercado Pago no la controlamos ni sabemos cuánto
tarda. Un diseño que solo funciona aprobado convierte una demora ajena en un bloqueo total. Con el
interruptor, la demora es una funcionalidad que todavía no se encendió.

### D8 — El webhook resuelve la credencial por tenant, no por proceso

La verificación server-to-server de una notificación usa la credencial vinculada del tenant dueño del
pedido, resuelta después de establecer el contexto de sistema (D4 de `pagos-mercado-pago`).

Esto refuerza por qué D8 de aquel change descartó el estado estático global del SDK: acá directamente
sería incorrecto, porque cada notificación necesita **una credencial distinta** según a quién
pertenezca el pedido.

## Threat model

Suma al de `pagos-mercado-pago`; no lo reemplaza.

| # | Amenaza | Defensa | Decisión |
|---|---|---|---|
| M1 | Filtración de la base con credenciales de N fotógrafos | Cifrado con clave fuera de la base | D5 |
| M2 | Vincular una cuenta ajena a un tenant, o la propia a un tenant ajeno | Valor de un solo uso atado al tenant, con vencimiento | D4 |
| M3 | Reutilizar un retorno de autorización capturado | El valor se consume al usarse | D4 |
| M4 | Credencial de un tenant usada para operar sobre otro | Resolución por tenant en cada llamada, sin estado compartido | D8 |
| M5 | Cobrar en la cuenta equivocada | La preferencia se crea con la credencial del dueño del evento | D8 |
| M6 | Manipular la comisión desde el cliente | La tasa y el cálculo viven en el servidor; el cliente no aporta ninguno | D1 |
| M7 | Evadir la comisión empujando transferencia | El devengamiento no depende del método de pago | D1 |
| M8 | Un fotógrafo consultando el saldo de otro | La cuenta corriente se scopea por tenant como cualquier entidad | — |
| M9 | Alterar el saldo directamente | El saldo se deriva de movimientos inmutables | D2 |
| M10 | Credencial expuesta en logs o respuestas de error | Nunca se registran ni se devuelven | D5 |

## Risks / Trade-offs

- **La aprobación de Mercado Pago puede demorarse o negarse** → D7: la capa se despliega dormida y el
  sistema funciona igual con un receptor. Ningún trabajo se descarta. Es el riesgo que motivó ese
  diseño.
- **La cuenta corriente es contabilidad, y la contabilidad mal hecha se paga cara** → D2 la vuelve
  reconstruible: ante una discrepancia siempre se puede recalcular desde los movimientos y comparar.
- **Cobrar comisión sobre pedidos pagados por transferencia exige confianza en el dato del pedido** →
  Si un fotógrafo registrara pedidos falsos o los cancelara para evadir, el devengamiento lo sigue
  (las cancelaciones revierten). Detectar abuso sistemático es una capa de negocio posterior, no de
  este change.
- **La comisión se suma a la de Mercado Pago y el fotógrafo pierde más por venta** → Es una decisión
  comercial, no técnica. El sistema la hace visible: el fotógrafo puede ver exactamente cuánto pagó de
  comisión y contra qué ventas.
- **Guardar credenciales de terceros implica responsabilidad legal sobre ellas** → Fuera del alcance
  técnico, pero conviene que la relación con cada fotógrafo lo contemple por escrito.

## Migration Plan

1. Iniciar el trámite de Split de Pagos con Mercado Pago. Corre en paralelo a todo lo demás.
2. Modelo y migración: cuenta de cobro, credenciales cifradas, movimientos de cuenta corriente.
3. Flujo de autorización completo, verificado contra el ambiente de pruebas de Mercado Pago.
4. Split en la creación de preferencias, detrás del interruptor de marketplace.
5. Cuenta corriente y liquidación.
6. Verificación real del split — **requiere la aprobación**; si no llegó, se posterga solo este paso.
7. **Rollback**: apagar el interruptor de marketplace devuelve el sistema al comportamiento de
   `pagos-mercado-pago`. Las credenciales vinculadas quedan y no se usan. Los movimientos de cuenta
   corriente se conservan.

## Open Questions

- **Tasa de comisión**: el número es una decisión comercial que no bloquea el diseño — la tasa es
  configurable y admite excepciones por tenant desde el día uno. Conviene definirla mirando lo que
  cobran FullFoto y mifotoar, que ya está anotado como ítem de relevamiento en
  `docs/05-notas-abiertas.md`.
- **Política de liquidación del saldo pendiente**: cada cuánto se reclama, si hay un mínimo para
  facturar, qué pasa si un fotógrafo deja de operar con saldo. Son reglas de negocio que se van a
  aclarar con el segundo fotógrafo real; el modelo de movimientos las soporta sin cambios.
