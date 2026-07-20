import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  input,
  linkedSignal,
  signal,
} from '@angular/core';
import { rxResource, takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSortModule, Sort } from '@angular/material/sort';
import { MatTableModule } from '@angular/material/table';
import { Observable, Subject, catchError, debounceTime, distinctUntilChanged, of } from 'rxjs';
import { QueryParams } from '../../../core/models/query-params.model';
import { QueryResult } from '../../../core/models/query-result.model';
import { TbiRowAction, TbiRowActionsComponent } from '../tbi-row-actions/tbi-row-actions.component';
import { TbiChipTone, TbiStatusChipComponent } from '../tbi-status-chip/tbi-status-chip.component';

/** Contenido de una celda renderizada como chip de estado (ver `TbiColumn.chip`). */
export interface TbiChipCell {
  label: string;
  tone?: TbiChipTone;
  icon?: string;
}

/**
 * Preset de chip Activo/Inactivo para columnas booleanas de estado. Evita repetir el mismo lambda
 * en cada listado (tenants, aplicaciones, permisos, menús…). Uso: `chip: (row) => activoChip(row.activo)`.
 */
export function activoChip(activo: boolean): TbiChipCell {
  return activo
    ? { label: 'Activo', tone: 'success', icon: 'check_circle' }
    : { label: 'Inactivo', tone: 'neutral', icon: 'block' };
}

/** Definición de columna de `tbi-table`. */
export interface TbiColumn<T> {
  /** Id de columna y, por defecto, propiedad de la fila a mostrar. */
  key: string;
  /** Encabezado visible. */
  header: string;
  /** Si la columna habilita ordenamiento (server-side). */
  sortable?: boolean;
  /** Render custom de la celda (default: `row[key]`). */
  cell?: (row: T) => string;
  /** Render como chip de estado. Tiene prioridad sobre `cell`/texto; `null` deja la celda vacía. */
  chip?: (row: T) => TbiChipCell | null;
  /** Alineación horizontal de header y celda (default: `start`). */
  align?: 'start' | 'center' | 'end';
  /** Columna que el usuario puede mostrar/ocultar desde el menú "Columnas". */
  optional?: boolean;
  /** Estado inicial oculto (sólo aplica si `optional`). */
  hidden?: boolean;
}

/** Función que trae una página de datos según los `QueryParams`. */
export type TbiTableFetch<T> = (query: QueryParams) => Observable<QueryResult<T>>;

/** Id de la columna de acciones (presente cuando hay `rowActions`). */
const ACTIONS_COLUMN = '__actions';

/** Mismas columnas a efectos de visibilidad (permite conservar la selección del usuario cuando el
 * padre re-emite el array con identidad nueva pero igual contenido, p. ej. un `computed`). */
const sameColumns = <T>(a: TbiColumn<T>[], b: TbiColumn<T>[]): boolean =>
  a.length === b.length &&
  a.every((col, i) => col.key === b[i].key && col.optional === b[i].optional);

/**
 * Tabla del design system sobre `mat-table` + `MatPaginator` + `MatSort`, con paginación
 * y orden **server-side**. Reemplaza a `mvz-table`/`ListComponentBase` del original por
 * composición: el consumidor pasa `columns` y una función `fetch` (típicamente
 * `CrudClient.getAllByCriteria`). El padre llama a `reload()` tras crear/editar/borrar o
 * al cambiar filtros (`filters`).
 */
