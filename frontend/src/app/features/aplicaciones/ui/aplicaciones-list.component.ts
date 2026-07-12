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
import { AplicacionesService } from '../data/aplicaciones.service';
import { Aplicacion } from '../domain/aplicacion.model';
import { AplicacionEditComponent } from './aplicacion-edit.component';

// TODO (Fase 4 - i18n): textos en español por ahora.
@Component({
  selector: 'tbi-aplicaciones-list',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatButtonModule, MatIconModule, TbiTableComponent],
  templateUrl: './aplicaciones-list.component.html',
  styleUrl: './aplicaciones-list.component.scss',
})
export class AplicacionesListComponent {
  private readonly service = inject(AplicacionesService);
  private readonly dialog = inject(MatDialog);
  private readonly notify = inject(NotificationService);

  protected readonly table = viewChild.required(TbiTableComponent);

  protected readonly columns: TbiColumn<Aplicacion>[] = [
    { key: 'id', header: 'Id', sortable: true },
    { key: 'codigo', header: 'Código', sortable: true },
    { key: 'nombre', header: 'Nombre', sortable: true },
    {
      key: 'activo',
      header: 'Activo',
      sortable: true,
      align: 'center',
      optional: true,
      chip: (row) => activoChip(row.activo),
    },
  ];

  protected readonly fetch = (query: QueryParams) => this.service.crud.getAllByCriteria(query);

  protected readonly rowActions: TbiRowAction<Aplicacion>[] = [
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

  protected edit(row: Aplicacion): void {
    this.openEdit(row.id);
  }

  /** Datos del diálogo de confirmación de borrado de la acción Eliminar. */
  protected confirmDelete(row: Aplicacion) {
    return { title: 'Eliminar', message: `¿Eliminar la aplicación "${row.nombre}"?` };
  }

  /** Thunk de borrado que invoca la acción Eliminar de `rowActions` (spinner durante la API). */
  protected removeFn(row: Aplicacion): () => Observable<unknown> {
    return () => {
      if (row.id == null) {
        return EMPTY;
      }
      return this.service.crud.delete(row.id).pipe(
        tap(() => {
          this.notify.success('Aplicación eliminada.');
          this.table().refresh();
        }),
      );
    };
  }

  private openEdit(id?: number): void {
    this.dialog
      .open<AplicacionEditComponent, { id?: number }, Aplicacion>(AplicacionEditComponent, {
        data: { id },
        width: '440px',
      })
      .afterClosed()
      .subscribe((saved) => {
        if (saved) {
          this.notify.success('Aplicación guardada.');
          this.table().refresh();
        }
      });
  }
}
