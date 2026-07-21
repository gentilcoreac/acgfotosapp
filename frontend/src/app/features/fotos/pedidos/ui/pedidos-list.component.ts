import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  signal,
  viewChild,
} from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { Observable, catchError, map, of, tap } from 'rxjs';
import { formatearPrecio } from '../../../../core/familia';
import { QueryParams } from '../../../../core/models/query-params.model';
import { NotificationService } from '../../../../shared/feedback/notification.service';
import { TbiRowAction } from '../../../../shared/ui/tbi-row-actions/tbi-row-actions.component';
import {
  TbiSelectComponent,
  TbiSelectOption,
} from '../../../../shared/ui/tbi-select/tbi-select.component';
import { TbiColumn, TbiTableComponent } from '../../../../shared/ui/tbi-table/tbi-table.component';
import { EventosService } from '../../eventos/data/eventos.service';
import { Evento } from '../../eventos/domain/evento.model';
import { PedidosService } from '../data/pedidos.service';
import { ESTADO_PEDIDO, ESTADO_PEDIDO_LABEL, EstadoPedido, Pedido, estadoPedidoChip } from '../domain/pedido.model';
import { PedidoDetalleComponent } from './pedido-detalle.component';

/** Valor del filtro que significa "todos los eventos" (PedidoCriteria.EventoId = 0). */
const TODOS_LOS_EVENTOS = 0;

// TODO (Fase 4 - i18n): textos en español por ahora.
@Component({
  selector: 'tbi-pedidos-list',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatIconModule, TbiSelectComponent, TbiTableComponent],
  templateUrl: './pedidos-list.component.html',
  styleUrl: './pedidos-list.component.scss',
})
export class PedidosListComponent {
  private readonly service = inject(PedidosService);
  private readonly eventosService = inject(EventosService);
  private readonly dialog = inject(MatDialog);
  private readonly notify = inject(NotificationService);

  protected readonly table = viewChild.required(TbiTableComponent);
  protected readonly formatearPrecio = formatearPrecio;

  /** Eventos del tenant: alimentan el filtro (mismo patrón que `grupos-list`). */
  private readonly eventosResource = rxResource({
    stream: () =>
      this.eventosService.crud.getAll().pipe(
        map((result) => result.items),
        catchError(() => of<Evento[]>([])),
      ),
  });
  private readonly eventos = computed(() => this.eventosResource.value() ?? []);

  protected readonly eventoFiltroOptions = computed<TbiSelectOption<number>[]>(() => [
    { value: TODOS_LOS_EVENTOS, label: 'Todos los eventos' },
    ...this.eventos().map((e) => ({ value: e.id ?? 0, label: e.nombre })),
  ]);
  protected readonly eventoFiltro = signal<number | null>(TODOS_LOS_EVENTOS);

  protected readonly estadoFiltroOptions: TbiSelectOption<EstadoPedido | null>[] = [
    { value: null, label: 'Todos los estados' },
    ...Object.entries(ESTADO_PEDIDO_LABEL).map(([valor, label]) => ({
      value: Number(valor) as EstadoPedido,
      label,
    })),
  ];
  protected readonly estadoFiltro = signal<EstadoPedido | null>(null);

  /** Se mergea a la query del listado (`PedidoCriteria`); la tabla recarga al cambiar. */
  protected readonly filtros = computed<QueryParams>(() => ({
    eventoId: this.eventoFiltro() ?? TODOS_LOS_EVENTOS,
    ...(this.estadoFiltro() != null ? { estado: this.estadoFiltro() } : {}),
  }));

  protected readonly columns: TbiColumn<Pedido>[] = [
    { key: 'id', header: 'Id', sortable: true },
    {
      key: 'creadoEn',
      header: 'Fecha',
      sortable: true,
      cell: (row) => this.formatearFecha(row.creadoEn),
    },
    { key: 'participanteNombre', header: 'Participante', sortable: true },
    {
      key: 'contacto',
      header: 'Contacto',
      cell: (row) => `${row.nombreContacto} · ${row.telefonoContacto}`,
    },
    { key: 'cantidadItems', header: 'Ítems', align: 'center' },
    {
      key: 'total',
      header: 'Total',
      align: 'end',
      cell: (row) => this.formatearPrecio(row.total),
    },
    { key: 'estado', header: 'Estado', chip: (row) => estadoPedidoChip(row.estado) },
  ];

  protected readonly fetch = (query: QueryParams) => this.service.crud.getAllByCriteria(query);

  protected readonly rowActions: TbiRowAction<Pedido>[] = [
    { icon: 'visibility', label: 'Ver detalle', handler: (row) => this.verDetalle(row) },
    {
      icon: 'print',
      label: 'Marcar impreso',
      hidden: (row) => row.estado !== ESTADO_PEDIDO.Pendiente && row.estado !== ESTADO_PEDIDO.Pagado,
      confirm: (row) => ({
        title: 'Marcar impreso',
        message: `¿Marcar el pedido #${row.id} como impreso?`,
      }),
      run: (row) => this.cambiarEstadoFn(row, ESTADO_PEDIDO.Impreso, 'Pedido marcado como impreso.'),
    },
    {
      icon: 'local_shipping',
      label: 'Marcar entregado',
      hidden: (row) => row.estado !== ESTADO_PEDIDO.Impreso,
      confirm: (row) => ({
        title: 'Marcar entregado',
        message: `¿Marcar el pedido #${row.id} como entregado?`,
      }),
      run: (row) => this.cambiarEstadoFn(row, ESTADO_PEDIDO.Entregado, 'Pedido marcado como entregado.'),
    },
  ];

  /** Wrapper de `cambiarEstado`: toast + refresco de la tabla al terminar (mismo idiom que
   * `removeFn` en `grupos-list.component.ts` — `run` solo maneja el spinner, no notifica ni refresca). */
  private cambiarEstadoFn(row: Pedido, estado: EstadoPedido, mensaje: string): Observable<unknown> {
    return this.service.cambiarEstado(row.id, estado).pipe(
      tap(() => {
        this.notify.success(mensaje);
        this.table().refresh();
      }),
    );
  }

  private formatearFecha(iso: string): string {
    return new Date(iso).toLocaleString('es-AR', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    });
  }

  protected verDetalle(row: Pedido): void {
    this.dialog
      .open<PedidoDetalleComponent, { id: number }, boolean>(PedidoDetalleComponent, {
        data: { id: row.id },
        width: '640px',
        maxWidth: '95vw',
        maxHeight: '92vh',
        autoFocus: false,
      })
      .afterClosed()
      .subscribe((huboCambios) => {
        if (huboCambios) {
          this.table().refresh();
        }
      });
  }
}
