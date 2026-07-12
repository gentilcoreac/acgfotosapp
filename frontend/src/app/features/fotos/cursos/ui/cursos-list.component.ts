import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  signal,
  viewChild,
} from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { Observable, EMPTY, catchError, map, of, tap } from 'rxjs';
import { QueryParams } from '../../../../core/models/query-params.model';
import { NotificationService } from '../../../../shared/feedback/notification.service';
import { TbiRowAction } from '../../../../shared/ui/tbi-row-actions/tbi-row-actions.component';
import {
  TbiSelectComponent,
  TbiSelectOption,
} from '../../../../shared/ui/tbi-select/tbi-select.component';
import { TbiColumn, TbiTableComponent } from '../../../../shared/ui/tbi-table/tbi-table.component';
import { EventosService } from '../../eventos/data/eventos.service';
import { Evento } from '../../eventos/domain/evento.model';
import { CursosService } from '../data/cursos.service';
import { Curso } from '../domain/curso.model';
import { CursoEditComponent } from './curso-edit.component';

/** Valor del filtro que significa "todos los eventos" (CursoCriteria.EventoId = 0). */
const TODOS_LOS_EVENTOS = 0;

// TODO (Fase 4 - i18n): textos en español por ahora.
@Component({
  selector: 'tbi-cursos-list',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatButtonModule, MatIconModule, TbiSelectComponent, TbiTableComponent],
  templateUrl: './cursos-list.component.html',
  styleUrl: './cursos-list.component.scss',
})
export class CursosListComponent {
  private readonly service = inject(CursosService);
  private readonly eventosService = inject(EventosService);
  private readonly dialog = inject(MatDialog);
  private readonly notify = inject(NotificationService);

  protected readonly table = viewChild.required(TbiTableComponent);

  /** Eventos del tenant: alimentan el filtro y resuelven la columna Evento (una sola consulta). */
  private readonly eventosResource = rxResource({
    stream: () =>
      this.eventosService.crud.getAll().pipe(
        map((result) => result.items),
        // el toast de error lo emite el errorInterceptor (global)
        catchError(() => of<Evento[]>([])),
      ),
  });
  private readonly eventos = computed(() => this.eventosResource.value() ?? []);

  protected readonly eventoFiltroOptions = computed<TbiSelectOption<number>[]>(() => [
    { value: TODOS_LOS_EVENTOS, label: 'Todos los eventos' },
    ...this.eventos().map((e) => ({ value: e.id ?? 0, label: e.nombre })),
  ]);
  protected readonly eventoFiltro = signal<number | null>(TODOS_LOS_EVENTOS);
  /** Se mergea a la query del listado (`CursoCriteria.EventoId`); la tabla recarga al cambiar. */
  protected readonly filtros = computed<QueryParams>(() => ({
    eventoId: this.eventoFiltro() ?? TODOS_LOS_EVENTOS,
  }));

  /** Índice eventoId → nombre para la columna Evento (el header trae solo el id). */
  private readonly eventoPorId = computed(
    () => new Map(this.eventos().map((e) => [e.id, e.nombre])),
  );

  // Listado liviano (CursoHeaderDto): los álbumes se traen en el edit. `columns` es computed
  // porque la columna Evento depende del lookup (cuando carga, cambia la referencia del array y
  // la tabla la vuelve a tomar).
  protected readonly columns = computed<TbiColumn<Curso>[]>(() => {
    const porId = this.eventoPorId();
    return [
      { key: 'id', header: 'Id', sortable: true },
      { key: 'nombre', header: 'Nombre', sortable: true },
      {
        key: 'evento',
        header: 'Evento',
        cell: (row) => porId.get(row.eventoId) ?? `Evento ${row.eventoId}`,
      },
      { key: 'cantidadAlbumes', header: 'Álbumes', align: 'center' },
    ];
  });

  protected readonly fetch = (query: QueryParams) => this.service.crud.getAllByCriteria(query);

  // La baja de un curso con fotos la bloquea la API (el toast global muestra el motivo).
  protected readonly rowActions: TbiRowAction<Curso>[] = [
    { icon: 'edit', label: 'Editar', handler: (row) => this.edit(row) },
    {
      icon: 'delete',
      label: 'Eliminar',
      danger: true,
      confirm: (row) => ({
        title: 'Eliminar',
        message: `¿Eliminar el curso "${row.nombre}" y sus álbumes?`,
      }),
      run: (row) => this.removeFn(row)(),
    },
  ];

  protected add(): void {
    this.openEdit();
  }

  protected edit(row: Curso): void {
    this.openEdit(row.id);
  }

  /** Thunk de borrado que invoca la acción Eliminar de `rowActions` (spinner durante la API). */
  protected removeFn(row: Curso): () => Observable<unknown> {
    return () => {
      if (row.id == null) {
        return EMPTY;
      }
      return this.service.crud.delete(row.id).pipe(
        tap(() => {
          this.notify.success('Curso eliminado.');
          this.table().refresh();
        }),
      );
    };
  }

  private openEdit(id?: number): void {
    this.dialog
      .open<CursoEditComponent, { id?: number }, Curso>(CursoEditComponent, {
        data: { id },
        width: '640px',
        maxWidth: '95vw',
        maxHeight: '92vh',
        autoFocus: false,
      })
      .afterClosed()
      .subscribe((saved) => {
        if (saved) {
          this.notify.success('Curso guardado.');
          this.table().refresh();
        }
      });
  }
}
