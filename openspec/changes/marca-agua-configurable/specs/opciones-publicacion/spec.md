## Purpose

Permite al fotógrafo decidir con qué resolución y calidad se publican las fotos que ven las familias,
como eje independiente de la marca de agua: se puede querer una marca sutil con resolución baja o al
revés.

## ADDED Requirements

### Requirement: Opciones de publicación administrables

El sistema SHALL permitir administrar conjuntos de opciones de publicación propios del estudio. Un
conjunto SHALL definir el lado mayor del preview, el lado mayor del thumbnail y la calidad de
compresión de los derivados.

Un conjunto SHALL poder marcarse como default del estudio, y SHALL haber a lo sumo uno default por
estudio.

#### Scenario: Alta de un conjunto de opciones

- **WHEN** el fotógrafo crea un conjunto con lado mayor de preview, de thumb y calidad válidos
- **THEN** el sistema lo guarda y queda disponible para asignarlo a eventos

#### Scenario: Valores fuera de rango

- **WHEN** el fotógrafo intenta guardar un valor de resolución o calidad fuera del rango admitido
- **THEN** el sistema rechaza el guardado indicando el rango válido

#### Scenario: Cambio del conjunto default del estudio

- **WHEN** el fotógrafo marca como default un conjunto y ya había otro default
- **THEN** el conjunto anterior deja de ser default y queda uno solo

#### Scenario: Aislamiento entre estudios

- **WHEN** un usuario de un estudio lista o consulta opciones de publicación
- **THEN** el sistema devuelve únicamente las de su propio estudio

### Requirement: Resolución en cascada de las opciones aplicables

Al generar los derivados de una foto, el sistema SHALL resolver qué opciones de publicación aplicar
en este orden: las asignadas al evento, luego el default del estudio, luego la configuración de la
aplicación.

#### Scenario: Evento con opciones propias

- **WHEN** el evento de la foto tiene opciones de publicación asignadas
- **THEN** el sistema aplica esas opciones, aunque el estudio tenga otro default

#### Scenario: Evento sin opciones propias

- **WHEN** el evento no tiene opciones asignadas y el estudio tiene un default
- **THEN** el sistema aplica el default del estudio

#### Scenario: Sin ninguna opción cargada

- **WHEN** ni el evento ni el estudio tienen opciones de publicación
- **THEN** el sistema aplica la resolución y calidad definidas en la configuración de la aplicación

### Requirement: Las opciones de publicación son independientes del perfil de marca

Un evento SHALL poder combinar cualquier perfil de marca de agua con cualquier conjunto de opciones
de publicación. Asignar uno SHALL NO condicionar ni modificar el otro.

#### Scenario: Combinación independiente

- **WHEN** el fotógrafo asigna a un evento un perfil de marca y deja las opciones de publicación en
  "usar la del estudio"
- **THEN** el evento usa el perfil asignado y el conjunto default del estudio

### Requirement: Los derivados nunca agrandan el original

El sistema SHALL generar derivados reduciendo el original cuando su lado mayor supera el configurado,
y SHALL dejarlo en su tamaño natural cuando ya es menor.

#### Scenario: Original más chico que el lado mayor configurado

- **WHEN** se publica una foto cuyo lado mayor es menor que el configurado para el preview
- **THEN** el preview conserva las dimensiones del original, sin agrandarlo

### Requirement: Guarda de acceso administrativo

La administración de opciones de publicación SHALL estar disponible únicamente para usuarios
administradores del estudio. Una sesión de familia SHALL ser rechazada en todos los métodos de esta
capacidad.

#### Scenario: Sesión de familia intenta administrar opciones de publicación

- **WHEN** una petición con sesión de familia invoca cualquier operación de opciones de publicación
- **THEN** el sistema responde 403 sin ejecutar la operación

### Requirement: Guardar opciones no modifica fotos existentes

Guardar, editar o eliminar un conjunto de opciones de publicación SHALL NO alterar los derivados de
fotos ya procesadas.

#### Scenario: Edición de un conjunto en uso

- **WHEN** el fotógrafo edita un conjunto asignado a un evento con fotos ya procesadas
- **THEN** esas fotos conservan sus derivados actuales hasta que se pida explícitamente regenerarlas
