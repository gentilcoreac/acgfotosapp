import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import { Router } from '@angular/router';
import { formatearPrecio, PedidoConfirmado } from '../../../../core/familia';
import { TbiButtonComponent } from '../../../../shared/ui/tbi-button/tbi-button.component';

/**
 * Vista propia de confirmación de pedido (Fase 2): ruta separada (`/pedido-confirmado`) a la que
 * `CarritoComponent.confirmar()` navega pasando el pedido por `NavigationExtras.state` — no hay
 * fetch propio, el pedido ya se confirmó.
 *
 * Sin `state` (entrada directa a la URL, F5 tras cerrar la pestaña, etc.) no hay nada que mostrar:
 * se manda de vuelta a la galería en vez de un cartel de error, porque no es un caso de fallo real.
 */
@Component({
  selector: 'tbi-pedido-confirmado',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatIconModule, TbiButtonComponent],
  template: `
    @if (pedido(); as pedido) {
      <div class="pedido-confirmado">
        <mat-icon class="pedido-confirmado__icono">check_circle</mat-icon>
        <h1>¡Pedido confirmado!</h1>
        <p class="pedido-confirmado__numero">Pedido #{{ pedido.id }}</p>
        <p class="pedido-confirmado__total">Total: {{ formatearPrecio(pedido.total) }}</p>
        <p class="pedido-confirmado__mensaje">
          El fotógrafo se va a contactar para coordinar la entrega y el pago.
        </p>
        <tbi-button label="Volver a mis fotos" (click)="volver()" />
      </div>
    }
  `,
  styles: `
    :host {
      display: flex;
      align-items: center;
      justify-content: center;
      min-height: 100vh;
      padding: 2.5rem 1.25rem;
    }

    .pedido-confirmado {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 0.5rem;
      max-width: 26rem;
      text-align: center;

      tbi-button {
        margin-top: 1rem;
      }
    }

    .pedido-confirmado__icono {
      width: 48px;
      height: 48px;
      font-size: 48px;
      color: var(--mat-sys-primary);
    }

    h1 {
      margin: 0.5rem 0 0;
      font-size: 1.375rem;
      font-weight: 500;
    }

    .pedido-confirmado__numero {
      margin: 0;
      color: var(--mat-sys-on-surface-variant);
    }

    .pedido-confirmado__total {
      margin: 0;
      font-size: 1.125rem;
      font-weight: 500;
    }

    .pedido-confirmado__mensaje {
      margin: 0;
      color: var(--mat-sys-on-surface-variant);
      font-size: 0.9375rem;
    }
  `,
})
export class PedidoConfirmadoComponent {
  private readonly router = inject(Router);

  protected readonly formatearPrecio = formatearPrecio;

  protected readonly pedido = signal<PedidoConfirmado | null>(this.leerPedidoDeNavegacion());

  constructor() {
    if (!this.pedido()) {
      this.router.navigateByUrl('/mi-album');
    }
  }

  protected volver(): void {
    this.router.navigateByUrl('/mi-album');
  }

  private leerPedidoDeNavegacion(): PedidoConfirmado | null {
    const state = (this.router.getCurrentNavigation()?.extras.state ?? history.state) as
      | { pedido?: PedidoConfirmado }
      | undefined;
    return state?.pedido ?? null;
  }
}
