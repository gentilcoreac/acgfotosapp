# Home (Dashboard)

El home es un **dashboard compuesto por widgets**. No hay lógica por tipo de usuario en el home
(nada de `if (isRoot) … else …`): un **registro** (`DASHBOARD_WIDGETS`) declara, por widget, **a
quién se le muestra** (`canShow(ctx)`), y el home renderiza cada widget **sólo si pasa**. Así es
estructuralmente imposible que un widget se muestre sin contemplar su audiencia, y escala a
cualquier cantidad de tipos de usuario sin tocar el home.

## Idea en una frase

> El registro decide **a quién** se le muestra cada widget (por capacidad, no por rol), y cada
> widget trae sus propios datos con **una sola llamada**.

## Piezas

| Pieza                     | Rol                                                                                                      |
| ------------------------- | -------------------------------------------------------------------------------------------------------- |
| `DASHBOARD_WIDGETS`       | Registro: lista de `{ component, canShow(ctx) }`. **Cada entrada DEBE declarar `canShow`** (audiencia).  |
| `HomeComponent`           | Contenedor. Recorre el registro y pinta cada widget con `NgComponentOutlet` **si `canShow(ctx)`**.       |
| `DashboardContextService` | Las **capacidades** del usuario: `isRoot`, `isImpersonating`, `canAccess(path)`. Se provee en el home.   |
| Widgets (`*.widget.ts`)   | Cada indicador/sección. Sólo trae y pinta sus datos. `:host { display: contents }` para caer en el grid. |

## Cómo se decide qué ve cada usuario

La visibilidad la decide el **registro** vía `canShow(ctx)`, gateando por **capacidad** (no por rol).
La capacidad de hoy es **acceso a una sección** (`ctx.canAccess('/usuarios')`), que reusa las
**allowed-routes** del usuario — lo mismo que arma el menú lateral (`GET menus/allowed-routes`).

```ts
// dashboard-widgets.ts
{ component: LicenciasKpisWidget, canShow: (ctx) => !ctx.isRoot() && ctx.canAccess('/usuarios') }

// home.html
@for (w of widgets; track w.component) {
  @if (canShow(w)) { <ng-container *ngComponentOutlet="w.component; injector: injector" /> }
}
```

- El widget **ni se instancia** si `canShow` da `false` → no puede pintar de más (el bug de las
  tarjetas en cero quedó imposible por construcción).
- **`canShow: () => true`** = mostrar a todos; el widget adapta su contenido (o se autolimita por
  datos) leyendo el contexto adentro.
- **Root** → `canAccess` devuelve `true` para todo. **Otro usuario** → según sus allowed-routes.
- **Impersonando** un cliente → el token deja de ser root y toma las capacidades del cliente (los
  widgets se recalculan solos).
- Error de red al traer allowed-routes → **falla abierto** (deja ver), igual que el guard de rutas.
  La autorización real siempre la valida la API en cada endpoint.

Las capacidades y los datos se **recargan al cambiar de contexto** (login / impersonar / parar): el
contexto y cada widget observan el `tenantId` del token.

## Reglas de datos

- **Una llamada por fuente.** Si varios indicadores salen del mismo dato, van **en el mismo widget**
  (ej. `LicenciasKpisWidget` hace **un** `getResumen` y pinta _Usadas / Por vencer / Vencidas_).
- **Se pide solo lo visible.** `ConteosKpisWidget` trae solo los conteos de las secciones a las que
  el usuario accede (no pide `/tenants` si no puede verlo → evita 401).
- **Recarga por contexto.** Cada widget vuelve a cargar cuando cambia el `tenantId` del token.

## Widgets actuales

| Widget                  | Muestra                                  | `canShow` (registro)                     | Llamada                   |
| ----------------------- | ---------------------------------------- | ---------------------------------------- | ------------------------- |
| `ConteosKpisWidget`     | Tenants / Usuarios / Grupos              | `() => true` (se autolimita por ruta)    | 1 por conteo visible      |
| `LicenciasKpisWidget`   | Licencias usadas / Por vencer / Vencidas | no root **y** `canAccess('/usuarios')`   | 1 (`getResumen`)          |
| `AccesosDirectosWidget` | Atajos a secciones (tiles sutiles)       | `() => true` (la API filtra por permiso) | 1 (`menus/dashboard`)     |
| `ActividadChartWidget`  | Gráfico de activos + altas por mes       | `canAccess('/usuarios')`                 | 1 (`getActividadMensual`) |

> `AccesosDirectosWidget` es lo más útil para el **usuario común** (su lanzadera): `menus/dashboard`
> ya devuelve solo los menús que puede ver, marcados `VisibleDash`. El label sale del `codigo`
> (`MENU_LABELS`, igual que el sidenav); el ícono sale directo de `imagenWeb`.

## Cómo agregar…

- **…un indicador nuevo:** creá el widget (sólo datos + render) y agregá una entrada
  `{ component, canShow }` a `DASHBOARD_WIDGETS`. El `canShow` es obligatorio → no se puede sumar sin
  decidir la audiencia.
- **…un tipo de usuario nuevo:** **nada.** Si sus permisos le dan acceso a una sección, hereda sus
  widgets automáticamente.

## Caso fino: misma ruta, distinto nivel (admin vs usuario común)

Si dos roles acceden a la misma ruta pero con distinto nivel, la ruta **no alcanza** como gate. El
**seam** previsto es agregar a `DashboardContextService` un `can(permisoCodigo)` apoyado en una señal
con los permisos del usuario, y usarlo en el `canShow` del registro (o, si el widget se muestra a
todos, adaptar su contenido adentro). El home no se toca.

## Tests

- `core/auth/allowed-routes.service.spec.ts` — fuente de capacidades: cache, **dedupe** de la llamada
  concurrente, falla abierto, no cachea errores.
- `dashboard/dashboard-context.service.spec.ts` — `canAccess`: root ve todo, no-root solo lo
  permitido, falla abierto, `isImpersonating`.
- `dashboard/dashboard-widgets.spec.ts` — **audiencia por widget**: todo widget declara `canShow`;
  común no ve licencias/actividad; admin sí; root no ve licencias pero sí actividad.
