# Base de datos de tests para E2E — análisis y plan

## Cómo correr la suite

Ver [`../../e2e/README.md`](../../e2e/README.md) — instrucciones completas con los comandos exactos.

Resumen: detener IIS Express dev → levantar API con env vars apuntando a `AcgFotos_TestE2E`
(Windows auth, Kestrel :30000) → `npm run start` → `npm run e2e`.

---

> Estado: **EJECUTADA** (2026-06-22). `AcgFotos_TestE2E` creada y sembrada (catálogo + userb/usersinlic);
> los 5 casos por rol (AUTH-02/03, AUTHZ-01/02/03) corren **verdes** contra ella. Este doc explica el
> análisis, las decisiones y cómo se reproduce. Cómo correr la suite: [`../../e2e/README.md`](../../e2e/README.md).
>
> **Hecho:** (1) esquema vía `dotnet ef database update` → `AcgFotos_TestE2E`. (2) catálogo = `seed.sql`
> (sin el `USE [AcgFotos]`). (3) extras sintéticos = [`../../e2e/seed/e2e-extras.sql`](../../e2e/seed/e2e-extras.sql)
> (tenant 2 activo + `userb` no-root con licencia adminCliente + Rol 2 → menú "Administración Cliente" +
> `usersinlic` no-root sin licencia). (4) API levantada con connection strings → `AcgFotos_TestE2E`.
> **Hecho también (2026-06-23):** reset entre corridas (Playwright `global-setup` + `seed/e2e-reset.sql`,
> ver abajo) y login acelerado (PBKDF2 a 1000 iter en el perfil de tests). **Pendiente:** si se quiere,
> versionar el catálogo como `e2e-catalog-seed.sql` (hoy se reusa `seed.sql` directo).

## Por qué hace falta

Los E2E corren contra **API + front reales**. Hoy apuntan a **dev (`AcgFotos`)**, cuyo seed es **todo
root**: no hay usuarios no-root (con/sin licencia) ni un tenant con branding. Por eso quedan en
`test.fixme` / pendientes:

- **AUTH-02** (no-root con licencia → menú acotado), **AUTH-03** (no-root sin licencia → bloqueado)
- **AUTHZ-01/02/03** (ruteo por permisos / menú por permisos / fallback)
- **THEME-01/02/03** (branding de un tenant concreto, re-tema al impersonar)
- Todo el **CRUD/THEME mutante** de fases 2-4 (necesita reset entre corridas).

## Hallazgo clave (no subestimar)

El `TestSeed.sql` de la suite de API (`Api/AcgFotos.Api.IntegrationTests/Infrastructure/TestSeed.sql`)
**ya tiene** lo que necesita el login por rol: `userb` (no-root, tenant 2 activo, **con licencia**),
`adminb`, tenants 1/2/3, etc. — password de todos: `Root@AcgFotos2026!`. **Pero NO siembra menús ni la
cadena permiso→endpoint→rol**, que es de donde el **front** arma el menú (`menus/principal`,
`menus/allowed-routes`). Con ese seed, un no-root logueado por la UI vería un **menú vacío**:

- sirve para AUTH-02/03 (login mecánico),
- **no** alcanza para AUTHZ-02/03 (menú representativo) → necesita además sembrar **menús + grants**.

Falta también: un **no-root SIN licencia en tenant activo** (para AUTH-03; `userc` no sirve: tiene
licencia Visualizador activa, no es un usuario sin licencia). El tenant con branding ya está sembrado
(tenant 2, colores/logo/favicon, para THEME-01/02/03).

## ¿Se sincroniza la DB de prod a la de tests? (cómo se hace en general)

**No** se hace una sync **viva** prod→test para tests automatizados. Razones: (a) **no-determinismo**
(prod cambia y rompe tests sin que toques nada), (b) **privacidad** (datos reales), (c) tamaño/costo,
(d) los tests deben **ser dueños de sus datos**. El patrón estándar separa **dos clases de datos**:

