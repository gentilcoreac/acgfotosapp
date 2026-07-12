import { ChangeDetectionStrategy, Component, inject, viewChild } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { Observable, EMPTY, tap } from 'rxjs';
import { QueryParams } from '../../../core/models/query-params.model';
import { NotificationService } from '../../../shared/feedback/notification.service';
import { TbiRowAction } from '../../../shared/ui/tbi-row-actions/tbi-row-actions.component';
import { TbiColumn, TbiTableComponent } from '../../../shared/ui/tbi-table/tbi-table.component';
import { TiposLicenciaService } from '../data/tipos-licencia.service';
import { TipoLicencia } from '../domain/tipo-licencia.model';
import { TiposLicenciaEditComponent } from './tipos-licencia-edit.component';

// TODO (Fase 4 - i18n): textos en español por ahora.
@Component({
  selector: 'tbi-tipos-licencias-list',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatButtonModule, MatIconModule, TbiTableComponent],
  templateUrl: './tipos-licencias-list.component.html',
  styleUrl: './tipos-licencias-list.component.scss',
})
export class TiposLicenciasListComponent {
  private readonly service = inject(TiposLicenciaService);
  private readonly dialog = inject(MatDialog);
  private readonly notify = inject(NotificationService);

  protected readonly table = viewChild.required(TbiTableComponent);

  protected readonly columns: TbiColumn<TipoLicencia>[] = [
    { key: 'id', header: 'Id', sortable: true },
    { key: 'codigoTipoLicencia', header: 'Código', sortable: true },
    { key: 'descripcion', header: 'Descripción', sortable: true },
    {
      key: 'esDefaultParaNuevoTenant',
      header: 'Default',
      align: 'center',
      optional: true,
      // Sólo se marca el tipo de licencia default (chip); el resto deja la celda vacía.
      chip: (row) =>
        row.esDefaultParaNuevoTenant ? { label: 'Default', tone: 'success', icon: 'star' } : null,
    },
  ];

  protected readonly fetch = (query: QueryParams) => this.service.crud.getAllByCriteria(query);

  protected readonly rowActions: TbiRowAction<TipoLicencia>[] = [
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

  protected edit(row: TipoLicencia): void {
    this.openEdit(row.id);
  }

  /** Datos del diálogo de confirmación de borrado de la acción Eliminar. */
  protected confirmDelete(row: TipoLicencia) {
    return { title: 'Eliminar', message: `¿Eliminar el tipo de licencia "${row.descripcion}"?` };
  }

  /** Thunk de borrado que invoca la acción Eliminar de `rowActions` (spinner durante la API). */
  protected removeFn(row: TipoLicencia): () => Observable<unknown> {
    return () => {
      if (row.id == null) {
        return EMPTY;
      }
      return this.service.crud.delete(row.id).pipe(
        tap(() => {
          this.notify.success('Tipo de licencia eliminado.');
          this.table().refresh();
        }),
      );
    };
  }

  private openEdit(id?: number): void {
    this.dialog
      .open<TiposLicenciaEditComponent, { id?: number }, TipoLicencia>(TiposLicenciaEditComponent, {
        data: { id },
        width: '440px',
      })
      .afterClosed()
      .subscribe((saved) => {
        if (saved) {
          this.notify.success('Tipo de licencia guardado.');
          this.table().refresh();
        }
      });
  }
}
