# Plan de testing E2E — Cliente AcgFotos

> Estado (2026-06-23): **Fases 0/1/2/3/4 implementadas — 37 casos E2E verdes.** Falta solo la
> mini-capacitación (y, a futuro, la Fase 5 de CI). Este doc define estrategia, arquitectura, convenciones
> y el roadmap; el catálogo de casos y su estado por caso viven en [`casos-e2e.md`](./casos-e2e.md).

---

## 1. Decisiones tomadas

| Decisión      | Elección                                          | Por qué                                                                                                                                                                                                                                               |
| ------------- | ------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Framework     | **Playwright Test nativo** (sin Cucumber/Gherkin) | Menos capas, mejor trace/debug/codegen, recomendado por la comunidad Angular. La guía interna previa era del **stack viejo** (Cucumber/Gherkin + Syncfusion/Bootstrap/NTLM) → no aplica al cliente nuevo y sin equipo QA dedicado el BDD es overhead. |
| Altitud       | **Capa fina de smoke** (~37 jornadas)             | Ya hay **376 tests de integración de API** + **~47 specs** de componente/servicio cubriendo validaciones y lógica fina. E2E exhaustivo por feature sería lento, frágil y redundante. Pirámide sana.                                                   |
| Datos/entorno | **Base de tests dedicada + seed**                 | API real apuntando a una base dedicada (estilo `AcgFotos_Tests`) con seed conocido y reseed entre corridas. Deterministas y sin ensuciar dev.                                                                                                           |

---

## 2. Dónde encaja E2E en la pirámide

El E2E **no** vuelve a probar lo que ya está cubierto abajo. Su trabajo es validar que las
**piezas integradas funcionan de punta a punta por la UI real** en las jornadas críticas: las que,
si se rompen, tiran abajo la app aunque cada unidad pase verde.

```
         ╱ E2E (Playwright) ╲          ~37 jornadas críticas cross-feature   ← ESTE PLAN
       ╱  componente/servicio ╲        ~47 specs (Karma+Jasmine, HTTP mock)
     ╱   integración de API     ╲      376 tests (xUnit + WebAppFactory + Respawn)
   ╱      unit puro              ╲     crypto/cookie/jwt/mappers
```

Regla de oro: **si un caso se puede probar barato en API o en componente, NO va a E2E.** El E2E
cubre el “pegamento” (router + guards + interceptor + store de sesión + theming + API real) que
ninguna capa de abajo ejercita junta.

---

## 3. Contexto del front (verificado 2026-06-22)

- **E2E:** 37 casos implementados (Fases 0-4). El catálogo previo de 13 smoke
  (`Api/Docs/casos-de-prueba/sections/650-e2e.md`) quedó absorbido y ampliado acá.
- **Front:** Angular 20.3, Material **M3**, wrappers `tbi-*`, JWT **bearer en memoria** + refresh por
  **cookie HttpOnly** (restauración de sesión en F5). Config runtime en
  `public/configurations/app.config.json` (`apiUrl`). Rutas: `login`, `confirmar-cuenta`,
  `reconnecting`, y bajo layout autenticado: `tipos-licencias`, `parametros`,
  `parametros-valor-tenant`, `roles`, `permisos`, `usuarios`, `aplicaciones`, `menus`, `tenants`,
  `grupos`, `endpoints`, home (`''`). `**` **redirige a home** (no hay página `/404`).
- **Selectores:** **no hay un solo `data-testid`** en el código. Material asocia label↔input, así que
  `getByLabel`/`getByRole` funcionan (ver §6).

---

## 4. Arquitectura de la suite

Los tests viven en el **repo del Cliente** (apuntan siempre a la última versión del front y comparten
historial con el código que prueban).

Estructura **actual** (Fases 0-4 completas):

