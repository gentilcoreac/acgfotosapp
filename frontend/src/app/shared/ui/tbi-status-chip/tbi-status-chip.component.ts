import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';

/** Tono semántico del chip de estado. Define el par container / on-container que pinta el chip. */
export type TbiChipTone = 'success' | 'error' | 'neutral' | 'warning';

/**
 * Chip de estado del design system (M3 Expressive). Píldora compacta de 26px con ícono + label,
 * para representar estados de fila (Activo/Bloqueado, etc.). El color sale del `tone`; el tono
 * `success` usa el token custom `--tbi-sys-success-container` (ver `_theme.scss`, no hay rol
 * success en M3 oficial). Lo consume `tbi-table` vía la propiedad `chip` de `TbiColumn`, pero es
 * autónomo y reusable en cualquier plantilla.
 */
@Component({
  selector: 'tbi-status-chip',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatIconModule],
  template: `
    <span class="tbi-status-chip" data-testid="status-chip" [class]="'tbi-status-chip--' + tone()">
      @if (icon(); as ic) {
        <mat-icon class="tbi-status-chip__icon">{{ ic }}</mat-icon>
      }
      {{ label() }}
    </span>
  `,
  styles: `
    .tbi-status-chip {
      display: inline-flex;
      align-items: center;
      gap: 5px;
      height: 26px;
      padding: 0 11px 0 8px;
      border-radius: 8px;
      font: var(--mat-sys-label-large);
      font-weight: 600;
      white-space: nowrap;
    }

    .tbi-status-chip__icon {
      width: 15px;
      height: 15px;
      font-size: 15px;
      line-height: 1;
    }

    // Sin ícono: el padding izquierdo vuelve a 11px para centrar el label.
    .tbi-status-chip:not(:has(.tbi-status-chip__icon)) {
      padding-left: 11px;
    }

    .tbi-status-chip--success {
      background: var(--tbi-sys-success-container);
      color: var(--tbi-sys-on-success-container);
    }

    .tbi-status-chip--error {
      background: var(--mat-sys-error-container);
      color: var(--mat-sys-on-error-container);
    }

    .tbi-status-chip--warning {
      background: var(--mat-sys-tertiary-container);
      color: var(--mat-sys-on-tertiary-container);
    }

    .tbi-status-chip--neutral {
      background: var(--mat-sys-surface-container-highest);
      color: var(--mat-sys-on-surface-variant);
    }
  `,
})
export class TbiStatusChipComponent {
  readonly label = input.required<string>();
  readonly tone = input<TbiChipTone>('neutral');
  /** Ícono de Material Symbols opcional a la izquierda del label. */
  readonly icon = input<string | null>(null);
}
