# ADR-0001: Theming por tenant en runtime con variables de sistema M3

- **Estado:** Aceptado · **Fecha:** 2026-06-04 · **Decisores:** Alberto Gentilcore

> Primer ADR del front (`Cliente`). Los ADR de la API viven en `Api/Docs/adrs/`.

## Contexto

Cada tenant define su identidad visual en el ABM (pestaña Estilos): colores de marca
(`colorPrimarioLight` / `colorPrimarioDark`), logos, favicon, fondos de login, etc. El front debe
aplicar ese estilo **en runtime**, antes del login, resolviendo el tenant por `?tenant=`, hostname o
un default de config. El endpoint `GET api/general/tenants/public-style/{valueToFilter}`
(`[AllowAnonymous]`) ya expone esos datos (`GetTenantPublicStyleOutput`).

**Cómo lo hace el cliente original (referencia):** el backend genera, por tenant, un archivo
`{codigo}.css` (endpoint `regenerar-themes`) sustituyendo ~6-10 valores `hsl()` hardcodeados en un
`template-base.css`; el front lo descarga con **XMLHttpRequest síncrono** y lo inyecta como `<link>`
en el `<head>`. Es decir: pipeline de generación de CSS en el servidor + carga bloqueante + solo
recolorea el primario (no genera una paleta coherente).

**Qué tenemos en el front nuevo:** Angular 20 + Angular Material **M3**. `mat.theme()` emite las
variables de sistema `--mat-sys-*` (47 tokens de color) como `light-dark(claro, oscuro)`, y todos los
componentes Material caen por fallback a esas variables (verificado en Fase 1). El modo light/dark se
controla seteando `color-scheme` en `<html>` (`ThemeStore`).

## Opciones

- **A — Portar el enfoque del original:** consumir el `{codigo}.css` generado por backend (o
  `styleSheetCssUrl`) e inyectarlo como `<link>`. **Descartada:** arrastra el pipeline server-side de
  CSS, depende de `regenerar-themes`, el XHR síncrono está deprecado y bloquea, y solo recolorea el
  primario. Deuda innecesaria teniendo M3.
- **B — Override de solo el color primario** (`--mat-sys-primary` y derivados) en runtime.
  **Descartada:** simple pero incoherente — componentes que usan `secondary`/`tertiary`/`surface`
  (p. ej. el ítem activo del sidenav usa `secondary-container`) quedarían en la paleta base mientras
  los botones toman el color del tenant.
- **C — Paleta tonal completa M3 en runtime (elegida):** generar, desde los dos colores de marca del
  tenant, un esquema M3 **tonal spot** para light y otro para dark con
  `@material/material-color-utilities` (la misma librería que usa el schematic
  `ng generate @angular/material:theme-color`), y sobreescribir **todos** los `--mat-sys-*` de color
  en `document.documentElement.style` como `light-dark(L, D)`.

## Decisión

**Opción C.** En el bootstrap (`provideTenantStyleInit`), un APP_INITIALIZER resuelve el tenant, trae
su `public-style` (anónimo, tolerante a fallo) y aplica:

- **Colores:** `buildTenantSysVars(lightHex, darkHex)` (`core/tenant-style/theme-palette.ts`) genera
  los dos esquemas M3 y devuelve el map `--mat-sys-<token>: light-dark(L, D)`; se setean inline en
  `<html>` (gana sobre el stylesheet base de `mat.theme()`). El toggle del `ThemeStore` (`color-scheme`)
  resuelve cuál se ve, sin recalcular nada.
- **Favicon y título** directo al `<head>`/`document.title`.
- **`darkModeByDefault`** se pasa a `ThemeStore.initialize()` como semilla (la preferencia guardada en
  localStorage del usuario sigue teniendo prioridad).
- **Logos** (header) los expone `TenantStyleStore` reactivo al modo. El **branding del login** (logo,
  fondo, variantes `TipoLayoutLogin`) y el escape hatch **`styleSheetCssUrl`** se implementaron en
  **Fase 2b**: `styleSheetCssUrl` se inyecta como `<link>` **solo si es del mismo origen que la API**
  (URL externa se ignora → mitiga inyección de CSS arbitrario de terceros).
