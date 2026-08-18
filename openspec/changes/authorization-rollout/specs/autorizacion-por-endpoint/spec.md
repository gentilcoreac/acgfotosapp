## Purpose

Garantiza que un usuario autenticado solo pueda ejecutar los endpoints que sus permisos cubren, y
define la taxonomía de permisos que determina esa cobertura para la plataforma y para el vertical
Fotos.

## ADDED Requirements

### Requirement: La autorización por endpoint se evalúa siempre

Toda petición a un endpoint no anónimo SHALL evaluarse contra los permisos efectivos del usuario. Un
usuario autenticado sin un permiso que cubra el endpoint SHALL recibir 403, y la acción NO SHALL
ejecutarse.

#### Scenario: Usuario sin el permiso requerido

- **WHEN** un usuario autenticado llama un endpoint que ninguno de sus permisos cubre
- **THEN** recibe 403 y la acción no se ejecuta

#### Scenario: Usuario con el permiso requerido

- **WHEN** un usuario autenticado llama un endpoint cubierto por alguno de sus permisos
- **THEN** la acción se ejecuta normalmente

#### Scenario: Endpoint anónimo

- **WHEN** se llama un endpoint marcado como anónimo
- **THEN** no se evalúan permisos y la acción se ejecuta

### Requirement: Root opera sin restricción de permisos

Un usuario root SHALL poder ejecutar cualquier endpoint sin necesidad de permisos asignados. Cuando
root impersona a otro usuario, SHALL evaluarse la autorización del usuario impersonado.

#### Scenario: Root sin permisos asignados

- **WHEN** root llama cualquier endpoint no anónimo
- **THEN** la acción se ejecuta

#### Scenario: Root impersonando a un usuario sin permiso

- **WHEN** root impersona a un usuario y llama un endpoint que los permisos de ese usuario no cubren
- **THEN** recibe 403

### Requirement: Una sesión de familia solo alcanza los endpoints marcados

Una sesión de familia SHALL poder ejecutar únicamente los endpoints explícitamente marcados como
habilitados para ella. Cualquier otro endpoint SHALL responder 403, sin consultar la matriz de
permisos de usuarios.

#### Scenario: Sesión de familia sobre un endpoint habilitado

- **WHEN** una sesión de familia llama un endpoint marcado como habilitado para familias
- **THEN** la acción se ejecuta

#### Scenario: Sesión de familia sobre un endpoint admin

- **WHEN** una sesión de familia llama un endpoint administrativo
- **THEN** recibe 403

### Requirement: El rol del fotógrafo cubre su operación completa

El rol con el que opera el fotógrafo SHALL tener permisos que cubran todos los endpoints necesarios
para su trabajo —eventos, grupos y participantes, fotos, tarjetas, pedidos, marca de agua y opciones
de publicación— sin necesitar permisos de administración de plataforma.

#### Scenario: El fotógrafo opera su vertical

- **WHEN** el fotógrafo recorre su operación completa: crear un evento, cargar grupos y
  participantes, subir fotos, generar tarjetas, consultar pedidos y ajustar la marca de agua
- **THEN** ninguna de esas acciones recibe 403

#### Scenario: El fotógrafo intenta administrar la plataforma

- **WHEN** el fotógrafo llama un endpoint de administración de plataforma que su rol no cubre
- **THEN** recibe 403

### Requirement: El fotógrafo administra su propio tenant sin ser root

Un usuario con el permiso de administración de su tenant SHALL poder editar los datos y el estilo de
su tenant —incluidos logos, colores y hoja de estilos—, administrar los usuarios de su tenant —alta,
bloqueo, desbloqueo y reenvío de confirmación de cuenta— y administrar sus grupos de usuarios. Ese
acceso SHALL limitarse a datos de su propio tenant.

#### Scenario: Cambio de identidad visual

- **WHEN** el fotógrafo edita los logos y colores de su tenant
- **THEN** el cambio se aplica sin intervención de un usuario root

#### Scenario: Alta de un usuario del propio negocio

- **WHEN** el fotógrafo da de alta un usuario para su negocio
- **THEN** el usuario queda creado en su tenant y él puede gestionarlo

#### Scenario: Recuperar un usuario propio bloqueado o sin confirmar

- **WHEN** un usuario del tenant queda bloqueado o sin confirmar su cuenta
- **THEN** el fotógrafo puede desbloquearlo y reenviarle la confirmación sin intervención de root

#### Scenario: No alcanza usuarios de otro tenant

- **WHEN** el fotógrafo lista o busca usuarios
- **THEN** obtiene únicamente usuarios de su propio tenant

#### Scenario: No alcanza operaciones de plataforma

- **WHEN** el fotógrafo intenta listar todos los tenants, impersonar a otro usuario o administrar el
  catálogo de la plataforma
- **THEN** recibe 403

#### Scenario: Tenant recién creado

- **WHEN** se crea un tenant nuevo con el rol por defecto
- **THEN** su usuario administrador puede autoadministrarse desde el primer ingreso

### Requirement: Un denegación por permiso queda auditada

Toda respuesta 403 por falta de permiso SHALL registrarse en la auditoría con el usuario, el endpoint
y el momento.

#### Scenario: Intento sin permiso

- **WHEN** un usuario llama un endpoint que sus permisos no cubren
- **THEN** queda un registro de auditoría con el usuario y el endpoint intentados

### Requirement: Una base nueva nace con la autorización operativa

El sembrado de una base nueva SHALL incluir el catálogo de endpoints, la taxonomía de permisos y las
asignaciones de permiso a rol necesarias para que los roles definidos operen sin 403 inesperados.

#### Scenario: Base recién sembrada

- **WHEN** se siembra una base nueva y un usuario de cada rol definido ejercita su operación
- **THEN** ninguno recibe 403 en las acciones que su rol contempla

### Requirement: El cambio de permisos se refleja sin reiniciar

Al modificarse los permisos de un usuario o de su rol, la autorización efectiva SHALL reflejar el
cambio sin requerir reinicio de la aplicación ni que el usuario vuelva a iniciar sesión.

#### Scenario: Permiso otorgado en caliente

- **WHEN** se le otorga a un rol un permiso que cubre un endpoint antes denegado
- **THEN** un usuario de ese rol pasa a poder ejecutarlo sin reiniciar ni reautenticarse

#### Scenario: Permiso revocado en caliente

- **WHEN** se le revoca a un rol un permiso
- **THEN** un usuario de ese rol pasa a recibir 403 en los endpoints que ese permiso cubría