```
Cliente/
├── e2e/
│   ├── fixtures/
│   │   ├── auth.ts                # loginAs(page, creds): login por test (ver "Patrones clave")
│   │   └── test-data.ts          # credenciales por rol (ROOT, USER_B, USER_SIN_LIC, USER_C)
│   ├── pages/                    # Page Objects (POM)
│   │   ├── login.page.ts  shell.page.ts  table.page.ts
│   │   ├── usuario-edit.page.ts  impersonation-dialog.page.ts
│   ├── seed/
│   │   └── e2e-extras.sql        # extras sintéticos sobre el catálogo (tenant 2 + userb/usersinlic/userc + pageusers)
│   ├── specs/
│   │   ├── auth.spec.ts          # AUTH-01/04/05/07 (arranque anónimo)
│   │   ├── session.spec.ts       # AUTH-06/08/09/10 (sesión/guards/restauración/reconexión)
│   │   ├── auth-roles.spec.ts    # AUTH-02/03 + AUTHZ-01/02/03 (no-root, AcgFotos_TestE2E)
│   │   ├── app-context.spec.ts   # APP-01/02/03 (selector de aplicación)
│   │   ├── impersonation.spec.ts # IMP-01/02/03/04
│   │   ├── theme.spec.ts         # THEME-04/05 (theming post-login)
│   │   ├── theme-branding.spec.ts# THEME-01/02/03 (branding por tenant)
│   │   ├── usuarios-crud.spec.ts # CRUD-01..08 (alta/editar/borrar/buscador/columnas/paginación/validación)
│   │   ├── errors.spec.ts        # ERR-01/02/03/04 (coalescencia / Ver detalle+ref / spinner / ruta inexistente)
│   └── tsconfig.json             # module commonjs/node (ver §9 nota técnica)
├── playwright.config.ts
```

**Estado: 37 casos verde** (Fases 0/1/2/3/4). Ver estado por caso en
[`casos-e2e.md`](./casos-e2e.md).

### Patrones clave

- **POM (Page Object Model):** cada pantalla/componente recurrente encapsula sus selectores e
  interacciones. Los specs no tocan selectores crudos. `table.page.ts` (tbi-table: buscador, columnas,
  paginación, acciones de fila) es transversal y lo reusan los listados; equivale a los “componentes
  globales” de la guía vieja pero apuntado al stack nuevo (`tbi-*`), no a Syncfusion.
- **Login fresco por test (NO `storageState` compartido):** cada test que necesita sesión llama a
  `loginAs(page, creds)` en su `beforeEach`. **Por qué no storageState:** el refresh token **ROTA** con
  detección de replay (ventana de gracia de 10s, por ADR) → una cookie de sesión guardada y reutilizada
  entre tests sería single-use y volvería la suite **flaky** (el 2º test que la use la encontraría
  rotada/revocada). El login fresco deja la cookie de refresh viva en el contexto del navegador (la
  restauración en F5/401 funciona dentro de ese mismo test) y **aísla** cada test. Los specs de **login
  en sí** arrancan anónimos (no usan `loginAs`).
- **Sin esperas arbitrarias:** nada de `waitForTimeout`. Auto-espera de Playwright + `expect(locator)` +
  `waitForURL`. (Ver §7.)
- **Aislamiento por test:** cada test corre en su propio `BrowserContext`. Los tests que **mutan** datos
  (CRUD, fases siguientes) crean entidades con sufijo único (timestamp) y limpian, o dependen del reseed.

### Config (resumen)

```ts
// playwright.config.ts
baseURL: 'http://localhost:4200',
trace: 'on-first-retry', screenshot: 'only-on-failure', video: 'retain-on-failure',
const EDGE = { ...devices['Desktop Edge'], channel: 'msedge' }; // Edge del sistema, sin descargar Chromium
projects: [{ name: 'e2e', testMatch: /specs[\\/].*\.spec\.ts/, use: EDGE }],
// webServer opcional: levantar `ng serve` (reuseExistingServer en local).
```

---

## 5. Estrategia de datos y entorno

E2E necesita **API + front levantados** y datos **deterministas**. **Análisis y plan detallado:**
[`base-de-tests.md`](./base-de-tests.md).

- **Base dedicada:** la API de E2E apunta a una base propia (estilo `AcgFotos_Tests`), nunca a `AcgFotos`
  (dev). Se siembra con un seed conocido (reutilizar el espíritu de `TestSeed.sql`: root, un
  admin-de-tenant con licencia, un no-root con licencia, un no-root **sin** licencia, tenant con
  branding).
