import { ChangeDetectionStrategy, Component, inject, viewChild } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { Observable, EMPTY, tap } from 'rxjs';
import { QueryParams } from '../../../../core/models/query-params.model';
import { NotificationService } from '../../../../shared/feedback/notification.service';
import { TbiRowAction } from '../../../../shared/ui/tbi-row-actions/tbi-row-actions.component';
import { TbiChipCell, TbiColumn, TbiTableComponent } from '../../../../shared/ui/tbi-table/tbi-table.component';
import { OpcionesPublicacionService } from '../data/opciones-publicacion.service';
import { OpcionesPublicacion } from '../domain/opciones-publicacion.model';
import { ComparadorTamanosComponent } from './comparador-tamanos.component';
import { OpcionesPublicacionEditComponent } from './opciones-publicacion-edit.component';

/** Chip "Default" para la única fila marcada como default del tenant (mismo criterio que `activoChip`). */
function defaultChip(esDefault: boolean): TbiChipCell | null {
  return esDefault ? { label: 'Default', tone: 'success', icon: 'star' } : null;
}

/**
 * Pantalla `/fotos/publicacion` (ADR-15 §5): ABM de conjuntos de resolución/calidad + comparador de
 * tamaños sobre una foto real, para decidir esos números con el resultado a la vista en vez de a ciegas.
 */
// TODO (Fase 4 - i18n): textos en español por ahora.
@Component({
  selector: 'tbi-publicacion',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatButtonModule, MatIconModule, TbiTableComponent, ComparadorTamanosComponent],
  templateUrl: './publicacion.component.html',
  styleUrl: './publicacion.component.scss',
})
export class PublicacionComponent {
  private readonly service = inject(OpcionesPublicacionService);
  private readonly dialog = inject(MatDialog);
  private readonly notify = inject(NotificationService);

  protected readonly table = viewChild.required(TbiTableComponent);

  protected readonly columns: TbiColumn<OpcionesPublicacion>[] = [
    { key: 'id', header: 'Id', sortable: true },
    { key: 'nombre', header: 'Nombre', sortable: true },
    { key: 'esDefault', header: 'Default', align: 'center', chip: (row) => defaultChip(row.esDefault) },
    { key: 'ladoMayorPreview', header: 'Lado mayor preview', sortable: true, cell: (row) => `${row.ladoMayorPreview}px` },
    { key: 'ladoMayorThumb', header: 'Lado mayor thumb', sortable: true, cell: (row) => `${row.ladoMayorThumb}px` },
    { key: 'calidad', header: 'Calidad', sortable: true, cell: (row) => `${row.calidad}%` },
  ];

  protected readonly fetch = (query: QueryParams) => this.service.crud.getAllByCriteria(query);

  protected readonly rowActions: TbiRowAction<OpcionesPublicacion>[] = [
    { icon: 'edit', label: 'Editar', handler: (row) => this.edit(row) },
    {
      icon: 'delete',
      label: 'Eliminar',
      danger: true,
      confirm: (row) => ({
        title: 'Eliminar',
        message: `¿Eliminar las opciones "${row.nombre}"?`,
      }),
      run: (row) => this.removeFn(row)(),
    },
  ];

  protected add(): void {
    this.openEdit();
  }

  protected edit(row: OpcionesPublicacion): void {
    this.openEdit(row.id);
  }

  protected removeFn(row: OpcionesPublicacion): () => Observable<unknown> {
    return () => {
      if (row.id == null) {
        return EMPTY;
      }
      return this.service.crud.delete(row.id).pipe(
        tap(() => {
          this.notify.success('Opciones eliminadas.');
          this.table().refresh();
        }),
      );
    };
  }

  private openEdit(id?: number): void {
    this.dialog
      .open<OpcionesPublicacionEditComponent, { id?: number }, OpcionesPublicacion>(
        OpcionesPublicacionEditComponent,
        { data: { id }, width: '520px', maxWidth: '95vw', autoFocus: false },
      )
      .afterClosed()
      .subscribe((saved) => {
        if (saved) {
          this.notify.success('Opciones guardadas.');
          this.table().refresh();
        }
      });
  }
}
