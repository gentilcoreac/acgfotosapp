import { ChangeDetectionStrategy, Component, input } from '@angular/core';

/**
 * Chip de tamaño/tipo de foto (p. ej. "10x15", "10x15 ×2"): mismo tratamiento visual en toda la
 * app (carrito de familia, resumen de `tbi-agregar-carrito`, detalle admin de pedidos) — antes
 * duplicado como CSS suelto en cada lugar, ahora un único componente (pedido de Alberto).
 */
@Component({
  selector: 'tbi-tamano-chip',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `<span class="tbi-tamano-chip">{{ label() }}</span>`,
  styles: `
    :host {
      display: inline-flex;
    }

    .tbi-tamano-chip {
      padding: 0.0625rem 0.5rem;
      border-radius: 6px;
      background: color-mix(in srgb, var(--mat-sys-primary) 12%, transparent);
      color: var(--mat-sys-on-surface-variant);
      font-size: 0.8125rem;
      font-weight: 500;
    }
  `,
})
export class TbiTamanoChipComponent {
  readonly label = input.required<string>();
}
