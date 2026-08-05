# regeneracion-derivados Specification

## Purpose

Permite rehacer los derivados de las fotos ya subidas de un evento cuando cambia su marca de agua o
sus opciones de publicación, como acción explícita del fotógrafo y con el costo a la vista antes de
confirmarla.

## Requirements

### Requirement: Regeneración explícita por evento

El sistema SHALL ofrecer una acción para regenerar los derivados de las fotos de un evento. La
regeneración SHALL ser explícita: ninguna otra operación SHALL disparar el reprocesamiento masivo de
fotos.

#### Scenario: Regeneración solicitada

- **WHEN** el fotógrafo confirma la regeneración de un evento con fotos procesadas
- **THEN** el sistema encola esas fotos para reprocesarlas y las vuelve a marcar como pendientes

#### Scenario: Guardar configuración no regenera

- **WHEN** el fotógrafo guarda un perfil de marca o unas opciones de publicación
- **THEN** el sistema no encola ninguna foto para reprocesar

#### Scenario: Evento sin fotos procesadas

- **WHEN** el fotógrafo solicita regenerar un evento que no tiene fotos procesadas
- **THEN** el sistema informa que no hay nada para regenerar y no encola trabajo

### Requirement: El conteo se informa antes de confirmar

El sistema SHALL informar cuántas fotos se van a regenerar ANTES de encolar el trabajo, para que el
fotógrafo confirme conociendo el costo.

#### Scenario: Confirmación con el conteo por delante

- **WHEN** el fotógrafo abre la acción de regenerar sobre un evento con 412 fotos procesadas
- **THEN** el sistema le indica que se van a regenerar 412 fotos antes de pedirle confirmación

#### Scenario: Cancelación

- **WHEN** el fotógrafo ve el conteo y cancela
- **THEN** el sistema no encola ninguna foto

### Requirement: Las fotos regeneradas usan la configuración vigente

Al reprocesarse, cada foto SHALL usar el perfil de marca de agua y las opciones de publicación que
resuelvan en ese momento según la cascada evento → estudio → configuración de la aplicación.

#### Scenario: Regeneración tras cambiar el perfil del evento

- **WHEN** el fotógrafo asigna otro perfil al evento y luego regenera
- **THEN** los derivados resultantes muestran la marca del perfil nuevo

### Requirement: La regeneración no toca los originales

La regeneración SHALL reconstruir únicamente los derivados a partir del original almacenado. El
original SHALL permanecer sin modificaciones y SHALL NO exponerse.

#### Scenario: Original intacto tras regenerar

- **WHEN** se regeneran los derivados de una foto
- **THEN** el original conserva su contenido y sus metadatos originales

### Requirement: Visibilidad del progreso y de los fallos

Durante la regeneración el sistema SHALL reflejar el estado de procesamiento de cada foto. Una foto
cuyo reprocesamiento falle SHALL quedar en estado de error con su detalle, y SHALL NO interrumpir el
reprocesamiento de las demás.

#### Scenario: Una foto falla durante la regeneración

- **WHEN** el reprocesamiento de una foto falla
- **THEN** esa foto queda en error con el detalle y el resto del evento se regenera igual

#### Scenario: Progreso visible

- **WHEN** una regeneración está en curso
- **THEN** el fotógrafo puede ver en la galería del evento qué fotos siguen pendientes

### Requirement: Guarda de acceso administrativo

La regeneración SHALL estar disponible únicamente para usuarios administradores del estudio, y sólo
sobre eventos de su propio estudio. Una sesión de familia SHALL ser rechazada.

#### Scenario: Sesión de familia intenta regenerar

- **WHEN** una petición con sesión de familia invoca la regeneración
- **THEN** el sistema responde 403 sin encolar trabajo

#### Scenario: Evento de otro estudio

- **WHEN** un administrador solicita regenerar un evento que no pertenece a su estudio
- **THEN** el sistema rechaza la operación sin encolar trabajo
