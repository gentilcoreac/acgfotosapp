# Plan de Refactorización — Nuevo Código Base SaaS

> Documento vivo. Registra la estrategia y las etapas para construir un nuevo código
> base Angular reutilizable a partir de este cliente. `ClienteOriginal` queda como
> **backup congelado de referencia funcional**; el desarrollo nuevo va en un repo aparte.

## Decisiones tomadas

| Decisión                | Elección                                                                                                                                                              | Fecha                              |
| ----------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ---------------------------------- |
| Versión Angular         | **Angular 20** (standalone por defecto, control flow `@if/@for`, signals, `inject()`)                                                                                 | 2026-05-25                         |
| Manejo de estado        | **Signals + servicios** (sin librería de estado)                                                                                                                      | 2026-05-25                         |
| Almacenamiento de token | **Cookie HttpOnly + refresh token** (coordinado con la API)                                                                                                           | 2026-05-25                         |
| Localización (i18n)     | Mantener esquema **server-driven** detrás de una abstracción (`TranslationService` con signals); Transloco queda como opción de _loader_ futura sin tocar componentes | 2026-05-25                         |
| Routing                 | Migrar de `useHash:true` a **`PathLocationStrategy`** si el hosting permite SPA-fallback; si no, mantener hash documentado                                            | 2026-05-25 (a confirmar en Fase 0) |
| UI kit                  | **Angular CDK + Material**, abstraídos detrás de wrappers propios; **eliminar `@ng-bootstrap`** (no correr dos UI kits)                                               | 2026-05-25                         |
| Alcance inicial         | Migrar solo `general` + `_core` + `auth` + `_layout`. **`budget` queda fuera** de la primera etapa                                                                    | 2026-05-25                         |
| TypeScript              | **strict mode ON** (`strict`, `strictNullChecks`, `noImplicitAny`) en el proyecto nuevo                                                                               | 2026-05-25                         |

## Estrategia general

- **`ClienteOriginal`** → backup, no se toca el código de la app.
- **`Cliente` (nuevo)** → Angular 20 desde cero, standalone, signals, reactive forms.
- **`Docs/fwk-notes.md`** → patrones reutilizables + auditoría de librerías + checklist de revisión.
- **`Docs/migration-log.md`** → seguimiento feature por feature.

---

## Fase 0 — Fundaciones del nuevo proyecto _(~1 semana)_

Repo nuevo que compila, con tooling y guardarraíles, sin features.

1. `ng new` Angular 20, standalone, `bootstrapApplication` + `ApplicationConfig`.
2. Decidir routing: evaluar `PathLocationStrategy` + regla de SPA-fallback en el hosting.
3. Tooling: ESLint (angular-eslint) + Prettier, Stylelint, husky + lint-staged, commitlint (commits en español).
4. **TypeScript estricto ON.**
5. Estructura de carpetas _feature-first_ (ver abajo).
6. **CI/CD con Azure Pipelines** (ver subsección dedicada).
7. Crear `fwk-notes.md` y `migration-log.md`.

### Estructura de carpetas objetivo

```
src/app/
├── core/            # singletons: auth, http, config, error handling, guards, interceptors
│   ├── auth/        # AuthStore (signals), guards funcionales, interceptor
│   ├── config/      # AppConfigService (provideAppInitializer)
│   ├── http/        # ApiClient base, interceptors
│   └── models/      # contratos transversales
├── shared/          # componentes/pipes/directivas reutilizables (ex "Mvz")
│   └── ui/          # design system: ui-table, ui-form-field, ui-select...
├── layout/          # shell visual
├── features/        # una carpeta por feature, lazy
│   ├── roles/
│   │   ├── data/        # services + DTOs + mappers
│   │   ├── domain/      # modelos + signals store de la feature
│   │   ├── ui/          # componentes
│   │   └── roles.routes.ts
│   └── ...
└── app.routes.ts
```

Renombrar prefijo `Mvz` → prefijo propio **`tbi`** (AcgFotos) para soltar la dependencia
conceptual del fwk de Movizen. Selectores: `tbi-*` (componentes), `tbi*` (directivas).

### CI/CD (Azure Pipelines)

Repo en Azure DevOps (`AcgFotosInterno/PowerBIEmbedded/_git/Cliente`) → se usa **Azure Pipelines**
(`azure-pipelines.yml` en la raíz).

**CI — en cada PR a `main` (gate de merge, obligatorio):**

1. `npm ci` (install reproducible desde `package-lock.json`).
2. `npm run format:check` (Prettier).
3. `npm run lint` (ESLint) + `npm run lint:styles` (Stylelint).
4. `npm run build` (build prod; falla si rompe presupuestos de bundle).
5. `npm test` headless (`--watch=false --browsers=ChromeHeadless`).
6. (Más adelante) E2E Playwright en un stage aparte.

**CD — al mergear a `main` / ramas de release:**

1. Build por ambiente (`build:dev` / `build:qa` / `build:prod`) con la config runtime
   correspondiente (`src/configurations/<env>/app.config.json` — patrón heredado del original).
2. Publicar artefacto del build (`dist/`).
3. Deploy al hosting de cada ambiente (a definir junto con la decisión de routing /
   SPA-fallback). Promoción dev → qa → prod, idealmente con aprobación manual para prod.

**Branch policies en `main`:** requerir PR, build de CI en verde, mínimo 1 reviewer, y
resolución de comentarios antes de mergear. Esto reemplaza la disciplina manual del original.

> El pipeline base (`azure-pipelines.yml`) y las branch policies son **piezas reutilizables
> entre clientes** → anotar en `fwk-notes.md`. Los pasos de deploy concretos dependen del
> hosting elegido y se completan cuando se defina (misma dependencia que el routing).

