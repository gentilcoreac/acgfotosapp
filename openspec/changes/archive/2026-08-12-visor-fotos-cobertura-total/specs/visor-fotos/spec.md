## ADDED Requirements

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
