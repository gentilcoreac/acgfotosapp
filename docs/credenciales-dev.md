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

## Bases de datos (PostgreSQL local, migrado 2026-07-24 — ver ADR-14 en docs/04-decisiones.md)

Instalado con `winget`/instalador oficial EDB (PostgreSQL 17.10), servicio de Windows
`postgresql-x64-17`, puerto 5432. El backend ya corre sobre Npgsql (`DatabaseFactory`); las
connection strings de dev/E2E están en `appsettings.Development.json`/`appsettings.E2E.json`.

| Dato | Valor |
|---|---|
| Usuario superusuario | `postgres` |
| Clave | `Root@AcgFotos2026!` (misma clave dev del resto del proyecto) |
| Host / puerto | `localhost` / `5432` |
| `psql` / herramientas | `C:\Program Files\PostgreSQL\17\bin\` (no quedó agregado al PATH del usuario) |

| Base | Uso |
|---|---|
| `AcgFotos` | desarrollo (host `dotnet run`) — esquema migrado, **sin datos sembrados todavía** (pendiente aparte, ver notas abiertas) |
| `AcgFotos_Tests` | tests de integración (se crea/migra sola; Respawn la resetea) |
| `AcgFotos_TestE2E` | e2e Playwright (`appsettings.E2E.json`) |

Connection string (Npgsql): `Host=localhost;Port=5432;Database=<Base>;Username=postgres;Password=Root@AcgFotos2026!`
— el nombre de la base va tal cual (`AcgFotos`, `AcgFotos_Tests`...); Postgres solo pliega a
minúscula los identificadores SIN comillas dentro de SQL, no el parámetro de conexión.

SQL Server quedó dado de baja (era transitorio, ADR-09/ADR-14) — ya no hace falta tenerlo instalado
para este proyecto.

## URLs

- API: http://localhost:30000 (perfil `http`; Swagger en `/swagger`)
- Front: http://localhost:4200 (`npm start`)

## Otros

- JWT key de dev: en `backend/AcgFotos.Api/appsettings.Development.json` (regenerada para este
  repo, no compartida con el código base).
- Email deshabilitado en dev (`Email:EmailEnabled=false`); si se habilita, la credencial SMTP va a
  User Secrets (`dotnet user-secrets set "Email:Password" "..."`).
