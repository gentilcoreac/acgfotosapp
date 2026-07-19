import { ChangeDetectionStrategy, Component, computed, effect, inject, input, signal, untracked } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { CarritoStore, TamanoPrecio } from '../../../../core/familia';
import { TbiSelectComponent, toSelectOptions } from '../../../../shared/ui/tbi-select/tbi-select.component';

/**
 * Selector de tamaño + cantidad para UNA foto (Fase 2). Recibe el catálogo ya cargado (no hace su
 * propio fetch) para poder usarse dos veces en la misma pantalla — grilla (dentro de un
 * `MatBottomSheet`) y preview ampliado — sin duplicar la llamada de red. Habla directo con
 * `CarritoStore` (mismo criterio que `tbi-foto-familia-img`: los componentes de familia no hacen
 * prop-drilling de eventos para algo que ya vive en un store de `core/familia`).
 *
 * Un desplegable (`tbi-select`) + un solo stepper, agrupados con poco espacio entre sí (feedback de
 * Alberto 2026-07-19: NI el toggle-group horizontal de la 1ª versión NI una fila-por-tamaño de la 2ª
 * sirvieron — la primera separaba tamaño/cantidad en extremos opuestos, "ir de un lado de la pantalla
 * al otro"; la segunda es inviable con muchos tamaños, demasiadas filas en el preview ampliado). El
 * tamaño arranca preseleccionado en el primero del catálogo (default) — si la familia quiere otro,
 * lo elige manualmente del desplegable.
 *
 * El contador ES la cantidad real en el carrito para `fotoId` + tamaño elegido (arranca en 0, no en
 * 1): +/- escriben directo al store, sin un botón "Agregar" aparte que confirmar. Esto también evita
 * el bug de arrastrar un número "fantasma" al cambiar de foto en el carrusel — como el número sale
 * del store por `fotoId`, cambia solo al cambiar el input.
 */
@Component({
  selector: 'tbi-agregar-carrito',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatButtonModule, MatIconModule, TbiSelectComponent],
  template: `
    @if (tamanosPrecios().length === 0) {
      <p class="agregar-carrito__hint">Todavía no hay tamaños disponibles para pedir.</p>
    } @else {
      <div class="agregar-carrito__fila">
        @if (resumenEnCarrito().length > 0) {
          <div class="agregar-carrito__resumen">
            <span class="agregar-carrito__resumen-label">Ya en tu carrito:</span>
            @for (item of resumenEnCarrito(); track item.nombre) {
              <span class="agregar-carrito__resumen-item">{{ item.nombre }} ×{{ item.cantidad }}</span>
            }
          </div>
        }

        <div class="agregar-carrito__selector">
          <tbi-select
            class="agregar-carrito__tamano"
            label="Tamaño"
            [options]="opciones()"
            [(value)]="tamanoSeleccionadoId"
          />

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

    // Resumen + selector en la MISMA fila — pedido de Alberto 2026-07-19. Grid de 3 columnas
    // (1fr / auto / 1fr) en vez de flex: así el selector (columna del medio) queda SIEMPRE
    // centrado y fijo, sin importar si el resumen está o no — con flex + space-between el selector
    // "saltaba" de la izquierda (sin resumen) a la derecha (con resumen); acá no se mueve nunca.
    .agregar-carrito__fila {
      display: grid;
      grid-template-columns: 1fr auto 1fr;
      align-items: center;
      gap: 0.5rem 0.75rem;
    }

    .agregar-carrito__resumen {
      grid-column: 1;
      display: flex;
      flex-wrap: wrap;
      align-items: center;
      justify-self: start;
      gap: 0.25rem;
      font-size: 0.8125rem;
    }

    .agregar-carrito__resumen-label {
      color: var(--mat-sys-on-surface-variant);
    }

    // Chip sutil por tamaño — pedido de Alberto 2026-07-19: separar "10x15 ×2" de "20x30 ×1" con un
    // poco de color, nada llamativo (mismo tinte que el chip "Grande/Chico" activo del header).
    .agregar-carrito__resumen-item {
      padding: 0.0625rem 0.375rem;
      border-radius: 6px;
      background: color-mix(in srgb, var(--mat-sys-primary) 12%, transparent);
      color: var(--mat-sys-on-surface-variant);
    }

    .agregar-carrito__selector {
      grid-column: 2;
      display: flex;
      align-items: center;
      justify-self: center;
      gap: 0.75rem;
    }

    .agregar-carrito__tamano {
      flex: none;
      width: 8.5rem;
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

  protected readonly opciones = computed(() => toSelectOptions(this.tamanosPrecios(), (t) => t.nombre));

  /** "Ya en tu carrito": TODOS los tamaños con copias de esta foto, no solo el elegido en el
   * desplegable — cada uno se pinta como un chip separado (ver template). Solo lectura, cruza el
   * mismo `CarritoStore` (no es un segundo estado a mantener sincronizado, no "duplica" el carrito). */
  protected readonly resumenEnCarrito = computed<{ nombre: string; cantidad: number }[]>(() => {
    const fotoId = this.fotoId();
    return this.tamanosPrecios()
      .map((t) => ({ nombre: t.nombre, cantidad: this.carrito.cantidadDe(fotoId, t.id) }))
      .filter((t) => t.cantidad > 0);
  });

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
