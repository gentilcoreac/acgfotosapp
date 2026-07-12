import { ChangeDetectionStrategy, Component, inject, viewChild } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { Observable, EMPTY, tap } from 'rxjs';
import { QueryParams } from '../../../core/models/query-params.model';
import { NotificationService } from '../../../shared/feedback/notification.service';
import { TbiRowAction } from '../../../shared/ui/tbi-row-actions/tbi-row-actions.component';
import { TbiColumn, TbiTableComponent } from '../../../shared/ui/tbi-table/tbi-table.component';
import { GruposService } from '../data/grupos.service';
import { Grupo } from '../domain/grupo.model';
import { GrupoEditComponent } from './grupo-edit.component';

// TODO (Fase 4 - i18n): textos en español por ahora.
@Component({
  selector: 'tbi-grupos-list',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatButtonModule, MatIconModule, TbiTableComponent],
  templateUrl: './grupos-list.component.html',
  styleUrl: './grupos-list.component.scss',
})
export class GruposListComponent {
  private readonly service = inject(GruposService);
  private readonly dialog = inject(MatDialog);
  private readonly notify = inject(NotificationService);

  protected readonly table = viewChild.required(TbiTableComponent);

  // El listado muestra Id, Nombre y la cantidad de miembros (GrupoHeaderDto, contado en SQL).
  protected readonly columns: TbiColumn<Grupo>[] = [
    { key: 'id', header: 'Id', sortable: true },
    { key: 'nombre', header: 'Nombre', sortable: true },
    { key: 'cantidadMiembros', header: 'Miembros' },
  ];

  protected readonly fetch = (query: QueryParams) => this.service.crud.getAllByCriteria(query);

  protected readonly rowActions: TbiRowAction<Grupo>[] = [
    { icon: 'edit', label: 'Editar', handler: (row) => this.edit(row) },
    {
      icon: 'delete',
      label: 'Eliminar',
      danger: true,
      confirm: (row) => this.confirmDelete(row),
      run: (row) => this.removeFn(row)(),
    },
  ];

  protected add(): void {
    this.openEdit();
  }

  protected edit(row: Grupo): void {
    this.openEdit(row.id);
  }

  /** Datos del diálogo de confirmación de borrado de la acción Eliminar. */
  protected confirmDelete(row: Grupo) {
    return { title: 'Eliminar', message: `¿Eliminar el grupo "${row.nombre}"?` };
  }

  /** Thunk de borrado que invoca la acción Eliminar de `rowActions` (spinner durante la API). */
  protected removeFn(row: Grupo): () => Observable<unknown> {
    return () => {
      if (row.id == null) {
        return EMPTY;
      }
      return this.service.crud.delete(row.id).pipe(
        tap(() => {
          this.notify.success('Grupo eliminado.');
          this.table().refresh();
        }),
      );
    };
  }

  private openEdit(id?: number): void {
    this.dialog
      .open<GrupoEditComponent, { id?: number }, Grupo>(GrupoEditComponent, {
        data: { id },
        width: '480px',
      })
      .afterClosed()
      .subscribe((saved) => {
        if (saved) {
          this.notify.success('Grupo guardado.');
          this.table().refresh();
        }
      });
  }
}
