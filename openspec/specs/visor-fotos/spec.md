# visor-fotos Specification

## Purpose

Permite ver en grande cualquier foto elegida desde una grilla de miniaturas y recorrer desde ahí las
demás fotos de esa misma colección, con un comportamiento único en toda la aplicación en vez de uno
distinto por pantalla.

## Requirements

### Requirement: Apertura desde una miniatura

Donde se muestre una grilla de miniaturas de fotos, el sistema SHALL permitir abrir cualquiera de ellas
en tamaño grande. La foto abierta SHALL ser la que el usuario eligió.

#### Scenario: Abrir una foto de la grilla

- **WHEN** el usuario activa una miniatura de la grilla
- **THEN** el sistema muestra esa foto en grande, ocupando el espacio disponible de la pantalla

#### Scenario: Foto entera, sin recorte ni scroll

- **WHEN** el sistema muestra una foto en grande
- **THEN** la foto se ve completa dentro del área disponible, sin recortarse y sin exigir scroll

### Requirement: Recorrido de la colección desde el visor

Desde una foto abierta, el sistema SHALL permitir pasar a la anterior y a la siguiente de la misma
colección sin volver a la grilla. La colección SHALL ser la que el usuario está viendo, incluidos los
filtros que tenga aplicados.

El sistema SHALL indicar la posición dentro de la colección.

#### Scenario: Pasar a la siguiente foto

- **WHEN** el usuario pide la foto siguiente desde el visor
- **THEN** el visor muestra la siguiente foto de la colección sin cerrarse

#### Scenario: Colección filtrada

- **WHEN** el usuario tiene un filtro aplicado en la grilla y abre una foto
- **THEN** el recorrido incluye únicamente las fotos que el filtro deja visibles

#### Scenario: Extremos de la colección

- **WHEN** el usuario está en la primera o en la última foto
- **THEN** el visor no ofrece avanzar más allá del extremo

#### Scenario: Posición visible

- **WHEN** el visor muestra una foto de una colección
- **THEN** el usuario puede ver en qué posición está y cuántas fotos hay

### Requirement: Acciones propias de cada contexto

El visor SHALL admitir que cada pantalla aporte sus propias acciones y su propia forma de mostrar la
foto, sin que eso cambie el comportamiento de apertura y recorrido.

El visor SHALL NO habilitar en un contexto acciones que ese contexto no ofrece.

#### Scenario: Contexto administrativo

- **WHEN** un administrador abre una foto desde la galería o desde el detalle de un pedido
- **THEN** dispone de sus acciones propias —alternar entre la vista del cliente y el original, y
  descargar el original— además del recorrido

#### Scenario: Contexto de familia

- **WHEN** una familia abre una foto de su álbum o de su carrito
- **THEN** dispone de sus elementos propios —el sello con el nombre de la familia, la fricción
  anti-copia y el agregado al carrito— además del recorrido

#### Scenario: El sello de familia se limita a la foto

- **WHEN** la foto se muestra completa dentro de un contenedor más ancho o más alto que su propia
  proporción (deja bandas vacías a los costados o arriba/abajo)
- **THEN** el sello con el nombre de la familia cubre únicamente el área donde está la foto, sin
  extenderse sobre las bandas vacías

#### Scenario: El visor de familias no accede al original

- **WHEN** una familia usa el visor
- **THEN** sólo se muestra el derivado con marca de agua, sin ninguna vía hacia el archivo original

### Requirement: Consistencia entre pantallas

El comportamiento de apertura, recorrido, cierre y posición SHALL ser el mismo en todas las pantallas
que muestren miniaturas de fotos.

#### Scenario: Misma foto desde distintas pantallas

- **WHEN** el usuario abre la misma foto desde dos pantallas distintas que la muestran
- **THEN** el visor se comporta igual en ambas, salvo por las acciones propias de cada contexto

### Requirement: Operable con teclado

El visor SHALL ser operable con teclado: recorrer la colección y cerrarlo.

#### Scenario: Recorrido y cierre por teclado

- **WHEN** el usuario usa el teclado con el visor abierto
- **THEN** puede pasar a la foto anterior o siguiente y cerrar el visor sin usar el puntero

### Requirement: Apertura de una imagen aislada

El visor SHALL admitir abrir una única imagen sin colección (por ejemplo, un QR generado o una vista
previa de archivo). En ese caso, el visor SHALL NO ofrecer navegación anterior/siguiente ni indicador
de posición.

#### Scenario: Abrir una imagen sin colección

- **WHEN** el usuario activa una imagen que no pertenece a una grilla ni a un conjunto navegable
- **THEN** el visor la muestra sola, sin flechas de recorrido ni contador de posición

### Requirement: Cobertura en toda la aplicación

Toda pantalla que muestre una imagen —sea una foto de un evento, un código QR generado, una vista
previa de un archivo a subir o una muestra renderizada de una configuración— SHALL ofrecer ampliarla al
activarla, salvo elementos puramente de branding (como el logo del tenant).

#### Scenario: Imagen fuera del dominio de fotos de evento

- **WHEN** el usuario activa una imagen que no es una foto de un evento (QR, vista previa de archivo,
  muestra renderizada)
- **THEN** el sistema la abre en grande, con el mismo criterio de imagen aislada u colección según
  corresponda

#### Scenario: Elemento de branding

- **WHEN** el usuario hace click sobre el logo del tenant en el layout o el login
- **THEN** el sistema no lo amplía: no es contenido de un evento
