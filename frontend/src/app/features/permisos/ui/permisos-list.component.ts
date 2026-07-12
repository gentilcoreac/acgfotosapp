import { ChangeDetectionStrategy, Component, inject, viewChild } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { Observable, EMPTY, tap } from 'rxjs';
import { QueryParams } from '../../../core/models/query-params.model';
import { NotificationService } from '../../../shared/feedback/notification.service';
import { TbiRowAction } from '../../../shared/ui/tbi-row-actions/tbi-row-actions.component';
import {
  TbiColumn,
  TbiTableComponent,
  activoChip,
} from '../../../shared/ui/tbi-table/tbi-table.component';
import { PermisosService } from '../data/permisos.service';
import { Permiso } from '../domain/permiso.model';
import { PermisoEditComponent } from './permiso-edit.component';

// TODO (Fase 4 - i18n): textos en español por ahora.
@Component({
  selector: 'tbi-permisos-list',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatButtonModule, MatIconModule, TbiTableComponent],
  templateUrl: './permisos-list.component.html',
  styleUrl: './permisos-list.component.scss',
})
export class PermisosListComponent {
  private readonly service = inject(PermisosService);
  private readonly dialog = inject(MatDialog);
  private readonly notify = inject(NotificationService);

  protected readonly table = viewChild.required(TbiTableComponent);

  // Listado liviano (PermisoHeaderDto): sin endpoints; aplicación y padre por su descripción.
  protected readonly columns: TbiColumn<Permiso>[] = [
    { key: 'id', header: 'Id', sortable: true },
    { key: 'nombre', header: 'Nombre', sortable: true },
    { key: 'codigoPermiso', header: 'Código', sortable: true, optional: true },
    { key: 'aplicacionDescripcion', header: 'Aplicación', sortable: true },
    { key: 'permisoPadreDescripcion', header: 'Permiso padre', sortable: true, optional: true },
    { key: 'activo', header: 'Activo', align: 'center', chip: (row) => activoChip(row.activo) },
  ];

  protected readonly fetch = (query: QueryParams) => this.service.crud.getAllByCriteria(query);

  protected readonly rowActions: TbiRowAction<Permiso>[] = [
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

  protected edit(row: Permiso): void {
    this.openEdit(row.id);
  }

  /** Datos del diálogo de confirmación de borrado de la acción Eliminar. */
  protected confirmDelete(row: Permiso) {
    return { title: 'Eliminar', message: `¿Eliminar el permiso "${row.nombre}"?` };
  }

  /** Thunk de borrado que invoca la acción Eliminar de `rowActions` (spinner durante la API). */
  protected removeFn(row: Permiso): () => Observable<unknown> {
    return () => {
      if (row.id == null) {
        return EMPTY;
      }
      return this.service.crud.delete(row.id).pipe(
        tap(() => {
          this.notify.success('Permiso eliminado.');
          this.table().refresh();
        }),
      );
    };
  }

  private openEdit(id?: number): void {
    this.dialog
      .open<PermisoEditComponent, { id?: number }, Permiso>(PermisoEditComponent, {
        data: { id },
        width: '900px',
        maxWidth: '95vw',
        maxHeight: '92vh',
        autoFocus: false,
      })
      .afterClosed()
      .subscribe((saved) => {
        if (saved) {
          this.notify.success('Permiso guardado.');
          this.table().refresh();
        }
      });
  }
}