@Component({
  selector: 'tbi-table',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    MatTableModule,
    MatPaginatorModule,
    MatSortModule,
    MatProgressBarModule,
    MatIconModule,
    MatButtonModule,
    MatMenuModule,
    MatTooltipModule,
    MatCheckboxModule,
    TbiStatusChipComponent,
    TbiRowActionsComponent,
  ],
  templateUrl: './tbi-table.component.html',
  styleUrl: './tbi-table.component.scss',
})
export class TbiTableComponent<T> {
  readonly columns = input.required<TbiColumn<T>[]>();
  readonly fetch = input.required<TbiTableFetch<T>>();
  /** Acciones de fila declarativas: render responsive (inline-hover desktop / 3-puntos touch) + confirm/spinner. */
  readonly rowActions = input<TbiRowAction<T>[]>([]);
  readonly pageSize = input<number>(10);
  readonly pageSizeOptions = input<number[]>([5, 10, 25, 50, 100]);
  /** Filtros extra (searchText y demás) que se mergean a la query. */
  readonly filters = input<QueryParams>({});
  /** Muestra el buscador (búsqueda libre server-side vía `searchText`). */
  readonly searchable = input<boolean>(false);
  readonly searchPlaceholder = input<string>('Buscar…');

  /** Vuelve a la primera página cada vez que el padre cambia `filters` (mismo criterio que antes:
   * un cambio de filtro invalida la página actual) — declarativo vía `linkedSignal`, en vez del
   * `effect` con flag `firstFilters` (frágil, ver lección de fase 1) de antes: no hay "primer disparo"
   * que saltear, la propia página inicial (0) ya es el resultado de la primera computación. Sigue
   * siendo asignable a mano (`onPage`/`onSort`/`onSearch`/`reload`), como cualquier otro signal. */
  protected readonly pageIndex = linkedSignal({ source: this.filters, computation: () => 0 });
  protected readonly currentPageSize = linkedSignal(() => this.pageSize());
  private readonly sortBy = signal('');
  private readonly descending = signal(false);
  protected readonly searchText = signal('');

  /** Fetchea sola al crearse y cada vez que cambian página/orden/búsqueda/`filters` — reemplaza el
   * `reload$`+`search$`+el `effect` con flag `firstFilters` (frágil, ver lección de fase 1) de antes:
   * al no haber un "segundo disparo" artificial que evitar, no hace falta ningún flag. */
  private readonly dataResource = rxResource({
    params: () => ({
      ...this.filters(),
      searchText: this.searchText() || undefined,
      page: this.pageIndex(),
      pageSize: this.currentPageSize(),
      orderBy: this.sortBy(),
      descendingOrder: this.descending(),
    }),
    stream: ({ params }) =>
      this.fetch()(params).pipe(catchError(() => of<QueryResult<T> | null>(null))),
  });

  protected readonly rows = computed(() => this.dataResource.value()?.items ?? []);
  protected readonly totalCount = computed(() => this.dataResource.value()?.totalCount ?? 0);
  /** `true` mientras hay un fetch en curso (público: el padre puede reflejarlo, p. ej. en un botón). */
  readonly loading = computed(() => this.dataResource.isLoading());
  /** El último fetch falló (el toast global del interceptor ya avisó); la tabla ofrece reintentar
   * en vez de mostrar un "Sin resultados" que miente. */
  protected readonly error = computed(() => this.dataResource.value() === null);

  /** Columnas que el usuario ocultó desde el menú "Columnas" (sembrado con las `hidden`).
   * Si el padre re-emite `columns` sin cambios reales, se conserva la selección del usuario. */
  private readonly hiddenColumns = linkedSignal<TbiColumn<T>[], Set<string>>({
    source: this.columns,
    computation: (cols, previous) => {
      if (previous && sameColumns(cols, previous.source)) {
        return previous.value;
      }
      return new Set(cols.filter((c) => c.optional && c.hidden).map((c) => c.key));
    },
  });
  /** Columnas marcadas `optional` (las que ofrece el menú de visibilidad). */
  protected readonly optionalColumns = computed(() => this.columns().filter((c) => c.optional));
  protected readonly showToolbar = computed(
    () => this.searchable() || this.optionalColumns().length > 0,
  );

  private readonly search$ = new Subject<string>();

