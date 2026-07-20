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
import {
  TbiColumn,
  TbiTableComponent,
  activoChip,
} from '../../../shared/ui/tbi-table/tbi-table.component';
import { MenusService } from '../data/menus.service';
import { Menu } from '../domain/menu.model';
import { MenuEditComponent } from './menu-edit.component';

// TODO (Fase 4 - i18n): textos en español por ahora.
@Component({
  selector: 'tbi-menus-list',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatIconModule,
    TbiTableComponent,
    TbiSelectComponent,
  ],
  templateUrl: './menus-list.component.html',
  styleUrl: './menus-list.component.scss',
})
export class MenusListComponent {
  private readonly service = inject(MenusService);
  private readonly dialog = inject(MatDialog);
  private readonly notify = inject(NotificationService);

  protected readonly table = viewChild.required(TbiTableComponent);

  // `permisoNombre` no es ordenable server-side (es proyección de `Permiso.Nombre`, no una columna
  // de `Menu`); el resto son columnas reales de la entidad.
  protected readonly columns: TbiColumn<Menu>[] = [
    { key: 'id', header: 'Id', sortable: true },
    { key: 'nombre', header: 'Nombre', sortable: true, optional: true },
    { key: 'codigo', header: 'Código', sortable: true },
    { key: 'permisoNombre', header: 'Permiso', optional: true },
    { key: 'orden', header: 'Orden', sortable: true, optional: true },
    {
      key: 'estado',
      header: 'Activo',
      sortable: true,
      align: 'center',
      optional: true,
      chip: (row) => activoChip(row.estado),
    },
  ];

  /** Filtro opcional por aplicación (la API lo soporta en el criteria). `null` = todas. */
  protected readonly aplicacionFilter = new FormControl<number | null>(null);
  private readonly aplicacionesResource = lookupResource(() => this.service.getAplicaciones(), []);
  protected readonly aplicacionOptions = computed<TbiSelectOption<number | null>[]>(() => [
    { value: null, label: 'Todas las aplicaciones' },
    ...(this.aplicacionesResource.value() ?? []).map((a) => ({ value: a.id, label: a.nombre })),
  ]);
  protected readonly filters = signal<QueryParams>({});

  protected readonly fetch = (query: QueryParams) => this.service.crud.getAllByCriteria(query);

  protected readonly rowActions: TbiRowAction<Menu>[] = [
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
    // Al cambiar el filtro, actualizamos `filters`: el propio `tbi-table` recarga desde la página 0
    // (su effect interno reacciona al input). Llamar `reload()` acá además leería el valor VIEJO del
    // input (recién se propaga en el siguiente ciclo) y disparaba un fetch de más.
    this.aplicacionFilter.valueChanges.pipe(takeUntilDestroyed()).subscribe((aplicacionId) => {
      this.filters.set(aplicacionId == null ? {} : { aplicacionId });
    });
  }

  protected add(): void {
    this.openEdit();
  }

  protected edit(row: Menu): void {
    this.openEdit(row.id);
  }

  /** Datos del diálogo de confirmación de borrado de la acción Eliminar. */
  protected confirmDelete(row: Menu) {
    return { title: 'Eliminar', message: `¿Eliminar el menú "${row.nombre}"?` };
  }

  /** Thunk de borrado que invoca la acción Eliminar de `rowActions` (spinner durante la API). */
  protected removeFn(row: Menu): () => Observable<unknown> {
    return () => {
      if (row.id == null) {
        return EMPTY;
      }
      return this.service.crud.delete(row.id).pipe(
        tap(() => {
          this.notify.success('Menú eliminado.');
          this.table().refresh();
        }),
      );
    };
  }

  private openEdit(id?: number): void {
    this.dialog
      .open<MenuEditComponent, { id?: number }, Menu>(MenuEditComponent, {
        data: { id },
        width: '520px',
        maxWidth: '95vw',
        maxHeight: '92vh',
        autoFocus: false,
      })
      .afterClosed()
      .subscribe((saved) => {
        if (saved) {
          this.notify.success('Menú guardado.');
          this.table().refresh();
        }
      });
  }
}
