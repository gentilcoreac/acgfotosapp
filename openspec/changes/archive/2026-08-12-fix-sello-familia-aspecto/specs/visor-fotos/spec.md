## MODIFIED Requirements

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
