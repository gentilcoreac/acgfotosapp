# e2e/ — Suite E2E (Playwright)

Tests end-to-end del cliente. **Plan, estrategia y catálogo de casos:** [`../Docs/e2e/`](../Docs/e2e/).

## Estructura

```
e2e/
├── global-setup.ts # reseed determinista antes de la corrida (sqlcmd; degrada elegante)
├── fixtures/   # auth.ts (loginAs: login fresco por test, NO storageState) + test-data.ts (creds del seed)
├── pages/      # Page Objects (login/shell/table/usuario-edit/impersonation-dialog/error-snack)
├── seed/       # e2e-extras.sql (datos sintéticos) + e2e-reset.sql (purga datos transitorios)
├── specs/      # los tests (auth, session, auth-roles, app-context, impersonation, theme*, usuarios-crud, errors)
└── tsconfig.json
```

> **Por qué no hay `setup/`/`.auth/` (storageState):** el refresh token rota con detección de replay
> (por ADR) → reutilizar un storageState entre tests lo volvería flaky. Cada test que necesita sesión
> loguea fresco con `loginAs` en su `beforeEach`. Detalle en [`../Docs/e2e/README.md`](../Docs/e2e/README.md) §4.

## Cómo correr

Requiere **front (ng serve) + API levantados**. La API es la que apunte la `apiUrl` del
`app.config.json` que sirva el front.

```powershell
# 1) API apuntada a AcgFotos_TestE2E (repo API, en su terminal — detener IIS Express dev antes):
$env:ASPNETCORE_ENVIRONMENT = "E2E"
$env:ASPNETCORE_URLS = "http://localhost:30000"
$env:Security__PasswordHasherIterations = "1000"
dotnet run --project AcgFotos.Api/AcgFotos.Api.csproj --no-launch-profile
```

```bash
npm run start          # 2) front en http://localhost:4200 (otra terminal)
npm run e2e            # 3) corre la suite completa (headless, Edge)
npm run e2e:ui         # modo UI interactivo
npm run e2e:debug      # paso a paso (inspector)
npm run e2e:report     # abre el último reporte HTML
```

Variables de entorno útiles: `E2E_BASE_URL` (default `http://localhost:4200`),
`E2E_ROOT_USER` / `E2E_ROOT_PASS` (credenciales del seed), `E2E_DB_SERVER` (default `localhost`),
`E2E_DB_NAME` (default `AcgFotos_TestE2E`), `E2E_RESEED=0` (desactiva el reseed inicial).

> **Reseed entre corridas:** el `globalSetup` ([`global-setup.ts`](./global-setup.ts)) deja la base en
> el estado base antes de la corrida (purga usuarios `e2e*` que el CRUD pudiera haber dejado y re-aplica
> `e2e-extras.sql`, idempotente), vía `sqlcmd`. **Degrada elegante:** si no hay `sqlcmd` en el PATH avisa
> y sigue (los tests que mutan son auto-contenidos). Requiere `sqlcmd` + `AcgFotos_TestE2E` accesible.

> **Datos — base dedicada `AcgFotos_TestE2E`** (ver [`../Docs/e2e/base-de-tests.md`](../Docs/e2e/base-de-tests.md)).
> Se crea una vez: `dotnet ef database update` apuntado ahí + correr el catálogo
> (`Api/Docs/postman/seed.sql` sin el `USE`) + [`seed/e2e-extras.sql`](./seed/e2e-extras.sql)
> (tenant 2 + `userb` no-root con licencia + `usersinlic` sin licencia). El front **no** cambia (sigue
> pegando a `:30000`); solo la API apunta a la test DB. Los specs por rol (AUTH-02/03, AUTHZ) requieren
> esta base; contra dev (todo root) no son deterministas.
