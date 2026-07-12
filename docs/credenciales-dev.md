# Credenciales y datos de desarrollo (SOLO DEV — nada de esto va a producción)

> Doc temporal de referencia rápida. Todo lo listado ya está en el código/seeds del repo;
> cuando exista un entorno real, las credenciales productivas van a User Secrets / variables
> de entorno, jamás acá.

## Login de la app (dev y tests)

| Usuario | Clave | Rol |
|---|---|---|
| `root` | `Root@AcgFotos2026!` | Administrador (tenant 1, isRoot) |

Todos los usuarios sembrados en tests/e2e (`userb`, `userc`, `userb2`, etc. — ver
`backend/AcgFotos.Api.IntegrationTests/Infrastructure/TestSeed.sql`) usan la MISMA clave.

## Bases de datos (SQL Server localhost, Integrated Security)

| Base | Uso |
|---|---|
| `AcgFotos` | desarrollo (host `dotnet run`) |
| `AcgFotos_Tests` | tests de integración (se crea/migra sola; Respawn la resetea) |
| `AcgFotos_TestE2E` | e2e Playwright (`appsettings.E2E.json`) |

## URLs

- API: http://localhost:30000 (perfil `http`; Swagger en `/swagger`)
- Front: http://localhost:4200 (`npm start`)

## Otros

- JWT key de dev: en `backend/AcgFotos.Api/appsettings.Development.json` (regenerada para este
  repo, no compartida con el código base).
- Email deshabilitado en dev (`Email:EmailEnabled=false`); si se habilita, la credencial SMTP va a
  User Secrets (`dotnet user-secrets set "Email:Password" "..."`).
