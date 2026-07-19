import { ChangeDetectionStrategy, Component, computed, effect, inject, input, signal, untracked } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatIconModule } from '@angular/material/icon';
import { CarritoStore, TamanoPrecio } from '../../../../core/familia';

/**
 * Selector de tamaño + cantidad para UNA foto (Fase 2). Recibe el catálogo ya cargado (no hace su
 * propio fetch) para poder usarse dos veces en la misma pantalla — grilla (dentro de un
 * `MatBottomSheet`) y preview ampliado — sin duplicar la llamada de red. Habla directo con
 * `CarritoStore` (mismo criterio que `tbi-foto-familia-img`: los componentes de familia no hacen
 * prop-drilling de eventos para algo que ya vive en un store de `core/familia`).
 *
 * El contador ES la cantidad real en el carrito para `fotoId` + tamaño elegido (arranca en 0, no
 * en 1): +/- escriben directo al store, sin un botón "Agregar" aparte que hubiera que confirmar.
 * Esto también evita el bug de arrastrar un número "fantasma" al cambiar de foto en el carrusel —
 * como el número sale del store por `fotoId`, cambia solo al cambiar el input.
 */
@Component({
  selector: 'tbi-agregar-carrito',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatButtonModule, MatButtonToggleModule, MatIconModule],
  template: `
    @if (tamanosPrecios().length === 0) {
      <p class="agregar-carrito__hint">Todavía no hay tamaños disponibles para pedir.</p>
    } @else {
      <div class="agregar-carrito__fila">
        <mat-button-toggle-group
          class="agregar-carrito__tamanos"
          [value]="tamanoSeleccionadoId()"
          (change)="tamanoSeleccionadoId.set($event.value)"
          aria-label="Tamaño"
        >
          @for (tamano of tamanosPrecios(); track tamano.id) {
            <mat-button-toggle [value]="tamano.id">{{ tamano.nombre }}</mat-button-toggle>
          }
        </mat-button-toggle-group>

        <div class="agregar-carrito__cantidad" role="group" aria-label="Cantidad en el carrito">
          <button
            matIconButton
            type="button"
            [disabled]="cantidad() === 0"
            (click)="restar()"
            aria-label="Quitar una copia"
          >
            <mat-icon>remove</mat-icon>
          </button>
          <span class="agregar-carrito__numero">{{ cantidad() }}</span>
          <button matIconButton type="button" (click)="sumar()" aria-label="Agregar una copia">
            <mat-icon>add</mat-icon>
          </button>
        </div>
      </div>
    }
  `,
  styles: `
    :host {
      display: block;
    }

    .agregar-carrito__hint {
      margin: 0;
      color: var(--mat-sys-on-surface-variant);
      font-size: 0.875rem;
    }

    .agregar-carrito__fila {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 0.75rem;
      flex-wrap: wrap;
    }

    .agregar-carrito__tamanos {
      flex-wrap: wrap;
    }

    .agregar-carrito__cantidad {
      display: flex;
      align-items: center;
      gap: 0.25rem;

      button {
        border-radius: 50%;
        transition: background-color 0.15s ease, transform 0.1s ease;

        &:hover:not(:disabled) {
          background-color: var(--mat-sys-surface-variant);
        }

        &:active:not(:disabled) {
          transform: scale(0.92);
        }
      }
    }

    .agregar-carrito__numero {
      min-width: 1.5rem;
      text-align: center;
      font-variant-numeric: tabular-nums;
      font-weight: 500;
    }
  `,
})
export class AgregarCarritoComponent {
  private readonly carrito = inject(CarritoStore);

  readonly fotoId = input.required<number>();
  readonly tamanosPrecios = input.required<TamanoPrecio[]>();

  protected readonly tamanoSeleccionadoId = signal<number | null>(null);

  constructor() {
    // Preselecciona el primer tamaño en cuanto llega el catálogo (input, no @Input: no hay
    // ngOnChanges — un effect es el reemplazo correcto para reaccionar a un input signal).
    effect(() => {
      const primero = this.tamanosPrecios()[0];
      if (primero && untracked(this.tamanoSeleccionadoId) === null) {
        this.tamanoSeleccionadoId.set(primero.id);
      }
    });
  }

  /** Cantidad real en el carrito para `fotoId` + tamaño elegido — no un contador local. */
  protected readonly cantidad = computed<number>(() => {
    const tamanoId = this.tamanoSeleccionadoId();
    return tamanoId === null ? 0 : this.carrito.cantidadDe(this.fotoId(), tamanoId);
  });

  protected sumar(): void {
    const tamanoId = this.tamanoSeleccionadoId();
    if (tamanoId !== null) {
      this.carrito.agregar(this.fotoId(), tamanoId, 1);
    }
  }

  protected restar(): void {
    const tamanoId = this.tamanoSeleccionadoId();
    if (tamanoId !== null) {
      this.carrito.actualizarCantidad(this.fotoId(), tamanoId, this.cantidad() - 1);
    }
  }
}
