import { ChangeDetectionStrategy, Component, inject, viewChild } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { Observable, EMPTY, tap } from 'rxjs';
import { QueryParams } from '../../../../core/models/query-params.model';
import { NotificationService } from '../../../../shared/feedback/notification.service';
import { TbiRowAction } from '../../../../shared/ui/tbi-row-actions/tbi-row-actions.component';
import {
  TbiChipCell,
  TbiColumn,
  TbiTableComponent,
} from '../../../../shared/ui/tbi-table/tbi-table.component';
import { EventosService } from '../data/eventos.service';
import { ESTADO_EVENTO, ESTADO_EVENTO_LABEL, Evento } from '../domain/evento.model';
import { EventoEditComponent } from './evento-edit.component';

/** Chip del estado del evento (tono según el ciclo de vida). */
function estadoChip(estado: Evento['estado']): TbiChipCell {
  switch (estado) {
    case ESTADO_EVENTO.Publicado:
      return { label: ESTADO_EVENTO_LABEL[estado], tone: 'success', icon: 'public' };
    case ESTADO_EVENTO.Cerrado:
      return { label: ESTADO_EVENTO_LABEL[estado], tone: 'warning', icon: 'lock' };
    default:
      return { label: ESTADO_EVENTO_LABEL[ESTADO_EVENTO.Borrador], tone: 'neutral', icon: 'edit' };
  }
}

/** `yyyy-MM-dd`/ISO → fecha local legible; vacío → em dash. */
function displayFecha(value?: string | null): string {
  if (!value) {
    return '—';
  }
  const [datePart] = value.split('T');
  const [y, m, d] = datePart.split('-').map(Number);
  if (!y || !m || !d) {
    return '—';
  }
  return new Date(y, m - 1, d).toLocaleDateString();
}

// TODO (Fase 4 - i18n): textos en español por ahora.
@Component({
  selector: 'tbi-eventos-list',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatButtonModule, MatIconModule, TbiTableComponent],
  templateUrl: './eventos-list.component.html',
  styleUrl: './eventos-list.component.scss',
})
export class EventosListComponent {
  private readonly service = inject(EventosService);
  private readonly dialog = inject(MatDialog);
  private readonly notify = inject(NotificationService);

  protected readonly table = viewChild.required(TbiTableComponent);

  // Listado liviano (EventoHeaderDto): el catálogo de tamaños se trae en el edit.
  protected readonly columns: TbiColumn<Evento>[] = [
    { key: 'id', header: 'Id', sortable: true },
    { key: 'nombre', header: 'Nombre', sortable: true },
    { key: 'lugarOrganizacion', header: 'Lugar/Organización', sortable: true, cell: (row) => row.lugarOrganizacion ?? '—' },
    { key: 'fecha', header: 'Fecha', sortable: true, cell: (row) => displayFecha(row.fecha) },
    {
      key: 'estado',
      header: 'Estado',
      align: 'center',
      chip: (row) => estadoChip(row.estado),
    },
    {
      key: 'fechaExpiracion',
      header: 'Disponible hasta',
      sortable: true,
      optional: true,
      cell: (row) => displayFecha(row.fechaExpiracion),
    },
  ];

  protected readonly fetch = (query: QueryParams) => this.service.crud.getAllByCriteria(query);

  protected readonly rowActions: TbiRowAction<Evento>[] = [
    { icon: 'edit', label: 'Editar', handler: (row) => this.edit(row) },
    {
      icon: 'delete',
      label: 'Eliminar',
      danger: true,
      confirm: (row) => ({
        title: 'Eliminar',
        message: `¿Eliminar el evento "${row.nombre}"?`,
      }),
      run: (row) => this.removeFn(row)(),
    },
  ];

  protected add(): void {
    this.openEdit();
  }

  protected edit(row: Evento): void {
    this.openEdit(row.id);
  }

  /** Thunk de borrado que invoca la acción Eliminar de `rowActions` (spinner durante la API). */
  protected removeFn(row: Evento): () => Observable<unknown> {
    return () => {
      if (row.id == null) {
        return EMPTY;
      }
      return this.service.crud.delete(row.id).pipe(
        tap(() => {
          this.notify.success('Evento eliminado.');
          this.table().refresh();
        }),
      );
    };
  }

  private openEdit(id?: number): void {
    this.dialog
      .open<EventoEditComponent, { id?: number }, Evento>(EventoEditComponent, {
        data: { id },
        width: '760px',
        maxWidth: '95vw',
        maxHeight: '92vh',
        autoFocus: false,
      })
      .afterClosed()
      .subscribe((saved) => {
        if (saved) {
          this.notify.success('Evento guardado.');
          this.table().refresh();
        }
      });
  }
}