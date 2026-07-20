import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  signal,
  viewChild,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { Observable, EMPTY, tap } from 'rxjs';
import { QueryParams } from '../../../core/models/query-params.model';
import { NotificationService } from '../../../shared/feedback/notification.service';
import { TbiRowAction } from '../../../shared/ui/tbi-row-actions/tbi-row-actions.component';
import { lookupResource } from '../../../shared/util/lookup-resource';
import {
  TbiSelectComponent,
  TbiSelectOption,
} from '../../../shared/ui/tbi-select/tbi-select.component';
import { TbiColumn, TbiTableComponent } from '../../../shared/ui/tbi-table/tbi-table.component';
import { ParametrosService } from '../data/parametros.service';
import { Parametro } from '../domain/parametro.model';
import { ParametroEditComponent } from './parametro-edit.component';

// TODO (Fase 4 - i18n): textos en español por ahora.
@Component({
  selector: 'tbi-parametros-list',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatIconModule,
    TbiTableComponent,
    TbiSelectComponent,
  ],
  templateUrl: './parametros-list.component.html',
  styleUrl: './parametros-list.component.scss',
})
export class ParametrosListComponent {
  private readonly service = inject(ParametrosService);
  private readonly dialog = inject(MatDialog);
  private readonly notify = inject(NotificationService);

  protected readonly table = viewChild.required(TbiTableComponent);

  protected readonly columns: TbiColumn<Parametro>[] = [
    { key: 'id', header: 'Id', sortable: true },
    { key: 'nombre', header: 'Nombre', sortable: true },
    { key: 'valor', header: 'Valor por defecto', sortable: true },
    { key: 'descripcion', header: 'Descripción', sortable: true, optional: true },
    { key: 'aplicacionNombre', header: 'Aplicación', optional: true },
  ];

  /** Filtro por aplicación (`ParametroCriteria.AplicacionId`), como el front original. */
  protected readonly filters = signal<QueryParams>({});
  private readonly aplicacionesResource = lookupResource(() => this.service.getAplicaciones(), []);
  protected readonly aplicacionFilterOptions = computed<TbiSelectOption<number | null>[]>(() => [
    { value: null, label: 'Todas las aplicaciones' },
    ...(this.aplicacionesResource.value() ?? []).map((a) => ({ value: a.id, label: a.nombre })),
  ]);
  protected readonly aplicacionFilter = new FormControl<number | null>(null);

  protected readonly fetch = (query: QueryParams) => this.service.crud.getAllByCriteria(query);

  protected readonly rowActions: TbiRowAction<Parametro>[] = [
    { icon: 'edit', label: 'Editar', handler: (row) => this.edit(row) },
    {
      icon: 'delete',
      label: 'Eliminar',
      danger: true,
      confirm: (row) => this.confirmDelete(row),
      run: (row) => this.removeFn(row)(),
    },
  ];

  constructor() {
    // El propio `tbi-table` recarga desde la página 0 al cambiar `filters` (su effect interno
    // reacciona al input); llamar `reload()` acá leería el valor VIEJO del input y dispararía un
    // fetch de más.
    this.aplicacionFilter.valueChanges.pipe(takeUntilDestroyed()).subscribe((aplicacionId) => {
      this.filters.set(aplicacionId == null ? {} : { aplicacionId });
    });
  }

  protected add(): void {
    this.openEdit();
  }

  protected edit(row: Parametro): void {
    this.openEdit(row.id);
  }

  /** Datos del diálogo de confirmación de borrado de la acción Eliminar. */
  protected confirmDelete(row: Parametro) {
    return { title: 'Eliminar', message: `¿Eliminar el parámetro "${row.nombre}"?` };
  }

  /** Thunk de borrado que invoca la acción Eliminar de `rowActions` (spinner durante la API). */
  protected removeFn(row: Parametro): () => Observable<unknown> {
    return () => {
      if (row.id == null) {
        return EMPTY;
      }
      return this.service.crud.delete(row.id).pipe(
        tap(() => {
          this.notify.success('Parámetro eliminado.');
          this.table().refresh();
        }),
      );
    };
  }

  private openEdit(id?: number): void {
    this.dialog
      .open<ParametroEditComponent, { id?: number }, Parametro>(ParametroEditComponent, {
        data: { id },
        width: '440px',
      })
      .afterClosed()
      .subscribe((saved) => {
        if (saved) {
          this.notify.success('Parámetro guardado.');
          this.table().refresh();
        }
      });
  }
}
