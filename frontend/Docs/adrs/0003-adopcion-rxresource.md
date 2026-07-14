# ADR-0003: Adopción de `rxResource` para data-fetching reactivo (reemplaza Observables + `subscribe()` manual en lecturas)

- **Estado:** Aceptado · **Fecha:** 2026-07-12 · **Decisores:** Alberto Gentilcore

## Qué es `rxResource` (contexto — no se había usado antes en el proyecto)

`rxResource` es la API de **resources** de Angular (estable desde Angular 22, `@angular/core/rxjs-interop`) aplicada a un `Observable`. Reemplaza el patrón manual de "constructor + `Subject`/signal + `switchMap` + `.subscribe()` + signals sueltos para `loading`/`error`/el dato" por una sola declaración:

```ts
private readonly aplicacionesResource = rxResource({
  params: () => this.form.tipoId().value(), // señal reactiva (opcional)
  stream: ({ params }) => this.service.getAplicaciones(params), // Observable
});

// consumo:
this.aplicacionesResource.value();     // T | undefined — el dato (undefined mientras no resolvió)
this.aplicacionesResource.isLoading(); // boolean
this.aplicacionesResource.error();     // Error | undefined
this.aplicacionesResource.reload();    // fuerza un refetch con los params vigentes
```

Comportamiento clave:

- **Refetchea sola** cada vez que cambia el valor devuelto por `params` — y **cancela automáticamente** el pedido anterior si todavía estaba en vuelo (antes esto exigía `switchMap` a mano, y un bug real de la migración — `MeasuresSectionComponent` — mostró qué pasa cuando falta: resultados de dos fetches mezclados fuera de orden).
- Si `params` no se especifica, corre **una sola vez** al crearse (ideal para catálogos/lookups fijos).
- Si `params` devuelve `undefined`, **no fetchea en absoluto** — el idioma que usamos en todo el proyecto para "sólo pedir esto si aplica" (alta vs. edición, root vs. no-root), reemplazando los `if` imperativos que antes envolvían el `subscribe()`.

## Qué va en `rxResource` y qué NO — el límite importante

**SÍ va acá:** cualquier **lectura reactiva o disparada al construir** un componente — catálogos/lookups, la carga de la entidad en un diálogo de edición, cascadas dependientes (aplicación elegida → sus permisos), tablas paginadas server-side. La característica común: nadie "hace click" para que ocurra, es consecuencia de que el componente existe o de que un signal cambió.

**NO va acá — sigue siendo `Observable` + `.subscribe()` imperativo:** cualquier **acción disparada por el usuario** con un efecto de una sola vez — guardar (`submit`), borrar, login/logout, confirmar un diálogo (`afterClosed()`), una acción de fila, exportar/importar un archivo. Ahí el usuario decide CUÁNDO pasa, no hay "valor actual" que mantener sincronizado — `rxResource` no aporta nada y complicaría el código (¿qué sería "el `params`" de un submit?). Estos conviven en el mismo archivo sin problema: un edit-component típico tiene `rxResource` para sus lookups y un `submit()` con `.subscribe()` de toda la vida.

## Opciones

- **A — Mantener el patrón anterior** (Observables + `injectCrudClient` + `subscribe()` manual en todo, incl. lecturas). Es lo que había desde la Fase 1 del front (`F1.3` había diferido `httpResource`/`rxResource` "todavía"). Descartada: implica repetir a mano cancelación de pedidos en vuelo, signals de `loading`/`error`, y — el motivador real — un bug de diseño ya encontrado (ver Desventajas) que el patrón manual no previene.
- **B — `rxResource` para toda lectura reactiva; `subscribe()` sólo para acciones/mutaciones (elegida).**
- **C — `httpResource`** (la variante sin RxJS, sobre `fetch` nativo). Descartada: todo el `CrudClient`/interceptors de error/auth del proyecto ya están construidos sobre `HttpClient`+`Observable` — migrar a `httpResource` obligaría a reescribir esa capa entera sin ganar nada, ya que `rxResource` cubre el mismo caso de uso adaptando el `Observable` existente.

## Decisión

Se eligió **B**. Adoptado incrementalmente por subsistema, no en un solo cambio: **Fase 1** (2026-07-12) convirtió 5 puntos puntuales; **Fase 2** (misma fecha, sesión posterior) lo extendió a los 9 `edit-components` que extienden `EditComponentBase`, la propia `EditComponentBase`, `TableManageComponent`/`RelationshipsSectionComponent`, y los 2 controles compartidos app-wide `TbiTableComponent`/`TbiSearchSelectComponent`.

