import { ChangeDetectionStrategy, Component, inject, viewChild } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { Observable, EMPTY, tap } from 'rxjs';
import { QueryParams } from '../../../core/models/query-params.model';
import { NotificationService } from '../../../shared/feedback/notification.service';
import { TbiRowAction } from '../../../shared/ui/tbi-row-actions/tbi-row-actions.component';
import { TbiColumn, TbiTableComponent } from '../../../shared/ui/tbi-table/tbi-table.component';
import { RolesService } from '../data/roles.service';
import { Rol } from '../domain/rol.model';
import { RolEditComponent } from './rol-edit.component';

// TODO (Fase 4 - i18n): textos en español por ahora.
@Component({
  selector: 'tbi-roles-list',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatButtonModule, MatIconModule, TbiTableComponent],
  templateUrl: './roles-list.component.html',
  styleUrl: './roles-list.component.scss',
})
export class RolesListComponent {
  private readonly service = inject(RolesService);
  private readonly dialog = inject(MatDialog);
  private readonly notify = inject(NotificationService);

  protected readonly table = viewChild.required(TbiTableComponent);

  // El listado del original sólo muestra Id y Descripción (RolHeaderDto no trae permisos).
  protected readonly columns: TbiColumn<Rol>[] = [
    { key: 'id', header: 'Id', sortable: true },
    { key: 'descripcion', header: 'Descripción', sortable: true },
  ];

  protected readonly fetch = (query: QueryParams) => this.service.crud.getAllByCriteria(query);

  protected readonly rowActions: TbiRowAction<Rol>[] = [
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

  protected edit(row: Rol): void {
    this.openEdit(row.id);
  }

  /** Datos del diálogo de confirmación de borrado de la acción Eliminar. */
  protected confirmDelete(row: Rol) {
    return { title: 'Eliminar', message: `¿Eliminar el rol "${row.descripcion}"?` };
  }

  /** Thunk de borrado que invoca la acción Eliminar de `rowActions` (spinner durante la API). */
  protected removeFn(row: Rol): () => Observable<unknown> {
    return () => {
      if (row.id == null) {
        return EMPTY;
      }
      return this.service.crud.delete(row.id).pipe(
        tap(() => {
          this.notify.success('Rol eliminado.');
          this.table().refresh();
        }),
      );
    };
  }

  private openEdit(id?: number): void {
    this.dialog
      .open<RolEditComponent, { id?: number }, Rol>(RolEditComponent, {
        data: { id },
        width: '480px',
      })
      .afterClosed()
      .subscribe((saved) => {
        if (saved) {
          this.notify.success('Rol guardado.');
          this.table().refresh();
        }
      });
  }
}
