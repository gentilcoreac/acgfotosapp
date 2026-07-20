import { DatePipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  LOCALE_ID,
  computed,
  effect,
  inject,
  signal,
  viewChild,
} from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { NotificationService } from '../../../shared/feedback/notification.service';
import { QueryParams } from '../../../core/models/query-params.model';
import { TbiButtonComponent } from '../../../shared/ui/tbi-button/tbi-button.component';
import { TbiDatePickerComponent } from '../../../shared/ui/tbi-date-picker/tbi-date-picker.component';
import {
  TbiSelectComponent,
  TbiSelectOption,
} from '../../../shared/ui/tbi-select/tbi-select.component';
import {
  TbiChipCell,
  TbiColumn,
  TbiTableComponent,
} from '../../../shared/ui/tbi-table/tbi-table.component';
import { TbiRowAction } from '../../../shared/ui/tbi-row-actions/tbi-row-actions.component';
import { lookupResource } from '../../../shared/util/lookup-resource';
import { LogsService } from '../data/logs.service';
import { LogInfo } from '../domain/log.model';
import { LogDetailComponent } from './log-detail.component';

/** Tono del chip según el nivel del log (Error/Fatal rojo, Warning ámbar, resto neutro). */
function levelChip(level: string): TbiChipCell {
  const l = (level ?? '').toLowerCase();
  if (l.startsWith('err') || l.startsWith('fatal')) {
    return { label: level, tone: 'error' };
  }
  if (l.startsWith('warn')) {
    return { label: level, tone: 'warning' };
  }
  return { label: level || '—', tone: 'neutral' };
}

/** Recorta el mensaje en la grilla (el completo va en el detalle) para que las filas no crezcan. */
function recortar(texto: string, max = 140): string {
  if (!texto) {
    return '';
  }
  return texto.length > max ? texto.slice(0, max) + '…' : texto;
}

/**
 * Listado del log de aplicación (read-only, **root** cross-tenant vía `logInfo/AllTenants`). Tabla
 * paginada **liviana** (sin excepción/propiedades) con filtros (búsqueda por mensaje, nivel, rango de
 * fechas) y el detalle completo de cada entrada por id en diálogo.
 */
@Component({
  selector: 'tbi-logs-list',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    TbiTableComponent,
    TbiDatePickerComponent,
    TbiSelectComponent,
    TbiButtonComponent,
  ],
  templateUrl: './logs-list.component.html',
  styleUrl: './logs-list.component.scss',
})
export class LogsListComponent {
  private readonly service = inject(LogsService);
  private readonly dialog = inject(MatDialog);
  private readonly notify = inject(NotificationService);
  private readonly table = viewChild(TbiTableComponent);
  /** Mismo formato que el detalle (`log-detail.component.html`): mismo `LOCALE_ID` que el resto de
   * la app (no se registró data de `es-AR`; los tokens numéricos del formato no dependen del locale). */
  private readonly datePipe = new DatePipe(inject(LOCALE_ID));

  /** Spinner del botón "Filtrar": activo desde que se aplica hasta que la tabla termina de cargar. */
  protected readonly filtrando = signal(false);

  protected readonly nivelOpciones: TbiSelectOption<string>[] = [
    { value: '', label: 'Todos los niveles' },
    { value: 'Error', label: 'Error' },
    { value: 'Warning', label: 'Warning' },
    { value: 'Information', label: 'Information' },
    { value: 'Fatal', label: 'Fatal' },
    { value: 'Debug', label: 'Debug' },
  ];

  /** Tenants para el filtro y para resolver el nombre en la columna (root ve todos). */
  private readonly tenantsResource = lookupResource(() => this.service.getTenants(), []);
  private readonly tenants = computed(() => this.tenantsResource.value() ?? []);
  private readonly tenantNombre = computed(
    () => new Map(this.tenants().map((t) => [t.id, t.nombre])),
  );
  protected readonly tenantOpciones = computed<TbiSelectOption<string>[]>(() => [
    { value: '', label: 'Todos los tenants' },
    ...this.tenants().map((t) => ({ value: String(t.id), label: t.nombre })),
  ]);

  protected readonly filtros = new FormGroup({
    fechaDesde: new FormControl('', { nonNullable: true }),
    fechaHasta: new FormControl('', { nonNullable: true }),
    level: new FormControl('', { nonNullable: true }),
    tenantId: new FormControl('', { nonNullable: true }),
  });

  /** Filtros de fecha/nivel/tenant aplicados (la búsqueda por mensaje va por el buscador). */
  protected readonly filters = signal<QueryParams>({});

  protected readonly columns: TbiColumn<LogInfo>[] = [
    {
      key: 'timeStamp',
      header: 'Fecha y hora',
      sortable: true,
      cell: (row) =>
        row.timeStamp
          ? (this.datePipe.transform(row.timeStamp, 'dd/MM/yyyy HH:mm:ss') ?? '—')
          : '—',
    },
    { key: 'level', header: 'Nivel', align: 'center', chip: (row) => levelChip(row.level) },
    { key: 'message', header: 'Mensaje', cell: (row) => recortar(row.message) },
    {
      key: 'tenantId',
      header: 'Tenant',
      align: 'center',
      cell: (row) => this.tenantNombre().get(row.tenantId) ?? String(row.tenantId),
    },
  ];

  constructor() {
    // Apaga el spinner del botón "Filtrar" cuando la tabla termina de cargar.
    effect(() => {
      if (this.table()?.loading() === false) {
        this.filtrando.set(false);
      }
    });
  }

  protected readonly rowActions: TbiRowAction<LogInfo>[] = [
    { icon: 'visibility', label: 'Ver detalle', handler: (row) => this.verDetalle(row) },
  ];

  protected readonly fetch = (query: QueryParams) => this.service.getAllTenants(query);

  /** Aplica los filtros de fecha/nivel/tenant (la tabla recarga sola al cambiar `filters`). */
  protected aplicar(): void {
    const { fechaDesde, fechaHasta, level, tenantId } = this.filtros.getRawValue();
    if (fechaDesde && fechaHasta && fechaDesde > fechaHasta) {
      this.notify.error('La fecha "desde" no puede ser posterior a la fecha "hasta".');
      return;
    }
    const filtros: QueryParams = {};
    if (fechaDesde) {
      filtros['fechaDesde'] = `${fechaDesde}T00:00:00`;
    }
    if (fechaHasta) {
      filtros['fechaHasta'] = `${fechaHasta}T23:59:59`;
    }
    if (level) {
      filtros['level'] = level;
    }
    if (tenantId) {
      filtros['tenantId'] = Number(tenantId);
    }
    this.filtrando.set(true);
    this.filters.set(filtros);
  }

  /** Limpia los filtros de fecha/nivel/tenant y la búsqueda libre (vive dentro de `tbi-table`). */
  protected limpiar(): void {
    this.filtros.reset();
    this.filters.set({});
    this.table()?.clearSearch();
  }

  protected verDetalle(row: LogInfo): void {
    this.dialog.open(LogDetailComponent, {
      data: { id: row.id },
      width: '720px',
      maxWidth: '95vw',
      maxHeight: '92vh',
      autoFocus: false,
    });
  }
}
