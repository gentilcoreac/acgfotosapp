import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialog, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { Observable, catchError, of, switchMap, tap } from 'rxjs';
import { formatearPrecio } from '../../../../core/familia';
import { ConfirmService } from '../../../../shared/feedback/confirm.service';
import { NotificationService } from '../../../../shared/feedback/notification.service';
import { TbiSelectComponent, TbiSelectOption } from '../../../../shared/ui/tbi-select/tbi-select.component';
import { TbiStatusChipComponent } from '../../../../shared/ui/tbi-status-chip/tbi-status-chip.component';
import { TbiTamanoChipComponent } from '../../../../shared/ui/tbi-tamano-chip/tbi-tamano-chip.component';
import { FotoImgComponent } from '../../fotos/ui/foto-img.component';
import { FotoPreviewDialogComponent } from '../../fotos/ui/foto-preview-dialog.component';
import { PedidosService } from '../data/pedidos.service';
import { ESTADO_PEDIDO, ESTADO_PEDIDO_LABEL, EstadoPedido, Pedido, PedidoItem, estadoPedidoChip } from '../domain/pedido.model';

/** Data que recibe el diálogo (mismo shape que `EditDialogData`, pero acá el id es obligatorio). */
export interface PedidoDetalleDialogData {
  id: number;
}

/** Líneas del pedido agrupadas por foto (misma idea que `/carrito`: una card por foto, una fila
 * por tamaño dentro) — reutiliza el mismo criterio visual, no el mismo componente (el carrito es
 * editable por la familia; acá es de solo lectura + las acciones de estado). */
interface GrupoPedidoDetalle {
  fotoId: number;
  nombreArchivoOriginal: string;
  lineas: (PedidoItem & { subtotal: number })[];
}

// TODO (Fase 4 - i18n): textos en español por ahora.
@Component({
  selector: 'tbi-pedido-detalle',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    MatButtonModule,
    MatDialogModule,
    MatIconModule,
    MatProgressBarModule,
    FotoImgComponent,
    TbiSelectComponent,
    TbiStatusChipComponent,
    TbiTamanoChipComponent,
  ],
  templateUrl: './pedido-detalle.component.html',
  styleUrl: './pedido-detalle.component.scss',
})
export class PedidoDetalleComponent {
  protected readonly data = inject<PedidoDetalleDialogData>(MAT_DIALOG_DATA);
  private readonly dialogRef = inject<MatDialogRef<PedidoDetalleComponent, boolean>>(MatDialogRef);
  private readonly dialog = inject(MatDialog);
  private readonly service = inject(PedidosService);
  private readonly notify = inject(NotificationService);
  private readonly confirmService = inject(ConfirmService);

  protected readonly formatearPrecio = formatearPrecio;
  protected readonly estadoPedidoChip = estadoPedidoChip;
  protected readonly ESTADO_PEDIDO = ESTADO_PEDIDO;

  /** `true` si se cambió el estado durante esta apertura (el padre refresca el listado al cerrar). */
  private readonly huboCambios = signal(false);

  protected readonly cambiandoEstado = signal(false);

  private readonly pedidoResource = rxResource({
    params: () => ({ id: this.data.id }),
    stream: ({ params }) =>
      this.service.crud.getById(params.id).pipe(
        // el toast de error lo emite el errorInterceptor (global)
        catchError(() => of(null)),
      ),
  });

  protected readonly pedido = computed<Pedido | null>(() => this.pedidoResource.value() ?? null);
  protected readonly cargando = computed(() => this.pedidoResource.isLoading());

  protected readonly grupos = computed<GrupoPedidoDetalle[]>(() => {
    const porFoto = new Map<number, GrupoPedidoDetalle>();
    for (const item of this.pedido()?.items ?? []) {
      let grupo = porFoto.get(item.fotoId);
      if (!grupo) {
        grupo = { fotoId: item.fotoId, nombreArchivoOriginal: item.nombreArchivoOriginal, lineas: [] };
        porFoto.set(item.fotoId, grupo);
      }
      grupo.lineas.push({ ...item, subtotal: item.cantidad * item.precioUnitarioSnapshot });
    }
    return [...porFoto.values()];
  });

