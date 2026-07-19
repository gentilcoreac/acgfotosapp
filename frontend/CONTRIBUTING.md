# Contributing Guide — AcgFotos Front (shell Angular 22)

## Índice

- [Documentación y Setup](#documentación-y-setup)
- [Idioma](#idioma)
- [Branching Strategy](#branching-strategy)
- [Convenciones de Código](#convenciones-de-código)
  - [Formato (enforced por .editorconfig/eslint)](#formato-enforced-por-editorconfigeslint)
  - [Naming Conventions](#naming-conventions)
  - [Comentarios](#comentarios)
  - [Patrones Obligatorios](#patrones-obligatorios)
- [Checklist: Nueva feature](#checklist-nueva-feature)
- [Pull Requests](#pull-requests)
- [Testing](#testing)
- [Troubleshooting](#troubleshooting)

---

## Documentación y Setup

- **Setup local** (Node, dev server, comandos): [`README.md`](README.md).
- **Origen**: shell heredado de `C:\PROYECTOS\CodigoBase\Cliente`, sin el vertical Budget — ver
  ADR-09 en [`../docs/04-decisiones.md`](../docs/04-decisiones.md).
- **Convenciones vigentes del repo**: [`CLAUDE.md`](CLAUDE.md) (raíz del front) — es la fuente de
  verdad más actualizada, este documento no la duplica, la complementa.
- **Plan y estado**: [`../docs/03-fases.md`](../docs/03-fases.md) (fases) y
  [`../docs/05-notas-abiertas.md`](../docs/05-notas-abiertas.md) (pendientes).
- **Comandos**: `npm start` (dev server :4200, API en :30000) · `npm test` (Vitest) · `npm run lint`
  · `npm run e2e` (Playwright).

## Idioma

- Código (clases, componentes, servicios, variables) en **inglés**.
- Comentarios y documentación en **español**.
- Mensajes de commit: **Conventional Commits, en español, minúscula** (`commitlint` lo exige —
  `feat: ...`/`fix: ...`, nunca `Feat: ...` ni sentence-case). Tipos válidos: `feat`, `fix`,
  `refactor`, `docs`, `test`, `build`, `ci`, `chore`, `perf`, `style`, `revert`.

## Branching Strategy

| Branch             | Propósito                            |
| ------------------ | ------------------------------------ |
| `master`           | Integración                          |
| `feature/<nombre>` | Desarrollo de funcionalidades        |
| `fix/<nombre>`     | Correcciones de bugs                 |

Ramas nuevas salen de `master` actualizado.

## Convenciones de Código

### Formato (enforced por `.editorconfig`/eslint)

- Indentación 2 espacios (TS/HTML/SCSS).
- ESLint fuerza selector de componente con prefijo **`tbi-`** (kebab-case) / directivas `tbi`
  (camelCase) — heredado del shell, se mantiene (`angular-eslint` + `typescript-eslint`). Prettier
  formatea, ESLint analiza (no se pisan).

### Naming Conventions

| Elemento                       | Convención                                                 | Ejemplo                                              |
| ------------------------------ | ---------------------------------------------------------- | ----------------------------------------------------- |
| Componentes                    | kebab-case archivo · PascalCase clase + sufijo `Component` | `foto-familia-preview-dialog.component.ts` → `FotoFamiliaPreviewDialogComponent` |
| Servicios                      | kebab-case archivo · PascalCase clase + sufijo `Service`   | `familia-galeria.service.ts` → `FamiliaGaleriaService` |
| Selector de componente         | `tbi-` + kebab-case                                        | `<tbi-carrito>`                                      |
| Modelos/dominio                | `*.model.ts`, tipos/interfaces PascalCase                  | `FotoFamiliaModel`                                   |
| Carpetas de feature (shell)    | `features/<feature>/{domain,data,ui}`                      | `features/usuarios/{domain,data,ui}`                 |
| Carpetas de feature (Fotos)    | `features/fotos/<subfeature>` y `features/familia/<subfeature>` (mismo patrón `ui/` interno) | `features/familia/carrito/ui` |
| Controles de formulario custom | `tbi-*`, implementan `FormValueControl` nativo             | `tbi-select`, `tbi-date-picker`                      |

### Comentarios

Regla dura, con incidente real detrás (ver más abajo): **el código se escribe para que se entienda
solo; el comentario es la excepción, no la norma.**

**Comentar SOLO lo que el código no puede decir por sí mismo:**

- Una divergencia deliberada del shell heredado o de "lo obvio" (por qué esta pantalla NO usa
  `main-layout`, por qué se eligió otro criterio para la zona de familias).
- Un constraint externo no evidente (un bug conocido de una librería, un límite del motor,
  un comportamiento de Angular 22 que no es intuitivo — ver ejemplo de `rxResource` abajo).
- Un workaround puntual, con motivo.

**NO comentar:**

- Lo que el nombre ya dice (`// carga los eventos` sobre `loadEventos()`).
- La justificación de por qué el cambio es correcto o la historia de cómo se llegó a él — eso es
  contenido del **mensaje de commit**, no del código.
- **Referencias a tareas o fases de trabajo internas/efímeras** (IDs de sesión, `[T-ALGO]`, nombres
  de una fase de un plan de trabajo puntual). Sin sentido para quien lee el archivo sin el contexto
  de esa sesión — y esa sesión no es recuperable. La trazabilidad va al commit y a
  `docs/04-decisiones.md`/`docs/05-notas-abiertas.md`, que sí son recuperables.
- Un `TODO` sin destino verificable. Si hay trabajo futuro genuino, el `TODO` apunta a algo real que
  se pueda ir a leer (`CLAUDE.md`, una entrada de `docs/05-notas-abiertas.md`), nunca a una fase
  interna que "ya se sabe cuál es" porque se habló en una conversación puntual.

_Por qué existe esta regla_: en el código base del que forkea este proyecto, durante un forward-port
un comentario de test terminó citando un identificador de tarea de una sesión de trabajo puntual —
sin significado para quien lea el código después. Mismo principio documentado del lado backend
([`../backend/CONTRIBUTING.md`](../backend/CONTRIBUTING.md)); esta sección es la versión front,
idéntica en espíritu.

### Patrones Obligatorios

Ver [`CLAUDE.md`](CLAUDE.md) para el detalle completo (Zoneless, `EditComponentBase`, Signal Forms,
layout de `features/`). Resumen de lo que más se repite y más fácil es romper por imitación de
código viejo:

#### Signal Forms para todo ABM

Extender `shared/forms/edit-component-base.ts` (`EditComponentBase<TEntity, TModel>`): `model =
signal<TModel>`, `form = form(model, schema)`, la subclase aporta `toEntity()`/`patchForm()`.
Template: `[formField]="form.campo"`. Selects dependientes: `disabled()` declarativo en el schema +
`effect`/`rxResource` para la carga — nunca `enable()`/`disable()` imperativo de FormGroup clásico
(este proyecto no usa Reactive Forms de esa forma).

#### Fetches reactivos: `rxResource` sobre `Subject`+`switchMap` manual

Para un fetch que depende de un signal (un select que dispara otro, la galería de familia
reaccionando al álbum activo), preferir `rxResource` (`@angular/core/rxjs-interop`) sobre armar un
`Subject`+`switchMap`+`effect` a mano. Firma real instalada (Angular 22): `rxResource({ params: () =>
señal, stream: ({params}) => Observable })` — **no** `request`/`{request}` (documentación de otras
versiones difiere).

**Si el fetch necesita suprimir un efecto colateral la PRIMERA vez** (ej. no podar una selección
recién cargada en el patch inicial de edición): **no usar un booleano "consumido"**
(`suppressNext = true` que se resetea a `false` en la primera corrida del `effect`). Si el signal que
dispara el fetch cambia dos veces seguidas antes de que el `effect` llegue a correr para la primera,
Angular coalescea ambas en una sola ejecución y el booleano se consume para el cambio equivocado —
bug real, no hipotético, encontrado y corregido en el código base del que forkea este proyecto. Usar
en cambio un **sentinel comparado contra el valor vigente**, releído dentro del `effect` en cada
ejecución:

```ts
private suppressForId: number | null | undefined = undefined; // undefined = sin armar
effect(() => {
  const currentId = this.form.algoId().value(); // leído fresco en cada ejecución
  const result = this.resource.value();
  if (result === undefined) return; // todavía cargando
  const suppress = this.suppressForId === currentId;
  this.suppressForId = undefined;
  if (!suppress) podarSeleccion();
});
// en patchForm: this.suppressForId = entity.algoId; luego this.model.set(...)
```

#### Controles custom (`tbi-*`)

Todos los `tbi-*` de formulario implementan `FormValueControl` nativo (Signal Forms) — ninguno
`ControlValueAccessor` (la única excepción intencional es `tbi-cell-input`). Angular 22 interopera
`[formControl]`/`[(ngModel)]` clásico contra un `FormValueControl` nativo sin adaptador — no hace
falta (ni corresponde) escribir un bridge al mezclar los dos estilos.

#### Capas shell/Fotos

`shared/`/`core/`/`layout/` = base del shell. `features/fotos/*` (admin) y `features/familia/*`
(zona pública de familias) pueden depender de `shared/`/`core/`; el shell **nunca** depende del
vertical Fotos. La zona de **familias** es mobile-first y **NO** comparte `main-layout` (el shell de
administración) — tiene su propio layout raíz, ver `CLAUDE.md`.

#### Tests parte del alcance

La suite (325 unit + e2e) queda verde en cada cambio; feature nueva = tests nuevos. En specs de
forms, setear el `model` signal directamente (no hay `FormGroup`). Para un `effect()`/`rxResource`
disparado por una acción imperativa del test (no un binding real de template), puede hacer falta
`fixture.detectChanges()` explícito después de la acción — un `signal.set()` llamado directo desde
el test no siempre alcanza a flushear el `effect` interno del resource solo.

## Checklist: Nueva feature

1. Carpeta `features/<feature>/{domain,data,ui}` (lazy) para features de administración; para la
   zona de familias, `features/familia/<subfeature>/ui` siguiendo el mismo patrón interno. Referencia
   admin: `features/usuarios`; referencia familia: `features/familia/carrito`.
2. `domain/`: modelos/tipos. `data/`: servicio(s) sobre `injectCrudClient` o `rxResource` según el
   caso (ver arriba). `ui/`: componentes standalone, `OnPush`.
3. Edit-components extienden `EditComponentBase`; list-components siguen el patrón de
   `tbi-table`/filtros existente (ver una feature ya portada como plantilla).
4. Registrar la ruta en el router correspondiente (lazy `loadComponent`/`loadChildren`) —
   `app.routes.ts` separa las rutas `/admin` (main-layout) de las rutas de familia.
5. Si la pantalla necesita permiso propio en `api/fotos/*`: confirmar que el endpoint ya esté
   abierto para el rol que la va a usar (los endpoints del vertical Fotos arrancan root-only hasta
   que el front los consume real, ver backend `CONTRIBUTING.md`).
6. Tests: unit (Vitest) de la feature nueva + actualizar e2e si toca un flujo real de usuario.

## Pull Requests

- Título descriptivo, en español.
- El PR debe compilar (`ng build`) y lintear sin errores nuevos, y no bajar la suite de tests.

## Testing

- **Unit**: Vitest vía Angular CLI (`ng test` / `npm test`). Filtrar un spec:
  `ng test --include='**/nombre.component.spec.ts'`.
- **E2E**: Playwright (`npm run e2e`; requiere front + API levantados, ver `e2e/README.md`).
- Antes de dar por verde un fix que toca reactividad async (`effect`/`rxResource`), correr la suite
  afectada **más de una vez** — un bug de coalescing de effects puede pasar en una corrida y fallar
  en la siguiente (o viceversa) según el orden de scheduling; una sola corrida verde no alcanza para
  confiar en ese tipo de cambio.

## Troubleshooting

| Problema                                                                         | Solución                                                                                                                                                   |
| ---------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Un test que cambia un signal y assertea el resultado falla intermitentemente     | Agregar `fixture.detectChanges()` después de la acción, y si depende de un `rxResource`, considerar `await fixture.whenStable()` + otro `detectChanges()`. |
| Un `effect()` con lógica "solo la primera vez" se salta la poda de una selección | Sospechar el patrón de booleano "consumido" — reemplazar por un sentinel comparado contra el valor vigente (ver arriba).                                   |
| `[formControl]` sobre un `tbi-*` "no debería andar"                              | Sí anda — Angular 22 interopera FluentForms clásico contra `FormValueControl` nativo sin adaptador. No es un bug.                                          |