- **Catálogo / configuración** (cambia poco): menús, permisos, endpoints, roles, aplicaciones, tipos de
  licencia y sus relaciones (permiso→endpoint, rol→permiso, menú→permiso). Esto **sí** querés igual a la
  config real → se toma **un snapshot UNA vez** de la DB de referencia (hoy **dev `AcgFotos`**; mañana
  prod si existe), se guarda como **seed versionado en el repo** (`e2e-catalog-seed.sql`) y queda
  **constante** entre corridas. Se **regenera solo cuando la config real cambia** (alta de menú/permiso).
  Es exactamente tu "esos datos pueden dejarse siempre igual".
- **Datos transaccionales** (usuarios, tenants, registros): **sintéticos y deterministas**, creados por
  el seed de tests (root + no-root con licencia + no-root sin licencia + tenant con branding). Nunca
  copiados de prod.

(Variante que algunos usan: un **refresh unidireccional del solo-catálogo** prod→test por script,
anonimizado, nunca datos de usuarios. Para nuestra escala alcanza el snapshot versionado.) **Decisión
(Alberto):** menús/permisos = **snapshot del catálogo actual, versionado y constante**; usuarios/tenants
de test = sintéticos.

## Opciones

| Opción                                            | Qué                                                                                                                                                        | Pro                                                                     | Contra                                                                                                        |
| ------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------- |
| **A. DB dedicada `AcgFotos_TestE2E`** (recomendada) | base propia, esquema por migraciones EF, datos por un seed SQL que **extiende** `TestSeed.sql` (+ menús/grants, + no-root sin licencia, + tenant branding) | aislada de dev y de la suite xUnit; determinista; reusa el seed probado | hay que armar el seed de menús/grants y la rutina de levantado                                                |
| B. Reusar `AcgFotos_Tests` (la de xUnit)            | apuntar la API E2E a esa base                                                                                                                              | cero seed nuevo                                                         | **conflicto**: la suite xUnit la **resetea por test** (Respawn) → no pueden convivir; y le faltan menús igual |
| C. Clonar la DB de dev                            | copia de `AcgFotos`                                                                                                                                          | menús/permisos completos "gratis"                                       | **no determinista** (dev muta); arrastra datos reales; pesada                                                 |

## Reset entre corridas — IMPLEMENTADO (2026-06-23)

Se eligió **reset directo de DB** (sobre el endpoint debug-only): el dev box ya corre SQL Server local
con auth Windows y así se creó la base, así que reusamos `sqlcmd` y **no** se agrega código/superficie a
la API. Lo orquesta el **`global-setup` de Playwright** ([`../../e2e/global-setup.ts`](../../e2e/global-setup.ts)):

1. `seed/e2e-reset.sql` → purga los datos transitorios de los tests mutantes (usuarios `e2e*` del CRUD
   que hubieran quedado por una corrida interrumpida; el catálogo, constante, NO se re-siembra).
2. `seed/e2e-extras.sql` → re-aplica (idempotente) los datos sintéticos por si faltaran.

**Degrada elegante:** si `sqlcmd` no está en el PATH (o `E2E_RESEED=0`), avisa y la suite **continúa**
(los tests que mutan son auto-contenidos). Config por env: `E2E_DB_SERVER` (default `localhost`),
`E2E_DB_NAME` (default `AcgFotos_TestE2E`). Para CI con base efímera, el `global-setup` es el punto donde
enchufar el reset (o migrar al endpoint debug-only si se prefiere no acoplar `sqlcmd`).

## Recomendación (con las decisiones de Alberto)

1. **DB dedicada `AcgFotos_TestE2E`** (Opción A) — instancia local de SQL Server (la misma de dev).
2. **Seed = dos capas:**
   - **Catálogo (constante, versionado):** `e2e-catalog-seed.sql` = **snapshot del catálogo actual de
     dev `AcgFotos`** (menús, permisos, endpoints, roles, aplicaciones, tipos de licencia + relaciones
     permiso→endpoint, rol→permiso, menú→permiso). Igual a la config real; se regenera solo si cambia.
   - **Usuarios/tenants de test (sintéticos):** root + `userb` (no-root con licencia) + un **no-root sin
     licencia** en tenant activo + un **tenant con branding**. Base: `TestSeed.sql` (ya tiene userb), +
     el no-root-sin-lic y el tenant branded.