  protected readonly puedeMarcarImpreso = computed(() => {
    const estado = this.pedido()?.estado;
    return estado === ESTADO_PEDIDO.Pendiente || estado === ESTADO_PEDIDO.Pagado;
  });
  protected readonly puedeMarcarEntregado = computed(
    () => this.pedido()?.estado === ESTADO_PEDIDO.Impreso,
  );

  /** Cualquier estado MENOS el actual — la API igual lo rechazaría como no-op, pero no tiene
   * sentido ofrecerlo. Siempre disponible (no depende del estado actual): es la vía de excepción
   * para cualquier corrección, no solo la del pedido ya entregado (2026-07-21, aclaración de
   * Alberto tras la vuelta anterior — la condicionaba a "sin botón guiado" y quedó sin poder
   * corregir un Pendiente). */
  protected readonly estadoManualOptions = computed<TbiSelectOption<EstadoPedido>[]>(() => {
    const actual = this.pedido()?.estado;
    return Object.entries(ESTADO_PEDIDO_LABEL)
      .map(([valor, label]) => ({ value: Number(valor) as EstadoPedido, label }))
      .filter((o) => o.value !== actual);
  });
  protected readonly estadoManualSeleccionado = signal<EstadoPedido | null>(null);

  protected abrirPreview(grupo: GrupoPedidoDetalle): void {
    // Acá el fotógrafo quiere ver la ORIGINAL primero (para el laboratorio); "vista del cliente"
    // queda como toggle secundario/opcional — al revés que la galería, donde el default sigue
    // siendo el derivado con watermark (pedido de Alberto 2026-07-21).
    this.dialog.open(FotoPreviewDialogComponent, {
      data: { id: grupo.fotoId, nombreArchivoOriginal: grupo.nombreArchivoOriginal, varianteInicial: 'original' },
      autoFocus: false,
      maxWidth: '100vw',
      width: '96vw',
      height: '94vh',
    });
  }

  protected marcarImpreso(): void {
    this.cambiarEstado(ESTADO_PEDIDO.Impreso, 'Pedido marcado como impreso.');
  }

  protected marcarEntregado(): void {
    this.cambiarEstado(ESTADO_PEDIDO.Entregado, 'Pedido marcado como entregado.');
  }

  /** Corrección manual: a diferencia de los botones de arriba, pide confirmación — no es el flujo
   * guiado, es "me equivoqué, corrijo" (pedido de Alberto 2026-07-20). */
  protected aplicarEstadoManual(): void {
    const nuevo = this.estadoManualSeleccionado();
    if (nuevo == null || this.cambiandoEstado()) {
      return;
    }
    this.confirmService
      .confirm({
        title: 'Cambiar estado',
        message: `¿Cambiar el estado del pedido a "${ESTADO_PEDIDO_LABEL[nuevo]}"? Usalo solo para corregir un estado equivocado.`,
      })
      .pipe(
        tap((confirmado) => {
          if (!confirmado) {
            this.estadoManualSeleccionado.set(null);
          }
        }),
        switchMap((confirmado) =>
          confirmado ? this.ejecutarCambioEstado(nuevo, `Estado cambiado a "${ESTADO_PEDIDO_LABEL[nuevo]}".`) : of(null),
        ),
      )
      .subscribe(() => this.estadoManualSeleccionado.set(null));
  }

  private cambiarEstado(estado: EstadoPedido, mensaje: string): void {
    this.ejecutarCambioEstado(estado, mensaje).subscribe();
  }

  private ejecutarCambioEstado(estado: EstadoPedido, mensaje: string): Observable<Pedido | null> {
    const id = this.pedido()?.id;
    if (id == null || this.cambiandoEstado()) {
      return of(null);
    }
    this.cambiandoEstado.set(true);
    return this.service.cambiarEstado(id, estado).pipe(
      tap({
        next: () => {
          this.notify.success(mensaje);
          this.huboCambios.set(true);
          this.pedidoResource.reload();
        },
        finalize: () => this.cambiandoEstado.set(false),
      }),
    );
  }

  protected cerrar(): void {
    this.dialogRef.close(this.huboCambios());
  }
}