- **Reseed:** `global-setup` resetea la base a un estado base **antes** de la corrida. Decisión abierta
  (§9): hacerlo por **endpoint de testing** expuesto solo en perfil debug, o por reset directo de DB
  (Respawn/script) como en la suite de API.
- **Config del front:** `app.config.json` de la corrida E2E debe apuntar `apiUrl` a la **API de tests**.
- **Sin qa/prd:** correr E2E = arrancar la API de tests + `ng serve`. Documentar el comando combinado.

---

## 6. Estrategia de selectores (importante)

Orden de prioridad para los POMs (Playwright recomienda rol/label sobre testid cuando aplican):

1. **`getByRole`** con nombre accesible — botones (`tbi-button` renderiza `<button>` con el label como
   texto → `getByRole('button', { name: 'Ingresar' })`), tablas, diálogos, menús.
2. **`getByLabel`** — `tbi-text-field` renderiza `mat-form-field` + `mat-label` asociada al input →
   `getByLabel('Usuario')` / `getByLabel('Contraseña')` funcionan.
3. **`data-testid`** — **HECHO (2026-06-23)** en los wrappers `tbi-*`/layout, para los elementos que
   antes dependían de clases CSS de app o de markup interno (la inversión de mayor retorno, una vez,
   beneficia a TODA la suite). Convención = `data-testid` en el wrapper (NO en el markup de Material
   interno), nombre kebab estable. Inventario actual:
   - `tbi-table`: `table-search`, `table-columns-btn`, `table-row`, `table-empty` (el paginator es
     Material puro → se usan sus clases públicas, no hay wrapper donde colgar el testid).
   - `tbi-row-actions`: `row-action-{label}` (inline) y `row-action-menu-{label}` (menú 3-puntos).
   - `tbi-status-chip`: `status-chip`. `tbi-button`: `button-spinner` (estado de carga).
   - `ErrorSnackComponent` (toast rico): `error-snack` (raíz del componente), `error-snack-toggle`
     (Ver/Ocultar detalle), `error-snack-copy`, `error-snack-ref`, `error-snack-detail`,
     `error-snack-close`. **OJO:** para contar toasts / coalescencia el POM usa la `panelClass`
     **`.tbi-snack--error`** (clase propia de la app), porque el toast **simple** de una línea (errores
     con solo `message`) NO usa `ErrorSnackComponent` y por ende no tiene el testid `error-snack`.
   - layout/shell: `toggle-menu`, `shell-logo`, `shell-title`, `theme-toggle`, `impersonate`, `logout`,
     `impersonation-banner`, `stop-impersonation`, `shell-user-name`, `shell-user-role`, `app-selector`,
     `nav-{route}` (p. ej. `nav-/usuarios`).
   - login: `login-logo`, `login-errors`. Diálogo de impersonación: `impersonate-error`.
4. `getByText`/`getByPlaceholder` — si hay texto/placeholder estable.
5. CSS por clase/id — último recurso (hoy solo el paginator de Material y el contenedor `mat-nav-list`,
   ambos estructurales y estables). **XPath: evitar.**

> Nota: los `tbi-*` aíslan Material a propósito. El `data-testid` va en el wrapper (no en el markup de
> Material interno) → mantiene ese aislamiento y da selectores estables ante upgrades de Material.

---

## 7. Convenciones para nuevos tests

- **Nombre del test = ID del caso + título.** Ej: `E2E-IMP-01 root impersona y ve el menú del destino`.
  Un test de Playwright por cada caso del catálogo ([`casos-e2e.md`](./casos-e2e.md)).
- **Sin waits arbitrarios.** Preferir `await expect(locator).toBeVisible()` / `.toHaveText()` /
  `.toHaveURL()`. Para navegación, `await page.waitForURL()`. Timeout explícito solo justificado.
- **Afirmar en la UI, no en internals.** El E2E valida lo que ve el usuario (menú, toast, fila, URL),
  no el store ni el localStorage — salvo casos puntuales de persistencia (app-context en F5).
