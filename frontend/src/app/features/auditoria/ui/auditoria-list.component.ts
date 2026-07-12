import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { QueryParams } from '../../../core/models/query-params.model';
import { TbiButtonComponent } from '../../../shared/ui/tbi-button/tbi-button.component';
import { TbiDatePickerComponent } from '../../../shared/ui/tbi-date-picker/tbi-date-picker.component';
import { TbiTextFieldComponent } from '../../../shared/ui/tbi-text-field/tbi-text-field.component';
import {
  TbiChipCell,
  TbiColumn,
  TbiTableComponent,
} from '../../../shared/ui/tbi-table/tbi-table.component';
import { TbiRowAction } from '../../../shared/ui/tbi-row-actions/tbi-row-actions.component';
import { AuditoriaService } from '../data/auditoria.service';
import { Auditoria } from '../domain/auditoria.model';
import { AuditoriaDetailComponent } from './auditoria-detail.component';

/** Tono del chip según la familia del status HTTP (2xx ok, 4xx aviso, 5xx error). */
function statusChip(code: string): TbiChipCell | null {
  if (!code) {
    return null;
  }
  const familia = code.trim().charAt(0);
  if (familia === '2') {
    return { label: code, tone: 'success' };
  }
  if (familia === '4') {
    return { label: code, tone: 'warning' };
  }
  if (familia === '5') {
    return { label: code, tone: 'error' };
  }
  return { label: code, tone: 'neutral' };
}

/**
 * Listado de Auditoría (read-only): tabla paginada server-side + barra de filtros (rango de fechas,
 * servicio, status) + detalle por fila en diálogo. No tiene alta/edición/baja (es un log).
 */
@Component({
  selector: 'tbi-auditoria-list',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    TbiTableComponent,
    TbiDatePickerComponent,
    TbiTextFieldComponent,
    TbiButtonComponent,
  ],
  templateUrl: './auditoria-list.component.html',
  styleUrl: './auditoria-list.component.scss',
})
export class AuditoriaListComponent {
  private readonly service = inject(AuditoriaService);
  private readonly dialog = inject(MatDialog);

  protected readonly filtros = new FormGroup({
    fechaDesde: new FormControl('', { nonNullable: true }),
    fechaHasta: new FormControl('', { nonNullable: true }),
    servicio: new FormControl('', { nonNullable: true }),
    resultStatusCode: new FormControl('', { nonNullable: true }),
  });

  /** Filtros aplicados (se mergean a la query de la tabla). Se actualiza al "Filtrar". */
  protected readonly filters = signal<QueryParams>({});

  protected readonly columns: TbiColumn<Auditoria>[] = [
    {
      key: 'fechaHora',
      header: 'Fecha y hora',
      sortable: true,
      cell: (row) => (row.fechaHora ? new Date(row.fechaHora).toLocaleString() : '—'),
    },
    { key: 'usuarioNombre', header: 'Usuario', cell: (row) => row.usuarioNombre || '—' },
    { key: 'servicio', header: 'Servicio', sortable: true },
    { key: 'metodo', header: 'Método', sortable: true },
    { key: 'httpMethod', header: 'HTTP', align: 'center', optional: true },
    {
      key: 'requestAbsolutePath',
      header: 'Ruta',
      optional: true,
      hidden: true,
    },
    {
      key: 'resultStatusCode',
      header: 'Resp.',
      align: 'center',
      sortable: true,
      chip: (row) => statusChip(row.resultStatusCode),
    },
  ];

  protected readonly rowActions: TbiRowAction<Auditoria>[] = [
    { icon: 'visibility', label: 'Ver detalle', handler: (row) => this.verDetalle(row) },
  ];

  protected readonly fetch = (query: QueryParams) => this.service.crud.getAllByCriteria(query);

  /** Aplica los filtros del formulario a la tabla (rango de fechas con bordes inclusivos del día). */
  protected aplicar(): void {
    const { fechaDesde, fechaHasta, servicio, resultStatusCode } = this.filtros.getRawValue();
    const filtros: QueryParams = {};
    if (fechaDesde) {
      filtros['fechaDesde'] = `${fechaDesde}T00:00:00`;
    }
    if (fechaHasta) {
      filtros['fechaHasta'] = `${fechaHasta}T23:59:59`;
    }
    if (servicio) {
      filtros['servicio'] = servicio;
    }
    if (resultStatusCode) {
      filtros['resultStatusCode'] = resultStatusCode;
    }
    // La tabla reacciona al cambio de `filters` (effect interno) y recarga sola.
    this.filters.set(filtros);
  }

  /** Limpia los filtros (la tabla recarga sola al cambiar `filters`). */
  protected limpiar(): void {
    this.filtros.reset();
    this.filters.set({});
  }

  protected verDetalle(row: Auditoria): void {
    this.dialog.open(AuditoriaDetailComponent, {
      data: { id: row.id },
      width: '720px',
      maxWidth: '95vw',
      maxHeight: '92vh',
      autoFocus: false,
    });
  }
}
