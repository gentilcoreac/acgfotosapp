## Purpose

Garantiza que los datos de un tenant no sean legibles ni modificables desde el contexto de otro
—incluido el contexto anónimo, que hoy es la excepción que abre el filtro— y define cómo una
consulta declara explícitamente que necesita cruzar tenants.

## ADDED Requirements

### Requirement: El aislamiento entre tenants aplica en todo contexto

Las consultas sobre entidades multi-tenant SHALL quedar acotadas al tenant del contexto vigente. Un
contexto sin tenant establecido —anónimo— NO SHALL obtener acceso ampliado a datos de ningún tenant.

#### Scenario: Lectura anónima de una entidad multi-tenant

- **WHEN** un request sin autenticar consulta una entidad multi-tenant por el camino de consulta
  normal
- **THEN** no obtiene filas de ningún tenant

#### Scenario: Lectura autenticada

- **WHEN** un usuario del tenant A consulta una entidad multi-tenant por el camino normal
- **THEN** obtiene únicamente filas del tenant A, sin importar cuántos tenants existan

#### Scenario: Contexto de sistema

- **WHEN** un proceso de fondo establece contexto de sistema para el tenant A y consulta por el
  camino normal
- **THEN** obtiene únicamente filas del tenant A

### Requirement: El acceso cross-tenant es una declaración explícita por consulta

Una consulta que necesite atravesar tenants SHALL declararlo en su propia definición. NO SHALL
existir ninguna condición del contexto que amplíe el alcance de una consulta que no lo declaró.

#### Scenario: Consulta que declara el cruce

- **WHEN** una consulta declara explícitamente que ignora el filtro de tenant
- **THEN** obtiene filas de todos los tenants, y esa capacidad es visible en la definición de la
  consulta

#### Scenario: Consulta que no lo declara, en contexto anónimo

- **WHEN** una consulta que no declara el cruce se ejecuta en contexto anónimo
- **THEN** su alcance no se amplía por el hecho de que el contexto sea anónimo

### Requirement: Los flujos anónimos de autenticación siguen operando

Los flujos que necesitan resolver una identidad antes de conocer su tenant —inicio de sesión,
recuperación de contraseña, confirmación de cuenta, renovación de token y canje de código de
acceso— SHALL seguir funcionando, declarando su acceso cross-tenant de forma explícita.

#### Scenario: Inicio de sesión

- **WHEN** un usuario de un tenant no raíz inicia sesión con credenciales válidas
- **THEN** la sesión se establece correctamente con su tenant

#### Scenario: Recuperación de contraseña

- **WHEN** un usuario de un tenant no raíz solicita recuperar su contraseña y luego la restablece
- **THEN** ambas operaciones se completan correctamente

#### Scenario: Canje de código de acceso

- **WHEN** una familia canjea un código de acceso válido de un tenant no raíz
- **THEN** obtiene su sesión de familia con el tenant y los participantes correctos

#### Scenario: Renovación de token

- **WHEN** se renueva un token con un refresh token válido de un tenant no raíz
- **THEN** la renovación se completa correctamente