- **Datos mutables con sufijo único** (timestamp) para no chocar entre corridas; limpieza en
  `afterEach` o dependencia del reseed.
- **Tags vía `@playwright`/grep:** `@smoke`, `@crud`, `@auth`… para filtrar corridas (`--grep @smoke`).
- **Enums/constantes** para textos de botones, labels y rutas (evitar literales repetidos en specs).

---

## 8. Roadmap por fases

| Fase                             | Estado | Entregable                                                                                                                      | Casos                                |
| -------------------------------- | ------ | ------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------ |
| **0 — Andamiaje**                | ✅     | Playwright, `playwright.config.ts`, base de tests `AcgFotos_TestE2E` + seed, login por test (`loginAs`), smoke login root → home. | —                                    |
| **1 — Auth & ruteo**             | ✅     | Login por rol, guards, logout, restauración F5, reconexión 429, allowed-routes, menú por permisos.                              | AUTH-01..10, AUTHZ-01..04            |
| **2 — Contexto & impersonación** | ✅     | Selector de app (switch/persistencia/visibilidad), impersonar/salir/refresh, theming/branding.                                  | APP-01..04, IMP-01..05, THEME-01..05 |
| **3 — CRUD canónico**            | ✅     | Usuarios ida y vuelta (alta/editar/borrar/F5), buscador, columnas, paginación, validación. (CRUD-09 N/A.)                       | CRUD-01..08                          |
| **4 — Errores & feedback**       | ✅     | Toast único (coalescencia), Ver detalle/ref, spinner anti-doble-submit, ruta inexistente.                                       | ERR-01..04                           |
| **5 — CI (futuro)**              | ⬜     | Correr la suite headless en pipeline con base efímera (Testcontainers/DB dedicada).                                             | —                                    |

**Estado (2026-06-23): Fases 0/1/2/3/4 completas — 37 casos verde.** Próximo: la mini-capacitación
(abajo) y, a futuro, la Fase 5 (CI). El reset entre corridas sigue diferido (los tests que mutan son
auto-contenidos; recién haría falta si crecen los mutantes — ver [`base-de-tests.md`](./base-de-tests.md)).

### Capacitación y autonomía (pendiente — en otro chat)

La estructura ya está completa (Fases 0-4: auth, sesión, authz, app, impersonación, theming, CRUD,
errores/feedback):

1. **Cómo correr la suite por tu cuenta.** El cómo-correr ya está en [`../../e2e/README.md`](../../e2e/README.md)
   (scripts `e2e` / `e2e:ui` / `e2e:debug` / `e2e:report`, requisitos de Node y servidores). Repasarlo
   en vivo.
2. **Mini-capacitación de testing E2E** (sesión aparte): recorrido por **cada parte** de la suite —
   `playwright.config.ts` (un único proyecto `e2e` sobre Edge, sin `storageState`), POMs (`pages/`),
   fixtures (`fixtures/`: `loginAs` = login fresco por test + `test-data.ts`), `seed/` (extras de la
   base de tests), `specs/` (los casos) — explicando **qué hace cada sección/módulo y cómo se relacionan
   entre sí** (p. ej. cómo `loginAs` aísla la sesión de cada test sin `storageState`; cómo un POM
   desacopla el spec del DOM). Objetivo: que puedas leer, correr y **escribir** casos nuevos sin
   asistencia. _(Se hace en otro chat.)_

---

## 9. Gaps y decisiones abiertas

1. **Reseed → ✅ RESUELTO (2026-06-23).** Reset directo de DB por `sqlcmd` desde el `global-setup`
   (`seed/e2e-reset.sql` + re-aplicar `e2e-extras.sql`), con degradación elegante; se descartó el
   endpoint debug-only para no sumar superficie a la API. Ver [`base-de-tests.md`](./base-de-tests.md).
