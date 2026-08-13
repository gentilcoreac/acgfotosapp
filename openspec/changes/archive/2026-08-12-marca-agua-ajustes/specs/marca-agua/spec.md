## ADDED Requirements

### Requirement: Nitidez de la composición de capas

Al componer una capa sobre una foto, el sistema SHALL preservar la nitidez del asset al rotarlo y
escalarlo: el resultado SHALL NO presentar bordes dentados ni artefactos de muestreo evitables por una
configuración de calidad razonable.

#### Scenario: Capa rotada y escalada

- **WHEN** el sistema compone una capa con ángulo distinto de 0 y una escala que reduce el asset
- **THEN** los bordes de la marca resultante se ven suavizados, sin el dentado de un muestreo sin
  filtrar

### Requirement: Separación configurable de la trama repetida

Una capa en modo repetido SHALL tener una separación configurable, expresada en porcentaje del tamaño
del propio tile: 100% significa marcas contiguas y valores mayores dejan hueco entre ellas.

La separación SHALL ser independiente de la escala: cambiar el tamaño de la marca SHALL NO alterar por
sí solo la cantidad de repeticiones sobre la foto.

El valor por defecto SHALL reproducir la densidad vigente antes de esta capacidad, de modo que un
perfil ya existente conserve su aspecto hasta que el fotógrafo cambie la separación explícitamente.

#### Scenario: Achicar la marca sin multiplicar las repeticiones

- **WHEN** el fotógrafo reduce la escala de una capa repetida sin tocar su separación
- **THEN** cada marca se ve más chica y la cantidad de repeticiones sobre la foto se mantiene

#### Scenario: Separar las marcas sin cambiar su tamaño

- **WHEN** el fotógrafo aumenta la separación de una capa repetida sin tocar su escala
- **THEN** las marcas conservan su tamaño y quedan más espaciadas, dejando ver más la foto

#### Scenario: Perfil existente sin separación explícita

- **WHEN** se procesan las fotos de un perfil creado antes de existir este parámetro
- **THEN** el resultado conserva la misma densidad que tenía antes

### Requirement: Coherencia entre la vista previa y el resultado

La vista previa del editor SHALL aplicar los mismos parámetros de colocación que usa la generación de
derivados, incluida la separación, de modo que lo que el fotógrafo ve al diseñar prediga el resultado
sobre las fotos.

#### Scenario: Separación reflejada en la vista previa

- **WHEN** el fotógrafo cambia la separación de una capa repetida en el editor
- **THEN** la vista previa muestra la nueva densidad, coincidente con la que se generará al procesar