## Ventajas

- **Cancelación automática de pedidos en vuelo** al cambiar los `params` — elimina una clase de bug real ya encontrada (`MeasuresSectionComponent`, fase 1: dos fetches disparados a mano sin `switchMap` podían resolver fuera de orden y mezclar resultados).
- **`loading`/`error`/el dato "gratis"** (`isLoading()`/`error()`/`value()`) — antes eran 2-3 signals sueltos que había que mantener sincronizados a mano en cada `subscribe()`.
- **Menos código por lookup**: un lookup fijo pasa de ~6 líneas (`subscribe({next, error})` + signal) a 1 declaración + un `computed`.
- **Un solo idioma para "fetch condicional"** (`params` → `undefined` = no fetchea) reemplaza los `if (!isRoot()) { ... }` imperativos dispersos que envolvían cada `subscribe()`.
- **Consistencia**: mismo patrón repetido en las ~15 features del shell — cualquiera que ya vio uno reconoce todos los demás.

## Desventajas / riesgos (a tener presentes)

- **Gotcha real ya encontrado (fase 1) — booleanos "consumidos" son frágiles acá:** un flag booleano para suprimir un side-effect en el primer disparo de un `effect()` que depende de `resource.value()` se rompe con el _coalescing_ de Angular (dos cambios sincrónicos seguidos pueden colapsar en una sola ejecución del `effect`, y el flag se consume para el cambio equivocado). Regla aplicada en todo el proyecto: comparar siempre contra un **valor de referencia releído dentro del `effect`**, nunca un booleano de un solo uso.
- **Gotcha de testing**: un `effect()` que depende de `resource.value()` no siempre corre con un solo `fixture.detectChanges()` cuando el signal que dispara el resource se cambió DIRECTO desde el test (no vía binding/template) — a veces hace falta un `detectChanges()` extra después de la acción.
- **Conviven dos patrones en el mismo archivo** (resource para lecturas, `subscribe()` para acciones) — hay que tener claro el límite de la sección de arriba para no mezclar mal (p. ej. no envolver un `submit()` en un `rxResource`).
- **API relativamente nueva** (`@publicApi 22.0` recién esta versión) — superficie de cambio si Angular la itera en próximas versiones (mitigado: es la API oficial recomendada por el equipo de Angular para reemplazar `HttpClient` imperativo, no una librería de terceros).
- **Adopción hoy es PARCIAL, no total** — ver "Estado de adopción" abajo.

## Estado de adopción (al cierre de esta ADR, 2026-07-12)

**Convertido** (rama `feature/rxresource-adoption`, PR pendiente de abrir): `EditComponentBase` + los 9 edit-components (`usuario/menu/permiso/parametro/tipos-licencia/rol/grupo/tenant-edit` + `TableManageComponent`) + `RelationshipsSectionComponent` + `TbiTableComponent`/`TbiSearchSelectComponent`.

**Detectado pero NO convertido todavía** (mismo patrón, candidatos para una próxima ronda — no bloquea esta decisión, sólo registra que la migración no terminó): `auditoria-detail`, `log-detail`, `profile` (carga de entidad por id, fuera de `EditComponentBase`); `logs-list`/`menus-list`/`parametros-list` (lookup de filtro); `parametros-valor-tenant` e `impersonation-dialog` (cascadas dependientes — los casos más claros de "debería ser esto"); `tenant-edit.getAdministradores` (se escapó del propio archivo migrado); `load-dim-time-modal`/`model-real-load-modal`/`tenant-database-edit` (fetch de construcción en diálogos); `main-layout.loadMenu`/`dashboard-context.reloadAllowed` (ya reactivos vía `effect()`, con `Subscription`/`destroyRef` manual que `rxResource` reemplazaría directamente).

## Consecuencias

- **A favor:** código de lectura más corto y uniforme; una clase de bug (mezcla de resultados fuera de orden) queda estructuralmente prevenida; nuevo código que copie el patrón por imitación hereda las ventajas sin pensarlo.
- **En contra / a vigilar:** el "gotcha" de flags de supresión hay que conocerlo antes de escribir un `effect()` nuevo sobre un resource (documentado arriba y en `CLAUDE.md`); la lista de "detectado pero no convertido" puede crecer si se sigue escribiendo código nuevo con `subscribe()` imperativo por imitación de archivos viejos — vale grepear `\.subscribe\(` de tanto en tanto en componentes nuevos para confirmar que el fetch es una acción, no una lectura.