2. **Discrepancia 404:** el catálogo viejo (E2E-08) esperaba una página `/404`, pero el código real hace
   `{ path: '**', redirectTo: '' }` → una ruta inexistente **va al home**. El caso E2E debe afirmar el
   comportamiento real (o se decide agregar una página 404 y se ajusta).
   2-bis. **No-root sin licencia:** confirmar el mensaje/flujo exacto del rechazo en el front (la API
   bloquea login/refresh sin licencia; ver cómo lo presenta la UI) para fijar la aserción de AUTH-03.
3. **Cómo se corre todo junto:** comando único que levante API-de-tests + `ng serve` con el
   `app.config.json` apuntando a la API de tests. Documentar en Fase 0.
4. **`data-testid` en `tbi-*` → ✅ HECHO (2026-06-23).** Testids en tabla / acciones de fila / snackbar /
   chip / botón / layout / login (inventario en §6); los POMs y specs migraron de clases CSS de app a
   `getByTestId`. La flakiness de latencia se atacó de raíz (PBKDF2 a 1000 iter en el perfil de tests,
   ver [`base-de-tests.md`](./base-de-tests.md)) → `retries` en local bajó a 1.
5. **Channel del browser:** se usa `msedge` (Edge del sistema, sin descargar Chromium) — mantener salvo
   que se prefiera Chromium bundled.
6. **Finding (Fase 1) — validación del login sin feedback visible → ✅ RESUELTO.** El submit del login
   con el form vacío **no mostraba** el mensaje "Requerido". Causa raíz (confirmada por DOM): el
   `matInput` interno de `tbi-text-field` **no tiene `ngControl` propio** (el `FormControl` vive en el
   wrapper vía CVA) y `MatInput` solo recalcula su `errorState` en `ngDoCheck` **si tiene `ngControl`**
   → el `errorStateMatcher` nunca se consultaba (código muerto), el error **jamás** se mostraba (ningún
   cambio de CD lo arreglaba). **Fix:** `tbi-text-field` ahora renderiza el error propio (`hasError()`
   lee el control externo) con change detection **Default** (para reflejar disparadores externos como
   `markAllAsTouched()`). `E2E-AUTH-05` ahora afirma que aparece "Requerido" en ambos campos + no
   dispara el POST. **Pendiente (mismo patrón):** `tbi-select` tiene el `errorStateMatcher` muerto por
   la misma razón → aplicar el mismo fix cuando se cubra un form con select.

### Nota técnica — Node 22.14.0 (Playwright sin pin)

La suite usa **la última Playwright estable** (`^1.61.0`, sin pin) sobre **Node 22.14.0** (ver `.nvmrc`).

**Por qué ese Node:** Node **22.15.0** agregó la API síncrona `module.registerHooks`; Playwright 1.61 la
usa, pero su _sync ESM loader_ hace `context.conditions?.includes("import")` asumiendo un array, mientras
Node pasa `conditions` como `Set` → `TypeError: context.conditions?.includes is not a function` y descubre
**0 tests**. Subir a Node 24/22.15+ **no** lo arregla (también traen esa API). El Node correcto más nuevo
que funciona con la última Playwright estable es **22.14.0** (último parche de la línea 22 LTS previo al
cambio; dentro del rango de Angular 20 `^22.12`). Cuando salga **Playwright 1.62 estable** (con el fix), se
puede volver a Node 22.15+/24 y actualizar `.nvmrc`.

**Setup del Node** (Windows, con nvm-windows): `nvm install 22.14.0 && nvm use 22.14.0`. Síntoma de usar
el Node equivocado: `context.conditions ... is not a function` al correr o listar los tests.

---

## 10. Referencias

- Catálogo de casos E2E (fuente única): [`casos-e2e.md`](./casos-e2e.md)
- Análisis y plan de la base de datos de tests: [`base-de-tests.md`](./base-de-tests.md)
- Comportamiento de front ya analizado (fuente de los casos), en el repo de la API:
  `Api/Docs/casos-de-prueba/sections/` → `410-f-auth`, `420-f-allowed`, `430-f-appctx`, `440-f-imp`,
  `450-f-http`, `470-f-theme`, `640-f-login`.
- Convenciones de la suite de API (inspiración de DRY/aislamiento), en el repo de la API:
  `Api/Docs/casos-de-prueba/testing-convenciones-api.md`