  constructor() {
    // Página fuera de rango (p. ej. se borró la última fila de la última página): retrocede a la
    // última página válida (el cambio de `pageIndex` dispara el refetch solo, vía `params`) en vez de
    // mostrar "Sin resultados" con datos reales. Autolimitado: la próxima corrida ve la MISMA
    // `totalCount` vieja hasta que el nuevo fetch resuelva, y el valor corregido no vuelve a cambiar
    // (el signal no notifica si se setea al mismo valor) — no hay loop.
    effect(() => {
      const result = this.dataResource.value();
      if (result && result.items.length === 0 && result.totalCount > 0 && this.pageIndex() > 0) {
        this.pageIndex.set(Math.ceil(result.totalCount / this.currentPageSize()) - 1);
      }
    });

    // Búsqueda con debounce: cada tecla resetea a la primera página y dispara un fetch server-side
    // (vía `params`, `searchText`/`pageIndex` son parte de la query combinada de arriba).
    this.search$
      .pipe(debounceTime(300), distinctUntilChanged(), takeUntilDestroyed())
      .subscribe((term) => {
        this.searchText.set(term);
        this.pageIndex.set(0);
      });
  }

  /** Recarga reseteando a la primera página (cambio de filtros, operaciones masivas). */
  reload(): void {
    if (this.pageIndex() === 0) {
      // Ya está en página 0: el cambio de página no dispararía un refetch solo.
      this.dataResource.reload();
    } else {
      this.pageIndex.set(0);
    }
  }

  /** Recarga conservando la página actual (post-CRUD: guardar, eliminar). */
  refresh(): void {
    this.dataResource.reload();
  }

  protected onPage(event: PageEvent): void {
    this.pageIndex.set(event.pageIndex);
    this.currentPageSize.set(event.pageSize);
  }

  protected onSort(sort: Sort): void {
    this.sortBy.set(sort.direction ? sort.active : '');
    this.descending.set(sort.direction === 'desc');
    this.pageIndex.set(0);
  }

  protected onSearch(value: string): void {
    this.search$.next(value);
  }

  /** Limpia la búsqueda libre (para los "Limpiar filtros" del padre, que no ven este input). */
  clearSearch(): void {
    this.search$.next('');
  }

  /**
   * Doble-click en una fila: ejecuta su acción primaria. Tomamos la primera acción con `handler`
   * sincrónico (típicamente Editar) que esté visible y habilitada para la fila. Las acciones API
   * (`run`, con confirmación/spinner) NO se disparan por doble-click — se invocan desde sus botones.
   */
  protected onRowActivate(row: T): void {
    const primary = this.rowActions().find(
      (action) => action.handler && !action.hidden?.(row) && !(action.disabled?.(row) ?? false),
    );
    primary?.handler?.(row);
  }

  protected isColumnVisible(key: string): boolean {
    return !this.hiddenColumns().has(key);
  }

  /** Muestra/oculta una columna opcional desde el menú "Columnas". */
  protected toggleColumn(key: string): void {
    this.hiddenColumns.update((hidden) => {
      const next = new Set(hidden);
      if (next.has(key)) {
        next.delete(key);
      } else {
        next.add(key);
      }
      return next;
    });
  }

  /** Hay columna de acciones si se pasaron acciones declarativas. */
  protected readonly hasActions = computed(() => this.rowActions().length > 0);

  protected readonly columnKeys = computed(() => {
    const hidden = this.hiddenColumns();
    const keys = this.columns()
      .filter((column) => !hidden.has(column.key))
      .map((column) => column.key);
    return this.hasActions() ? [...keys, ACTIONS_COLUMN] : keys;
  });

  /** Track de filas por `id` cuando existe (fallback: identidad del objeto). */
  protected readonly trackRow = (_index: number, row: T): unknown =>
    (row as { id?: unknown }).id ?? row;

  protected cellValue(column: TbiColumn<T>, row: T): string {
    if (column.cell) {
      return column.cell(row);
    }
    const value = (row as Record<string, unknown>)[column.key];
    return value == null ? '' : String(value);
  }
}
