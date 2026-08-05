## Purpose

Permite al fotógrafo definir la marca de agua que protege las fotos que ven las familias — su
contenido, colocación e intensidad — sin tocar configuración ni reiniciar el sistema, y verla
aplicada antes de comprometerla sobre las fotos de un evento.

## ADDED Requirements

### Requirement: Perfil de marca de agua

El sistema SHALL permitir administrar perfiles de marca de agua propios del estudio (tenant). Un
perfil SHALL tener un nombre, entre 1 y 3 capas, y la indicación de si la marca se aplica también al
thumbnail. Un perfil SHALL poder marcarse como default del estudio, y SHALL haber a lo sumo uno
default por estudio.

#### Scenario: Alta de un perfil con una capa

- **WHEN** el fotógrafo crea un perfil con nombre y una capa válida
- **THEN** el sistema lo guarda y queda disponible para asignarlo a eventos

#### Scenario: Perfil sin capas

- **WHEN** el fotógrafo intenta guardar un perfil sin ninguna capa
- **THEN** el sistema rechaza el guardado indicando que un perfil necesita al menos una capa

#### Scenario: Perfil con más de 3 capas

- **WHEN** el fotógrafo intenta guardar un perfil con 4 o más capas
- **THEN** el sistema rechaza el guardado indicando el máximo de 3 capas

#### Scenario: Cambio del perfil default del estudio

- **WHEN** el fotógrafo marca como default un perfil y ya había otro default
- **THEN** el perfil anterior deja de ser default y queda uno solo

#### Scenario: Aislamiento entre estudios

- **WHEN** un usuario de un estudio lista o consulta perfiles
- **THEN** el sistema devuelve únicamente los perfiles de su propio estudio

### Requirement: Capa de marca de agua

Cada capa SHALL consistir en una imagen PNG con transparencia más sus parámetros de colocación:
modo de colocación (repetida en mosaico o en una de 9 posiciones fijas), escala expresada en
porcentaje del ancho de la foto, margen, ángulo, opacidad y modo de fusión
(`Normal`, `Superponer`, `Diferencia`).

El sistema SHALL componer las capas en orden sobre la foto y NO SHALL dibujar texto: la imagen de la
capa es el contenido definitivo de la marca.

#### Scenario: Capa repetida en mosaico

- **WHEN** una capa está configurada como repetida y se genera un derivado
- **THEN** la marca aparece repetida cubriendo toda la superficie de la imagen, incluidas las
  esquinas, con el ángulo configurado

#### Scenario: Capa en posición fija

- **WHEN** una capa está configurada en una de las 9 posiciones fijas con un margen dado
- **THEN** la marca aparece una sola vez, en esa posición, respetando el margen

#### Scenario: Varias capas combinadas

- **WHEN** un perfil tiene un logo en posición fija y una trama repetida
- **THEN** el derivado muestra ambas, compuestas en el orden definido por el perfil

#### Scenario: Modo de fusión aplicado

- **WHEN** una capa usa el modo de fusión `Diferencia`
- **THEN** la marca permanece visible tanto sobre zonas claras como oscuras de la foto

### Requirement: Validación del asset de capa al subirlo

El sistema SHALL validar el archivo de una capa al subirlo, y SHALL rechazar lo que no pueda
componer. La validación SHALL determinar el formato real decodificando el archivo, no confiando en su
extensión ni en el content-type declarado. El sistema SHALL inspeccionar las dimensiones declaradas
del archivo ANTES de decodificarlo por completo, y SHALL rechazar el archivo cuando excedan el techo
configurado.

El sistema SHALL guardar el asset sin pérdida (PNG), nunca en un formato con pérdida.

Los avisos SHALL indicar el número concreto y la consecuencia real, no un mensaje genérico.

#### Scenario: Archivo que no es una imagen

- **WHEN** el fotógrafo sube como capa un archivo que no es una imagen decodificable
- **THEN** el sistema lo rechaza indicando que el archivo no es una imagen válida

#### Scenario: Imagen con dimensiones desproporcionadas

- **WHEN** el archivo subido declara dimensiones que superan el techo configurado
- **THEN** el sistema lo rechaza sin llegar a decodificar la imagen completa

