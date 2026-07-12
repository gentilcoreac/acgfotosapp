# Notas del Framework — Patrones reutilizables para todos los clientes

> Documento vivo. Cada decisión/patrón que adoptemos en el nuevo código base y que deba
> replicarse en los demás clientes que usan este "fwk" se anota acá, con su porqué.

## Índice

- [Flujo de trabajo por feature (obligatorio)](#flujo-de-trabajo-por-feature-obligatorio)
- [Frontera de reuso (framework vs app)](#frontera-de-reuso-framework-vs-app)
- [Conceptos clave decididos](#conceptos-clave-decididos)
- [Auditoría de librerías](#auditoría-de-librerías)
- [Checklist de revisión de código](#checklist-de-revisión-de-código)
- [Hallazgos de seguridad (cliente)](#hallazgos-de-seguridad-cliente)
- [Glosario de migración (qué reemplaza a qué)](#glosario-de-migración-qué-reemplaza-a-qué)

---

## Flujo de trabajo por feature (obligatorio)

**Antes de migrar/escribir cualquier feature** se hacen estos pasos y se dejan anotados en la
propia feature (cabecera del `.model.ts` / `.service.ts` o un comment breve):

1. **Analizar los endpoints de la API** que consume la feature: verbos disponibles
   (`GET` lista paginada, `GET {id}`, `POST` update/create, `PUT`, `DELETE`), la ruta real
   (`api/<modulo>/<entidad>`) y la forma exacta del DTO (request y response).
2. **Chequear contra el front original (`ClienteOriginal`)**: qué propiedades, columnas,
   validaciones y comportamientos tenía el ABM/listado original. **El ABM nuevo puede
   necesitar más propiedades** que las que asumimos al principio — alinearlas con lo que
   espera/devuelve la API y con lo que mostraba el front viejo.
3. **Si hay diferencias** entre DTO de la API ↔ front original ↔ modelo nuevo, **hacerlas
   notar explícitamente** antes de avanzar (no resolver en silencio).

Razón: evitar ABMs incompletos (faltan campos que la API exige o que el usuario ya usaba) y
descubrir temprano los desajustes de contrato. Aplica a **cada** feature de `general`.

---

## Frontera de reuso (framework vs app)

El objetivo es un **código base reutilizable entre clientes**. La estructura del front
refleja la misma frontera de reuso que la API (Core = fwk / Base = módulo / app), pero con
los nombres de convención de Angular:

| Frontend (`src/app/`)          | Rol                                                            | Análogo en la API              |
| ------------------------------ | -------------------------------------------------------------- | ------------------------------ |
| `core/`                        | servicios singleton (auth, http, config, guards, interceptors) | `AcgFotos.Core` (fwk)            |
| `shared/` (incl. `shared/ui/`) | UI reutilizable sin estado (design system, pipes, directivas)  | `AcgFotos.Core` (fwk)            |
| `layout/`                      | shell visual                                                   | (fwk)                          |
| `features/`                    | features del negocio (general, budget, …)                      | `AcgFotos.Base` + módulos por BC |

- **`core` + `shared` + `layout` = framework reutilizable** (lo que compartirían todos los clientes).
- **`features` = app / específico del proyecto** (lo que cambia por cliente).
- **`core` vs `shared`**: ambos son "núcleo", pero `core` = servicios/DI/singletons (sin UI) y
  `shared` = UI presentacional importable (muchas instancias). Se mantienen separados (convención
  Angular estándar); **no** renombrar `shared`.

**Regla de oro (para poder extraer el framework después):** `features/` **solo consume** del
framework, **nunca exporta hacia** `core`/`shared`/`layout`. Si se respeta, cuando aparezca el
2º cliente la extracción de `core`+`shared`+`layout` a una **librería del workspace Angular**
(`projects/` + `ng-packagr`, o Nx) es mecánica — ese es el equivalente exacto del split
Core/Base de la API (lib compartida + apps por cliente).

---

## Conceptos clave decididos

### Routing: `useHash` vs `PathLocationStrategy`

- `useHash:true` → URLs `app.com/#/ruta`. El fragment (`#...`) **no se envía al server**;
  siempre sirve `index.html`. Pro: cero config de hosting. Contras: URLs feas, malo para
  SEO/SSR, choca con redirects OAuth que usan fragment.
- `PathLocationStrategy` (default) → URLs limpias `app.com/ruta`. Requiere **SPA-fallback**
  (reescribir rutas desconocidas → `index.html`; 1 regla en IIS/nginx/Azure SWA).
- **Decisión:** preferir `PathLocationStrategy` salvo hosting sin control de rewrites.

### Refresh token

- **Access token** corto (5–15 min) como `Bearer`; **refresh token** largo en cookie
  `HttpOnly`, usado solo contra `/auth/refresh`.
- Mejora: ventana de ataque chica, UX sin deslogueos, **revocación real** (server-side vía
  `SecurityStamp`), detección de robo por rotación.
- Impacto: API necesita endpoint de refresh+revoke; cliente debe serializar 401 concurrentes
  con un único refresh en vuelo; CORS con `withCredentials`.

### UI kit

- **Angular CDK + Material**, siempre detrás de wrappers propios (permite swap futuro).
- **No** correr dos UI kits → eliminar `@ng-bootstrap`.
- Theming sobre **CSS variables/tokens** (compatible con themes por tenant y con M3).

### Theming y layout por tenant (requisito de negocio) — IMPLEMENTADO (ciclo "Themes y estilo", 2026-06-04)

> El enfoque **cambió** respecto del original. Detalle y trade-offs en el **ADR-0001**
> (`Docs/adrs/0001-runtime-theming-m3-css-variables.md`). Lookups por tenant: ver sección de abajo.

- **Motor = Angular Material M3 + CSS custom properties** (no el `tenant.css` lazy del original).
  `mat.theme()` emite las variables de sistema `--mat-sys-*` como `light-dark(claro, oscuro)`. El tema
  por tenant se aplica **sobreescribiendo esos tokens en runtime** (`document.documentElement.style`),
  generando la **paleta tonal completa** desde los colores de marca del tenant con
  `@material/material-color-utilities`. **NO** se inyecta un CSS generado por backend: el pipeline
  `regenerar-themes` + `template-base.css` queda **solo para el `ClienteOriginal`**.
- **Dark mode** = `ThemeStore` (signals) que setea `color-scheme` en `<html>` (toggle en el toolbar,
  persistido en localStorage; semilla inicial por `darkModeByDefault` del tenant). No re-emite el tema.
- **De dónde sale el estilo:** `tenants/public-style` (anónimo, en el bootstrap, por dominio/`?tenant=`)
  para el login; y `tenants/header-style/{id}` al loguear/impersonar → el tema **sigue al tenant del
  usuario activo** (`TenantThemeCoordinator` reacciona al claim `tenant` del token). Logout revierte al
  branding por dominio / base.
- **Qué se aplica hoy:** colores (paleta), favicon, título, logo de header, y branding del login (logo,
  fondo, variantes `tipoLayoutLogin` 0/1/2). `styleSheetCssUrl` (CSS custom del tenant) se inyecta como
  `<link>` **solo si es del mismo origen que la API** (mitiga inyección de CSS de terceros).
- **El shell lee del store, no hardcodea** (`TenantStyleStore` en `core/`); colores/opciones vía
  tokens/CSS vars, nunca hardcodeados. Piezas reutilizables entre clientes → mantener estables.
- **Pendiente (futuro):** config de layout fina por tenant (visibilidad/orden de header/sidebar/footer)
  — hoy solo se customiza el login y los logos; el resto del shell es fijo.

### Lookups multi-tenant en ABMs (aplicaciones por tenant vs catálogo global)

Al poblar selects/checkboxes de un ABM hay que distinguir **lookup por tenant** de **lookup
global**, porque comparten entidad pero responden distinto:

- **Aplicaciones asignables a un usuario** → solo las que **su tenant tiene contratadas**
  (`gen_TenantAplicaciones`), vía `GET api/general/aplicaciones/aplicaciones-tenant`. **No** el
  catálogo global. Es el comportamiento multi-tenant correcto; el front original también usaba
  `getAplicacionesPorTenant()`. Si ese endpoint viene vacío, es porque el tenant no tiene
  aplicaciones vinculadas — **dato faltante, no bug del ABM**.
- **Aplicaciones en el ABM de Parámetros** (define el **default** del parámetro, system-level) → el
  **catálogo global** (`GET api/general/aplicaciones`, listado estándar), porque un parámetro es a
  nivel **sistema/aplicación**, no por tenant.
- **Aplicaciones en el ABM de Parámetros por tenant** (`parametros-valor-tenant`, root, override del
  valor) → las aplicaciones **del tenant elegido** (`GET api/general/aplicaciones/aplicaciones-tenant-id/{tenantId}`,
  root-only). Mismo recurso que el caso anterior pero **distinto endpoint**: acá root **elige un
  tenant cualquiera** y sólo customiza valores de las apps que ese tenant tiene contratadas → lookup
  por tenant, pero por el **tenant seleccionado en pantalla** (no el del usuario logueado, como en el
  lookup de aplicaciones por usuario). Tres usos del mismo recurso, tres endpoints según el caso.

Regla: antes de elegir el endpoint de un lookup, preguntarse _"¿esto se acota a un tenant (¿cuál:
el del usuario o uno elegido?) o es global?"_. Mismo recurso (`aplicaciones`), varios endpoints,
según el caso de negocio. Documentarlo en el `.service.ts` de la feature (qué endpoint y por qué).

### CI/CD (Azure Pipelines)

- **CI por PR (gate de merge):** `npm ci` → `format:check` → `lint` + `lint:styles` →
  `build` (prod) → `test` headless. Más adelante stage E2E (Playwright).
- **CD al mergear:** build por ambiente (`dev`/`qa`/`prod`) con su `app.config.json`,
  publicar artefacto (`dist/`), deploy con promoción dev → qa → prod (aprobación manual a prod).
- **Branch policies en `main`:** PR obligatorio, CI en verde, ≥1 reviewer, comentarios resueltos.
- El `azure-pipelines.yml` + las branch policies son **plantilla reutilizable entre clientes**.
  Los pasos de deploy dependen del hosting (misma dependencia que routing/SPA-fallback).

---

## Auditoría de librerías

Estado: `KEEP` (mantener) · `REPLACE` · `REMOVE` · `EVALUATE`.

| Librería                             | Uso actual           | Veredicto    | Razón / acción                                                      |
| ------------------------------------ | -------------------- | ------------ | ------------------------------------------------------------------- |
| `@angular/cdk`                       | overlay, etc.        | **KEEP**     | Base indiscutida, first-party.                                      |
| `@angular/material`                  | UI principal         | **KEEP**     | First-party, compatible siempre; abstraer en wrappers.              |
| `@ng-bootstrap/ng-bootstrap`         | algunos componentes  | **REMOVE**   | No correr dos UI kits; migrar a Material/CDK.                       |
| `moment` + `material-moment-adapter` | fechas               | **REPLACE**  | moment en mantenimiento. Unificar en **Luxon** + su date-adapter.   |
| `luxon`                              | fechas extra         | **KEEP**     | Será la única lib de fecha.                                         |
| `jquery` + `@types/jquery`           | DOM imperativo       | **REMOVE**   | Anti-patrón en Angular moderno; usar APIs nativas/CDK.              |
| `ngx-spinner`                        | spinner              | **EVALUATE** | Reemplazable por overlay CDK + signal.                              |
| `ngx-scrollbar`                      | scrollbar            | **EVALUATE** | Hay dos libs de scrollbar; dejar una.                               |
| `simplebar-angular`                  | scrollbar (shell)    | **EVALUATE** | Idem; elegir una sola.                                              |
| `ngx-permissions` (v19)              | permisos en template | **EVALUATE** | Reemplazable por directiva propia `*hasPermission` sobre AuthStore. |
| `ngx-cookie-service`                 | cookies token        | **REMOVE**   | Innecesario con cookie HttpOnly (la maneja el server).              |
| `@swimlane/ngx-graph` + `d3`         | grafos               | **EVALUATE** | Verificar si lo usa `general`; parece de `budget`. Quitar si no.    |
| `lodash-es` (solo `get`)             | validación NgForm    | **REMOVE**   | Reemplazable por optional chaining nativo.                          |
| `patch-package`                      | parches              | **EVALUATE** | Revisar qué parchea; idealmente eliminar.                           |
| `ngx-material-timepicker`            | hora                 | **EVALUATE** | Ver si Material timepicker (v19+) lo cubre.                         |
| `@angular-slider/ngx-slider`         | sliders              | **EVALUATE** | Ver si Material slider alcanza.                                     |
| `flag-icons`                         | banderas i18n        | **KEEP**     | Aceptable.                                                          |
| `cronstrue`                          | traducir cron        | **KEEP**     | Específico y liviano.                                               |
| `file-saver-es`                      | descargas            | **EVALUATE** | Ver si se reemplaza por API nativa de descarga.                     |
| `@ngx-loading-bar/core`              | barra de navegación  | **EVALUATE** | Ver si se mantiene.                                                 |
| `@angularclass/hmr`                  | HMR                  | **REMOVE**   | Angular CLI ya trae HMR moderno.                                    |

---

## Checklist de revisión de código

Aplicar a cada componente/servicio migrado (pedido: minuciosidad línea por línea).

- [ ] Standalone (sin NgModule).
- [ ] `ChangeDetectionStrategy.OnPush`.
- [ ] Estado local con **signals** (`signal/computed/effect`), no propiedades sueltas.
- [ ] DI con `inject()`, no constructor con parámetros públicos.
- [ ] `input()/output()/model()` en lugar de `@Input/@Output` decoradores.
- [ ] Control flow nuevo `@if/@for/@switch` (no `*ngIf/*ngFor`).
- [ ] Formularios **reactivos tipados** (no template-driven / `[(entity)]`).
- [ ] Sin suscripciones manuales colgadas: `takeUntilDestroyed()` o `async` pipe / `toSignal`.
- [ ] Sin `any` explícito; tipos en todos los bordes.
- [ ] Sin código muerto / comentado (el original arrastra bloques comentados grandes).
- [ ] Strings visibles vía pipe de localización; ninguno hardcoded.
- [ ] Sin acceso directo a `environment` como estado mutable.
- [ ] Sin `HttpClient` directo en componente; usar servicio sobre `ApiClient`.
- [ ] Sin `NO_ERRORS_SCHEMA` salvo justificación documentada.
- [ ] Manejo de error y loading explícito (signals de estado).

---

## Hallazgos de seguridad (cliente)

Detectados en `ClienteOriginal`, a resolver en el nuevo base (Fase 1):

1. **Token en cookie no-HttpOnly, ofuscado con `btoa` (no cifrado), sin `Secure`/`SameSite`.**
   `btoa` no protege nada; un XSS roba el token. → cookie HttpOnly + access en memoria.
2. **Sin refresh token / renovación silenciosa.** → implementar patrón refresh.
3. **Expiración calculada en cliente** y frágil (se escribe fecha actual y luego se corrige).
   → fuente de verdad: `exp` del JWT / respuesta del server.
4. **Estado global mutable** (`environment.tenant/tenantId` mutados en runtime). → AuthStore.
5. **Permisos/perfil en localStorage + autorización client-side.** Aceptable solo porque la
   API revalida cada endpoint (`[Authorize]`, rate limiting, `SecurityStamp`).
6. **`NO_ERRORS_SCHEMA`** desactiva validación de templates en módulos clave.
7. **Bug:** `ApiService.getHTTPHeaders()` ignora el retorno de `HttpHeaders.set()`
   (inmutable) → el header nunca se aplica.

---

## Glosario de migración (qué reemplaza a qué)

| Original (`ClienteOriginal`)                             | Nuevo base                                      |
| -------------------------------------------------------- | ----------------------------------------------- |
| NgModules                                                | Standalone + `bootstrapApplication`             |
| Template-driven + `[(entity)]`                           | Reactive Forms tipados `[formGroup]`            |
| `ApiService<T>`                                          | `ApiClient` tipado                              |
| `ListComponentBase` / `EditComponentBase` (`@Directive`) | Bases por composición + signals                 |
| Clases interceptor                                       | Interceptores funcionales (`HttpInterceptorFn`) |
| Guards clase / `CanMatchFn` ad-hoc                       | Guards funcionales sobre AuthStore              |
| Storage services dispersos + cookies                     | AuthStore (signals) + cookie HttpOnly           |
| `*ngIf/*ngFor`                                           | `@if/@for`                                      |
| `@Input/@Output`                                         | `input()/output()/model()`                      |
| prefijo `Mvz`                                            | prefijo propio (`tb-`/`app-`)                   |
