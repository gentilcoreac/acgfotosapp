import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import { RouterModule } from '@angular/router';

/** Tono del ícono de la KPI card: normal (primary), aviso (tertiary) o error. */
export type TbiKpiTone = 'default' | 'warning' | 'error';

/**
 * KPI card del design system (M3, handoff Admin). Tarjeta con ícono, valor grande y label, para
 * los indicadores del homepage. Si se pasa `link`, toda la tarjeta navega a esa ruta (con
 * affordance de hover); si no, es presentacional. El valor es libre (number o string como "3 / 10").
 */
@Component({
  selector: 'tbi-kpi-card',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatIconModule, RouterModule],
  template: `
    <a class="kpi" [class.kpi--link]="!!link()" [routerLink]="link() ?? null">
      <span
        class="kpi__icon"
        [class.kpi__icon--warning]="tone() === 'warning'"
        [class.kpi__icon--error]="tone() === 'error'"
      >
        <mat-icon>{{ icon() }}</mat-icon>
      </span>
      <span class="kpi__text">
        <span class="kpi__value">{{ value() }}</span>
        <span class="kpi__label">{{ label() }}</span>
      </span>
    </a>
  `,
  styles: `
    .kpi {
      display: flex;
      align-items: center;
      gap: 14px;
      padding: 18px 20px;
      border: 1px solid color-mix(in srgb, var(--mat-sys-outline-variant) 55%, transparent);
      border-radius: 22px;
      background: var(--mat-sys-surface-container-low);
      color: inherit;
      text-decoration: none;
    }

    .kpi--link {
      cursor: pointer;
      transition: border-color 0.15s ease;
    }

    .kpi--link:hover {
      border-color: color-mix(in srgb, var(--mat-sys-primary) 45%, transparent);
    }

    .kpi__icon {
      flex: none;
      display: grid;
      place-items: center;
      width: 44px;
      height: 44px;
      border-radius: 14px;
      background: var(--mat-sys-primary-container);
      color: var(--mat-sys-on-primary-container);
    }

    .kpi__icon--warning {
      background: var(--mat-sys-tertiary-container);
      color: var(--mat-sys-on-tertiary-container);
    }

    .kpi__icon--error {
      background: var(--mat-sys-error-container);
      color: var(--mat-sys-on-error-container);
    }

    .kpi__text {
      display: flex;
      flex-direction: column;
      min-width: 0;
      line-height: 1.15;
    }

    .kpi__value {
      font-size: 32px;
      font-weight: 500;
      letter-spacing: -0.5px;
      color: var(--mat-sys-on-surface);
    }

    .kpi__label {
      font: var(--mat-sys-body-medium);
      color: var(--mat-sys-on-surface-variant);
    }
  `,
})
export class TbiKpiCardComponent {
  readonly icon = input.required<string>();
  readonly label = input.required<string>();
  readonly value = input.required<string | number>();
  readonly tone = input<TbiKpiTone>('default');
  /** Ruta a la que navega la tarjeta al hacer click. Si se omite, la tarjeta es no-navegable. */
  readonly link = input<string>();
}