#### Scenario: Archivo que excede el peso máximo

- **WHEN** el archivo subido supera el peso máximo configurado
- **THEN** el sistema lo rechaza indicando el peso del archivo y el máximo admitido

#### Scenario: Logo sin canal alfa

- **WHEN** el fotógrafo sube un logo sin transparencia
- **THEN** el sistema lo acepta y le avisa que se va a componer con su fondo sólido

#### Scenario: Logo más chico que lo que pide la escala

- **WHEN** el fotógrafo sube un logo cuyo ancho es menor al que requiere la escala elegida
- **THEN** el sistema lo acepta y le avisa con el ancho real del logo, el ancho necesario y que a esa
  escala se va a ver borroso

#### Scenario: El asset se conserva sin pérdida

- **WHEN** el sistema almacena el asset de una capa
- **THEN** lo guarda en PNG, sin recodificarlo a un formato con pérdida

### Requirement: Resolución en cascada del perfil aplicable

Al generar los derivados de una foto, el sistema SHALL resolver qué perfil aplicar en este orden:
perfil asignado al evento, luego perfil default del estudio, luego la configuración de la aplicación.

Cuando no haya ningún perfil cargado, el sistema SHALL producir derivados equivalentes a los del
comportamiento previo a esta capacidad.

#### Scenario: Evento con perfil propio

- **WHEN** el evento de la foto tiene un perfil asignado
- **THEN** el sistema aplica ese perfil, aunque el estudio tenga otro default

#### Scenario: Evento sin perfil propio

- **WHEN** el evento no tiene perfil asignado y el estudio tiene un default
- **THEN** el sistema aplica el default del estudio

#### Scenario: Sin ningún perfil cargado

- **WHEN** ni el evento ni el estudio tienen perfil
- **THEN** el sistema aplica la marca definida en la configuración de la aplicación

### Requirement: Guarda de acceso administrativo

La administración de perfiles de marca de agua SHALL estar disponible únicamente para usuarios
administradores del estudio. Una sesión de familia SHALL ser rechazada en todos los métodos de esta
capacidad.

#### Scenario: Sesión de familia intenta administrar perfiles

- **WHEN** una petición con sesión de familia invoca cualquier operación de perfiles de marca
- **THEN** el sistema responde 403 sin ejecutar la operación

### Requirement: El asset se rasteriza al tamaño máximo de uso

El sistema SHALL componer las capas escalando el asset únicamente hacia abajo. El asset SHALL
generarse o validarse contra el mayor tamaño en píxeles que podría llegar a necesitarse según la
resolución de publicación y la escala configuradas.

#### Scenario: Composición sobre un derivado

- **WHEN** el sistema compone una capa sobre un derivado
- **THEN** el asset se reduce o se usa a tamaño natural, nunca se agranda

### Requirement: Guardar un perfil no modifica fotos existentes

Guardar, editar o eliminar un perfil de marca de agua SHALL NO alterar los derivados de fotos ya
procesadas. Los cambios SHALL aplicarse a las fotos que se procesen a partir de ese momento.

#### Scenario: Edición de un perfil en uso

- **WHEN** el fotógrafo edita un perfil asignado a un evento con fotos ya procesadas
- **THEN** esas fotos conservan sus derivados actuales hasta que se pida explícitamente regenerarlas

### Requirement: Aviso al guardar un perfil sin protección efectiva

Cuando un perfil resulte en fotos sin protección visible, el sistema SHALL advertirlo en términos de
la consecuencia real antes de guardarlo.

#### Scenario: Perfil con todas las capas en opacidad nula

- **WHEN** el fotógrafo guarda un perfil cuyas capas no dejan marca visible
- **THEN** el sistema le advierte que las familias van a ver esas fotos sin ninguna protección

### Requirement: El servidor no depende de fuentes instaladas

La generación de derivados SHALL NO requerir tipografías instaladas en el sistema operativo del
servidor.

#### Scenario: Servidor sin fuentes del sistema

- **WHEN** el sistema genera derivados en un entorno sin tipografías instaladas
- **THEN** la marca de agua se aplica correctamente
