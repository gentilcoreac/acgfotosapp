## Why

`AuthorizationEnabled=false` en dev y producción. El filtro `EndpointAuthoritation` existe, funciona y
está testeado —hay un host de pruebas propio con el flag en `true` y siete archivos de test que ya lo
ejercitan— pero en la aplicación real está apagado, así que **la matriz de permisos por endpoint no
se evalúa nunca**.

La consecuencia práctica: cualquier JWT válido puede llamar cualquier endpoint. Eso ya se detectó en
Fase 2, cuando un token de familia recién canjeado pudo borrar la foto de otro participante por el
endpoint admin. Se resolvió con una defensa puntual (`FamiliaSessionGuard` en los AppServices del
vertical) y se decidió posponer el rollout completo. Esa decisión se revierte ahora, por dos motivos:

1. **Cada endpoint que se construye con el flag apagado es deuda.** El módulo de pagos suma endpoints
   nuevos que, sin esto, nacerían sin mapeo de permisos y habría que retrofittearlos después, de
   memoria y sin la señal de un 403 que indique qué falta.
2. **Los errores tienen que aparecer ahora, no en el deploy.** Con el flag prendido, un endpoint sin
   mapear falla de inmediato y de forma evidente durante el desarrollo. Apagado, el mismo problema se
   descubre el día que un usuario real no puede operar.

El costo está acotado y la mecánica ya existe: `discover` enumera el catálogo de endpoints
automáticamente, así que el trabajo es de datos —diseñar la taxonomía de permisos y mapear— no de
construir maquinaria.

## What Changes

- **BREAKING (operativo)**: `AuthorizationEnabled` pasa a `true`. Todo endpoint no anónimo pasa a
  exigir que el usuario tenga un permiso que lo cubra. Root no se ve afectado: `CheckPemissions()` lo
  exime salvo cuando impersona.
- Se define la **taxonomía de permisos** de la aplicación: qué permisos existen y qué área funcional
  cubre cada uno, para la plataforma heredada completa y para el vertical Fotos.
- Se siembra el **catálogo completo**: `gen_Endpoints` poblado desde `discover`, y las filas de
  permiso→endpoint y rol→permiso para los ~165 endpoints existentes.
- Los seeds (`TestSeed.sql`, `dev-alta-fotografo.sql`) incluyen la matriz completa, de modo que una
  base nueva nace con autorización funcionando.
- Los tests que autentican como usuario no-root reciben los permisos que su escenario necesita.
- `FamiliaSessionGuard` **se conserva**: la defensa en profundidad no se retira al prender el flag,
  porque protege contra que alguien vuelva a apagarlo.
- **Administración del propio tenant**: el fotógrafo pasa a poder gestionar **su** tenant (incluidos
  logos, colores y hoja de estilos), **sus** usuarios (alta, bloqueo, reenvío de confirmación de
  cuenta) y **sus** grupos de usuarios, sin ser root. Las operaciones genuinamente cross-tenant
  —listar todos los tenants, impersonar, administrar el catálogo de la plataforma— siguen siendo
  root-only.

## Capabilities

### New Capabilities

- `autorizacion-por-endpoint`: la garantía de que un usuario autenticado solo puede ejecutar los
  endpoints que sus permisos cubren, y la taxonomía de permisos que define esa cobertura.

### Modified Capabilities

Ninguna capability existente cambia de comportamiento observable: las pantallas y los flujos siguen
haciendo lo mismo para quien tiene permiso. Lo que cambia es que quien no lo tiene deja de poder.

## Impact

**Configuración**

- `AuthorizationEnabled` pasa a `true` en `appsettings.json`.

**Datos / seeds**

- `gen_Permisos`: hoy tiene una sola fila (`PermisoRoot`). Pasa a tener la taxonomía completa.
- `gen_Endpoints`: hoy vacío, se puebla por `discover`.
- Tablas de permiso→endpoint y rol→permiso: hoy sin filas para nada que no sea root.
- `AcgFotos.Api.IntegrationTests/Infrastructure/TestSeed.sql` y
  `backend/scripts/dev-alta-fotografo.sql`.

**Tests**

- ~525 tests. Los que autentican como `root` no se ven afectados (root bypassa). Los que usan usuarios
  no-root sí: `TestData.UserB` aparece en ~151 call sites y `AdminB` en ~61.

**Frontend**

- Verificar que un 403 por falta de permiso se presenta con un mensaje claro y no como un error
  genérico. El filtro ya devuelve 403 con la forma estándar `{ message, errors }` precisamente para
  eso, pero conviene ejercitarlo.

**Riesgo**

El modo de falla es ruidoso: un endpoint sin mapear da 403 inmediato, no un acceso indebido
silencioso. El error posible es de más restricción, no de menos — que es la dirección barata para
equivocarse. La contracara honesta es el volumen: es el change que más superficie toca de los tres de
esta tanda.
