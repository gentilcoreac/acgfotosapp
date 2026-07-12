import { ChangeDetectionStrategy, Component, inject, viewChild } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { Router } from '@angular/router';
import { QueryParams } from '../../../core/models/query-params.model';
import { NotificationService } from '../../../shared/feedback/notification.service';
import { TbiRowAction } from '../../../shared/ui/tbi-row-actions/tbi-row-actions.component';
import {
  TbiColumn,
  TbiTableComponent,
  activoChip,
} from '../../../shared/ui/tbi-table/tbi-table.component';
import { TenantService } from '../data/tenant.service';
import { Tenant } from '../domain/tenant.model';
import { TenantEditComponent } from './tenant-edit.component';

// TODO (Fase 4 - i18n): textos en español por ahora.
@Component({
  selector: 'tbi-tenants-list',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatButtonModule, MatIconModule, TbiTableComponent],
  templateUrl: './tenants-list.component.html',
  styleUrl: './tenants-list.component.scss',
})
export class TenantsListComponent {
  private readonly service = inject(TenantService);
  private readonly dialog = inject(MatDialog);
  private readonly notify = inject(NotificationService);
  private readonly router = inject(Router);

  protected readonly table = viewChild.required(TbiTableComponent);

  protected readonly columns: TbiColumn<Tenant>[] = [
    { key: 'id', header: 'Id', sortable: true },
    { key: 'codigo', header: 'Código', sortable: true },
    { key: 'nombre', header: 'Nombre', sortable: true },
    { key: 'tituloWeb', header: 'Título web', sortable: true, optional: true },
    {
      key: 'activo',
      header: 'Activo',
      sortable: true,
      align: 'center',
      chip: (row) => activoChip(row.activo),
    },
    { key: 'hostName', header: 'HostName', sortable: true, optional: true },
  ];

  protected readonly fetch = (query: QueryParams) => this.service.crud.getAllByCriteria(query);

  // Tenants no se eliminan desde el listado: sólo edición.
  protected readonly rowActions: TbiRowAction<Tenant>[] = [
    { icon: 'edit', label: 'Editar', handler: (row) => this.edit(row) },
  ];

  protected add(): void {
    this.openEdit();
  }

  protected edit(row: Tenant): void {
    this.openEdit(row.id);
  }

  private openEdit(id?: number): void {
    this.dialog
      .open<TenantEditComponent, { id?: number }, Tenant>(TenantEditComponent, {
        data: { id },
        width: '820px',
        maxWidth: '95vw',
      })
      .afterClosed()
      .subscribe((saved) => {
        if (saved) {
          this.notify.success('Tenant guardado.');
          this.table().refresh();
        }
      });
  }
}