3. **Reset** = diferido (Fase 1 read-only solo siembra 1 vez); endpoint debug-only cuando lleguen los
   mutantes.
4. La API E2E corre con connection strings → `AcgFotos_TestE2E`; el front sirve un `app.config.json` con
   `apiUrl` a esa API.

## Primeros pasos (ejecución)

> **Estado (2026-06-25):** `AcgFotos_TestE2E` ya está creada, migrada y sembrada. Los pasos a continuación describen cómo reproducirla desde cero si fuera necesario.

```bash
# 1) Crear el esquema en la DB dedicada (repo API, connection strings → AcgFotos_TestE2E)
dotnet ef database update --project AcgFotos.Base.SqlMigrations --startup-project AcgFotos.Api

# 2a) Generar el snapshot del catálogo desde dev (una vez; versionar en el repo):
#     scriptear data-only de las tablas de catálogo de AcgFotos → e2e-catalog-seed.sql
#     (mssql-scripter, o SSMS "Generate Scripts → Data only", o INSERTs por query)
# 2b) Aplicar a AcgFotos_TestE2E: e2e-catalog-seed.sql + usuarios/tenants de test (sintéticos)

# 3) Levantar la API apuntada a AcgFotos_TestE2E + el front, y correr la suite
npm run e2e
```

Tablas de catálogo a snapshotear (verificar el set exacto contra el esquema): `gen_Aplicaciones`,
`gen_Menus` (+ relación menú↔permiso), `gen_Permisos`, `gen_Endpoints`, `gen_PermisoEndpoints`,
`gen_Roles`, `gen_RolPermisos`, `gen_TipoLicencia`, `gen_TipoLicenciaRoles`, y `gen_Parametros` de
config (p. ej. Email). Los **usuarios/tenants/licencias** NO entran al snapshot (son sintéticos).

Al tener el seed: en `e2e/fixtures/test-data.ts` ya están `USER_B` (= `userb`/`Root@AcgFotos2026!`) y
`USER_SIN_LIC` (pendiente de sembrar) — quitar `.fixme` de `specs/auth-roles.spec.ts` y afinar las
aserciones de menú contra el seed real.

## Decisiones — estado

- ✅ DB dedicada **`AcgFotos_TestE2E`** (Opción A), instancia SQL local.
- ✅ **Menús/permisos = snapshot del catálogo actual, versionado y constante** (no sync viva de prod).
- ✅ Reset **IMPLEMENTADO** (2026-06-23): reset directo de DB por `sqlcmd` desde el `global-setup`
  (`seed/e2e-reset.sql` + re-aplicar `e2e-extras.sql`), con degradación elegante. (Se descartó el
  endpoint debug-only para no sumar superficie a la API.)
- ⬜ Definir la **herramienta de snapshot** (mssql-scripter vs SSMS Generate-Scripts vs INSERTs por
  query) y verificar el **set exacto de tablas de catálogo**.

## Performance / flakiness (techo conocido)

La suite corre **serial** contra la API real + `AcgFotos_TestE2E` en el dev box (que a la vez corre
API + SQL Server + `ng serve` + Edge). Cada test hace **login** + queries de menú.

**Fix aplicado (2026-06-23): login acelerado.** El login dejó de ser CPU-bound: la API lee la clave de
config **`Security:PasswordHasherIterations`** (en E2E = `1000`, ver `IdentityHelper`) y el seed
(`e2e-extras.sql`, incluido root) trae los hashes ya a **1000 iteraciones** (Identity lee el nº de
iteraciones del propio hash al verificar → login ~instantáneo desde el primer request). Solo en la base
E2E — **nunca** en prod (debilita el hashing). Con eso, `retries` en local bajó a **1** (2 en CI por la
variabilidad del runner); `expect.timeout` 15s + el timeout amplio de `expectLoggedIn` (30s) quedan como
colchón. Otras palancas si hiciera falta: correr por grupos, o subir el paralelismo **solo** si se aísla
el estado por dato (hoy comparten `AcgFotos_TestE2E`).
