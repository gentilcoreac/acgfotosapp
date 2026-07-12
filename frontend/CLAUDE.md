# AcgFotos — Front web (shell Angular 22)

Shell heredado del código base (`C:\PROYECTOS\CodigoBase\Cliente`, rama `feature/rxresource-adoption`)
sin el vertical Budget. Sobre esta plataforma (auth, usuarios, roles, menús, tenants) se construye el
vertical **Fotos** (galería de familias, carrito, pedidos — ver `../docs/03-fases.md`).

## Comandos

- Node ≥22.22.3 (`.nvmrc`). Si el Node global es menor, hay uno portátil en
  `..\.tools\node-v22.22.3-win-x64` (prepender al PATH).
- `npm start` → dev server en :4200. La API es `AcgFotos.Api` en :30000
  (`../backend`; creds dev `root` / `Root@AcgFotos2026!`).
- `npm test` → unit (Vitest; filtrar: `npm test -- --filter="NombreSpec"`).
- `npm run lint` / `npm run e2e` (Playwright; requiere front + API E2E levantados, ver `e2e/README.md`).

## Arquitectura

- `core/` (auth, http, config, tenant-style) · `shared/` (forms base, ui `tbi-*`, feedback) ·
  `layout/` · `features/<feature>/{domain,data,ui}` (lazy). Plantilla de referencia para una
  feature nueva: `features/usuarios`.
- Data-fetching: **Observables + `injectCrudClient`** (`core/http/crud-client.ts`) para el patrón
  CRUD base. `rxResource` (`@angular/core/rxjs-interop`, firma real: `params`/`stream: ({params})`,
  NO `request`) ya se adoptó para fetches reactivos por request-signal — al escribir un fetch nuevo
  que dependa de un signal, preferirlo sobre `Subject`+`switchMap`+`effect` manual.

## Convenciones (heredadas del shell — no regresionar)

- **Zoneless**: `provideZonelessChangeDetection()`, sin `zone.js`. Componentes **OnPush** siempre.
- **Signal Forms** para todo ABM: extender `shared/forms/edit-component-base.ts`
  (`EditComponentBase<TEntity, TModel>`): `model` = `signal<TModel>`, `form = form(model, schema)`,
  subclase aporta `toEntity()`/`patchForm()`. Template: `[formField]="form.campo"`,
  `<form novalidate (submit)="$event.preventDefault(); submit()">`.
- **Controles custom**: los `tbi-*` de formulario implementan el contrato nativo `FormValueControl`
  (la única excepción intencional es `tbi-cell-input`, CVA a propósito).
- Standalone components + control flow `@if/@for` + signals para estado. Naming `tbi-*` (prefijo
  heredado del shell; se mantiene).
- **Tests parte del alcance**: la suite (unit + e2e) queda verde en cada cambio; feature nueva =
  tests nuevos. En specs de forms, setear el `model` signal (no hay `FormGroup`).
- i18n diferido: textos en español hardcodeados por ahora.

## Reglas propias de AcgFotos

- La zona de **familias** (galería/carrito, a construir) es mobile-first y NO comparte layout con
  `/admin` (`main-layout` es el shell de administración).
- Premisa de imágenes: el front solo consume URLs firmadas de derivados con watermark; nunca
  renderiza originales (ver `../docs/04-decisiones.md` ADR-01/ADR-06).
