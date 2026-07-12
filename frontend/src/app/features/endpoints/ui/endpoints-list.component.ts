import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  inject,
  signal,
  viewChild,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { EMPTY, Observable, tap } from 'rxjs';
import { QueryParams } from '../../../core/models/query-params.model';
import { NotificationService } from '../../../shared/feedback/notification.service';
import { TbiRowAction } from '../../../shared/ui/tbi-row-actions/tbi-row-actions.component';
import {
  TbiChipCell,
  TbiColumn,
  TbiTableComponent,
  activoChip,
} from '../../../shared/ui/tbi-table/tbi-table.component';
import { EndpointsService } from '../data/endpoints.service';
import { Endpoint } from '../domain/endpoint.model';
import { EndpointEditComponent } from './endpoint-edit.component';

function httpMethodChip(method: string): TbiChipCell {
  const m = (method ?? '').toUpperCase();
  const map: Record<string, TbiChipCell> = {
    GET: { label: 'GET', tone: 'neutral' },
    POST: { label: 'POST', tone: 'success' },
    PUT: { label: 'PUT', tone: 'warning' },
    PATCH: { label: 'PATCH', tone: 'warning' },
    DELETE: { label: 'DELETE', tone: 'error' },
  };
  return map[m] ?? { label: m || '—', tone: 'neutral' };
}

@Component({
  selector: 'tbi-endpoints-list',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatButtonModule, MatIconModule, TbiTableComponent],
  templateUrl: './endpoints-list.component.html',
  styleUrl: './endpoints-list.component.scss',
})
export class EndpointsListComponent {
  private readonly service = inject(EndpointsService);
  private readonly dialog = inject(MatDialog);
  private readonly notify = inject(NotificationService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly table = viewChild.required(TbiTableComponent);
  protected readonly discovering = signal(false);

  protected readonly columns: TbiColumn<Endpoint>[] = [
    { key: 'moduleName', header: 'Módulo', sortable: true },
    { key: 'namespace', header: 'Namespace', sortable: true, optional: true, hidden: true },
    { key: 'controllerName', header: 'Controller', sortable: true },
    { key: 'actionName', header: 'Acción', sortable: true },
    {
      key: 'httpMethod',
      header: 'Método',
      sortable: true,
      align: 'center',
      chip: (row) => httpMethodChip(row.httpMethod),
    },
    { key: 'route', header: 'Ruta', sortable: true },
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

  protected readonly rowActions: TbiRowAction<Endpoint>[] = [
    { icon: 'edit', label: 'Editar', handler: (row) => this.edit(row) },
    {
      icon: 'delete',
      label: 'Eliminar',
      danger: true,
      confirm: (row) => this.confirmDelete(row),
      run: (row) => this.removeFn(row)(),
    },
  ];

  protected descubrir(): void {
    this.discovering.set(true);
    this.service
      .descubrir()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (endpoints) => {
          this.discovering.set(false);
          this.notify.success(`${endpoints.length} endpoints registrados.`);
          this.table().reload();
        },
        error: () => this.discovering.set(false),
      });
  }

  protected add(): void {
    this.openEdit();
  }

  protected edit(row: Endpoint): void {
    this.openEdit(row.id);
  }

  protected confirmDelete(row: Endpoint) {
    return {
      title: 'Eliminar',
      message: `¿Eliminar el endpoint "${row.httpMethod} ${row.route}"?`,
    };
  }

  protected removeFn(row: Endpoint): () => Observable<unknown> {
    return () => {
      if (row.id == null) return EMPTY;
      return this.service.crud.delete(row.id).pipe(
        tap(() => {
          this.notify.success('Endpoint eliminado.');
          this.table().refresh();
        }),
      );
    };
  }

  private openEdit(id?: number): void {
    this.dialog
      .open<EndpointEditComponent, { id?: number }, Endpoint>(EndpointEditComponent, {
        data: { id },
        width: '600px',
      })
      .afterClosed()
      .subscribe((saved) => {
        if (saved) {
          this.notify.success('Endpoint guardado.');
          this.table().refresh();
        }
      });
  }
}