---

## Fase 1 — Núcleo: seguridad, HTTP y config _(~1–2 semanas)_ — CRÍTICA

Fase de mayor alineación con la API. No avanzar a features sin esto sólido.

1. **AuthStore con signals**: `isAuthenticated = computed()`, `currentUser`, `tenant`,
   `permissions` como signals. **No mutar `environment`** como estado (anti-patrón actual).
2. **Token storage seguro (coordinado con la API .NET):**
   - API setea **refresh token en cookie `HttpOnly; Secure; SameSite`**.
   - Access token **solo en memoria** (signal), nunca en localStorage ni cookie JS-readable.
   - Eliminar el `btoa` (es codificación, NO cifrado — falsa sensación de seguridad).
   - **Requiere en la API:** `POST /auth/refresh` con rotación, endpoint de revoke,
     invalidación vía `SecurityStamp` (ya existe el campo).
3. **`authInterceptor` funcional**: inyecta `Bearer`, `cultureInfo`, headers multi-tenant;
   ante `401` → refresh silencioso → reintento único → si falla, logout. Serializar los 401
   concurrentes con un único refresh en vuelo (`shareReplay`).
4. **`errorInterceptor` funcional**: parseo centralizado (incl. caso Blob), notificaciones,
   redirección 404. Mantener el contrato de error de la API (`string[]` localizado).
5. **Guards funcionales**: `authGuard`, `anonGuard`, `permissionGuard`. Autorización fina
   **siempre revalidada por la API**.
6. **`ApiClient` base tipado** (reemplaza `ApiService<T>`): genéricos reales, query-params
   seguro, headers correctos (arregla bug de `HttpHeaders` inmutable).
7. **AppConfigService** con `provideAppInitializer`.
8. **Hardening**: CSP + headers de seguridad (coord. hosting), revisar sanitización, sin
   secretos en `environment`.

---

## Fase 2 — Capa compartida / Design System _(~1–2 semanas)_

1. Migrar componentes `Mvz*` reutilizables a standalone + signals + `OnPush` +
   `input()/output()/model()`.
2. **Template-driven → Reactive Forms**: nuevo base de edición expone `FormGroup` tipado
   (`[formGroup]`). Crear wrappers `ui-form-field` con `ControlValueAccessor` para no repetir
   boilerplate en cada feature.
3. Modernizar pipes (`LocalizePipe` puro respaldado por signal; eliminar `console-log.pipe`).
4. **Quitar jQuery**; reemplazar por APIs nativas / CDK.
5. **Auditoría de librerías** (ver `fwk-notes.md`).

---

## Fase 3 — Migración de features de `general` _(iterativa, ~3–6 semanas)_

Orden sugerido (de menor a mayor acoplamiento):

1. **Pilotos**: `tipos-licencias`, `parametros` (validan el patrón list+edit reactive).
2. **Core de seguridad**: `roles`, `permisos`, `usuarios`, `endpoints`, `groups`.
3. **Multi-tenant**: `tenants` (+ themes), `aplicaciones`, `parametros-valor-tenant`.
4. **Observabilidad/UX**: `menu`, `auditoria`, `logs`, `dashboard`, `profile`, errores,
   términos/privacidad.

Cada feature: standalone, `OnPush`, signals locales, reactive form tipado, servicio sobre
`ApiClient`, lazy `loadComponent/loadChildren`, sin código muerto. Cerrar con checklist.

---

## Fase 4 — Themes, layout por tenant, i18n y multi-tenant runtime _(~1 semana)_

> **Requisito de negocio (clave):** los temas se cargan **a demanda** (lazy, no todos al
> bootstrap) y **toda la configuración del layout es customizable por tenant** (no solo
> colores: logo, header, sidebar, footer, formato de login, qué elementos se muestran, etc.).
> Esto condiciona el diseño del shell (`layout/`) y del theming desde la Fase 0/2.

1. **Theming por tenant, carga a demanda:**
   - `ThemeService` (signals) que resuelve el tema del tenant activo y **carga su `tenant.css`
     bajo demanda** (inyección dinámica del `<link>`/stylesheet al resolver el tenant, no
     precargado). Cachear por tenant; revertir a `Default` en logout.
   - Sin colores hardcoded: todo sobre **variables CSS/tokens** (compatible con M3).
   - La API genera el `tenant.css` desde `template-base.css` y lo sirve desde el storage del
     backend (mantener ese contrato).
2. **Layout configurable por tenant:**
   - El shell (`layout/`) se arma desde un **modelo de configuración de layout por tenant**
     (signals): logo, formato de login, visibilidad/orden de header/sidebar/footer y demás
     opciones. El layout **lee config, no hardcodea**. Pensar esta config como contrato
     reutilizable entre clientes (anotar en `fwk-notes.md`).
3. Formalizar `TranslationService` server-driven detrás de abstracción.
4. Validar flujo multi-tenant completo alineado a los claims de la API.

---

## Fase 5 — Testing y cierre _(continuo)_

1. Unit tests (Vitest/Jest). **Obligatorios**: AuthStore, interceptores, guards.
2. E2E Playwright: login, refresh token, permisos, multi-tenant.
3. Presupuestos de bundle, análisis de dependencias, documentación final.

---

## Dependencias con la API (.NET)

- `POST /auth/refresh` con rotación de refresh token.
- Emisión del refresh token como cookie `HttpOnly; Secure; SameSite`.
- Endpoint de revoke / invalidación vía `SecurityStamp`.
- Headers de CSP/seguridad servidos por el host.

Encaja con el refactor en curso de la API (ver `Api/solicitud.md`).
