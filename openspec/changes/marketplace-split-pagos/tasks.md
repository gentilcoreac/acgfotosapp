## 0. Prerrequisito externo — arrancar ya, corre en paralelo

- [ ] 0.1 Iniciar el trámite de Split de Pagos con Mercado Pago: relevar requisitos previos, contactar al equipo comercial y registrar qué piden y qué plazo estiman
- [ ] 0.2 Anotar en el documento funcional el estado del trámite y de qué depende (bloquea solo el grupo 8)
- [ ] 0.3 Relevar qué comisión cobran FullFoto y mifotoar, como insumo para definir la tasa propia

## 1. Modelo: cuenta de cobro del fotógrafo

- [ ] 1.1 Entidad de cuenta de cobro por tenant: estado de vinculación, identificación de la cuenta en Mercado Pago, momentos de vinculación y última renovación
- [ ] 1.2 Almacenamiento de credenciales vinculadas, cifradas con clave fuera de la base (D5)
- [ ] 1.3 Entidad del valor de un solo uso de la vinculación: valor, tenant, vencimiento, consumido (D4)
- [ ] 1.4 Migración EF con `Down` completo
- [ ] 1.5 Verificar por consulta directa a la base que las credenciales no son legibles ni utilizables

## 2. Flujo de autorización

- [ ] 2.1 Inicio de la vinculación: generar el valor de un solo uso, asociarlo al tenant y redirigir a la autorización de Mercado Pago
- [ ] 2.2 Retorno de la autorización: validar el valor (existe, no consumido, no vencido, del tenant correcto), consumirlo, e intercambiar por las credenciales
- [ ] 2.3 Rechazar el retorno cuando el fotógrafo cancela la autorización, con mensaje que explique la consecuencia
- [ ] 2.4 Rechazar la vinculación si esa cuenta de Mercado Pago ya está vinculada a otro tenant
- [ ] 2.5 Desvinculación: eliminar credenciales, conservar historial de cobros y saldo, advertir si hay saldo pendiente
- [ ] 2.6 `FamiliaSessionGuard` en todos los métodos del AppService de vinculación
- [ ] 2.7 Verificar que ninguna respuesta de estado de vinculación incluye la credencial

## 3. Renovación y caída de la vinculación

- [ ] 3.1 Renovación proactiva antes del vencimiento, sin intervención del fotógrafo
- [ ] 3.2 Marcar la vinculación como caída cuando la renovación falla de forma definitiva
- [ ] 3.3 Avisar al fotógrafo de la caída y dejar de ofrecer pago online en sus eventos (D6)
- [ ] 3.4 Verificar que la credencial no aparece en logs por ningún camino de renovación ni de error

## 4. Comisión devengada

- [ ] 4.1 Tasa de comisión general de la plataforma, configurable
- [ ] 4.2 Tasa propia por tenant, opcional, incluida tasa cero
- [ ] 4.3 Devengar la comisión al confirmar el pedido, con la tasa vigente congelada junto al devengamiento (D1)
- [ ] 4.4 Revertir el devengamiento al cancelarse un pedido no cobrado
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
- [ ] 6.3 Declarar la comisión de la plataforma en la preferencia
- [ ] 6.4 No ofrecer pago online cuando el fotógrafo no tiene cuenta vinculada o la tiene caída
- [ ] 6.5 Al acreditarse un cobro con split, registrar la comisión como retenida en la cuenta corriente
- [ ] 6.6 La verificación server-to-server del webhook usa la credencial del tenant dueño del pedido, resuelta tras establecer el contexto de sistema

## 7. Frontend

- [ ] 7.1 Pantalla de cuenta de cobro: estado de vinculación, acción de vincular, acción de desvincular con su advertencia
- [ ] 7.2 Aviso visible cuando la vinculación está caída, con el camino para re-autorizar
- [ ] 7.3 Cuenta corriente del fotógrafo: saldo pendiente y detalle de movimientos con el pedido que originó cada uno
- [ ] 7.4 Administración de la plataforma: tasa general, excepciones por tenant, visión consolidada de saldos
- [ ] 7.5 En el flujo de la familia, ocultar el pago online cuando el fotógrafo no puede cobrar online

## 8. Verificación contra Mercado Pago — depende de la aprobación (0.1)

- [ ] 8.1 Vinculación real de una cuenta de prueba mediante el flujo de autorización completo
- [ ] 8.2 Cobro real con split: verificar que el fotógrafo recibe el neto y la plataforma su comisión
- [ ] 8.3 Verificar el orden de descuentos: primero la comisión de Mercado Pago, después la de la plataforma
- [ ] 8.4 Renovación real de credenciales
- [ ] 8.5 Revocación desde el panel de Mercado Pago: verificar que la vinculación se marca como caída
- [ ] 8.6 Si la aprobación no llegó, dejar constancia y desplegar con el interruptor apagado (D7)

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

## 10. Suites y documentación

- [ ] 10.1 Suite de integración del backend en verde
- [ ] 10.2 Suite unit del frontend + lint + build en verde
- [ ] 10.3 ADR en `docs/04-decisiones.md`: estrategia de monetización — comisión sobre el pedido y no sobre el pago, con el agujero de la transferencia que la motiva y los modelos descartados (suscripción pura, comisión solo sobre Mercado Pago, y cobrar todo la plataforma y liquidar, que la volvería intermediario financiero)
- [ ] 10.4 ADR en `docs/04-decisiones.md`: vinculación por autorización y custodia de credenciales de terceros
- [ ] 10.5 Ampliar el documento funcional `docs/06-cobros.md` con la parte de plataforma: qué cobra, cómo se retiene, cómo se lee la cuenta corriente y qué pasa con las ventas cobradas por fuera de Mercado Pago
- [ ] 10.6 Actualizar `docs/00-vision.md` y `docs/03-fases.md`: AcgFotos deja de ser la herramienta de un fotógrafo y pasa a ser una plataforma multi-fotógrafo con ingresos propios
- [ ] 10.7 Anotar en `docs/05-notas-abiertas.md` lo que queda fuera: facturación electrónica, planes comerciales, onboarding autogestionado de fotógrafos y detección de abuso