- **El tema sigue al tenant efectivo del usuario** (no solo al dominio/URL): `TenantThemeCoordinator`
  reacciona al claim `tenant` del token (login / impersonación / parar / restore) y aplica el estilo de
  ese tenant vía `header-style/{id}` (autenticado); al desloguear vuelve al branding por dominio del
  bootstrap. Así "ingreso a un tenant → veo sus colores" funciona también en dev (localhost) y con
  dominios compartidos. No re-aplica el modo light/dark (respeta la preferencia del usuario).

### `regenerar-themes` y propagación del branding

El endpoint `regenerar-themes` del backend (que genera el `{codigo}.css` estático) **no se usa** desde
este front, y por eso se **quitó del ABM** la acción "Regenerar temas" (botón + `regenerarTodosLosThemes`/
`regenerarThemePorTenantId` en `TenantService`). Motivo: con el enfoque M3, **el cliente NO consume CSS
pre-generado** — lee los colores crudos del tenant y arma la paleta en runtime. Por lo tanto, **un cambio
en el ABM (color/logo/favicon/etc.) impacta automáticamente** la próxima vez que el front trae el estilo
del tenant: en el **bootstrap** (`public-style`, login/anónimo) y al **loguear/impersonar** (`header-style`).
No hay paso de regeneración; no es "en vivo" sobre una sesión ya abierta (requiere recargar / re-login).

Nota de coexistencia: los endpoints `regenerar-themes` **siguen vivos en el backend** porque el
`ClienteOriginal` sí consume ese CSS (su `EditByAdminClient` lo regenera solo al guardar). Se retiran del
backend cuando el original se dé de baja.

## Consecuencias

**A favor:**

- Tema **coherente**: toda la paleta (primary/secondary/tertiary/surfaces/outline/error) deriva de la
  marca del tenant, no solo el primario.
- **Sin pipeline de CSS en el servidor** ni `regenerar-themes`, **sin XHR síncrono**, sin inyección de
  `<link>`. El cambio es setear CSS custom properties (instantáneo, no bloquea el render).
- Reusa el mismo modelo de tokens que M3 + el dark mode ya existente (`light-dark()` + `color-scheme`).

**En contra / costos:**

- Nueva dependencia `@material/material-color-utilities` (Google, sin deps transitivas, ~la que usa el
  schematic de Material).
- El `--mat-sys-primary` resultante es un **tono armonizado** del color de marca (M3 elige el tono por
  contraste), no el hex literal. Es el comportamiento M3 correcto (accesibilidad), pero puede diferir
  levemente del color exacto cargado en el ABM.
- Las URLs de imágenes vienen absolutas del backend → si el host de la API cambia, cambian las URLs
  (no se prepend-ea `apiUrl`).

## Consideraciones futuras (no urgente)

- **`styleSheetCssUrl` es en gran medida redundante.** Como la paleta entera se deriva de los 2 colores
  de marca, el CSS custom rara vez hace falta; además es el campo más sensible (de ahí la guarda
  same-origin). Candidato a **deprecar** si no se usa en la práctica.
- **Colapsar los 2 colores a 1 semilla.** Hoy el ABM tiene `colorPrimarioLight` + `colorPrimarioDark` y
  se genera un esquema M3 desde cada uno. M3 puede derivar **ambos** modos (claro y oscuro) desde una
  **sola** semilla, así que el ABM podría simplificarse a un único color de marca. Trade-off: se pierde
  poder elegir un color **distinto** para dark (algunas marcas lo quieren). Por eso se mantienen los 2 —
  es una feature, no deuda.
- **Observaciones de backend** (no de este front, baja sensibilidad — solo branding): `public-style`
  (`[AllowAnonymous]`) permite enumerar códigos de tenant; `header-style/{id}` (`[Authorize]`) no está
  scopeado por tenant (un usuario autenticado podría leer el branding de otro tenant). Revisar al
  endurecer la API.
