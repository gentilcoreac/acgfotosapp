import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

/** Una serie del gráfico: nombre, valores por categoría y tono de color. */
export interface TbiBarSeries {
  name: string;
  values: number[];
  /** Color M3 de las barras: primary (default) o tertiary. */
  tone?: 'primary' | 'tertiary';
}

/**
 * Gráfico de barras agrupadas, CSS puro (sin librería). Pensado para series chicas como la
 * actividad mensual del homepage. Cada categoría (`categories[i]`) muestra una barra por serie,
 * con altura proporcional al máximo global. Presentacional y reusable.
 */
@Component({
  selector: 'tbi-bar-chart',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="plot" role="img" [attr.aria-label]="ariaLabel()">
      @for (cat of categories(); track $index; let i = $index) {
        <div class="group">
          <div class="bars">
            @for (s of series(); track s.name) {
              <div
                class="bar"
                [class.bar--tertiary]="s.tone === 'tertiary'"
                [style.height.%]="pct(s.values[i])"
                [title]="cat + ' · ' + s.name + ': ' + s.values[i]"
              >
                @if (s.values[i] > 0) {
                  <span class="bar__value">{{ s.values[i] }}</span>
                }
              </div>
            }
          </div>
          <span class="label">{{ cat }}</span>
        </div>
      }
    </div>

    <div class="legend">
      @for (s of series(); track s.name) {
        <span class="legend__item">
          <span
            class="legend__swatch"
            [class.legend__swatch--tertiary]="s.tone === 'tertiary'"
          ></span>
          {{ s.name }}
        </span>
      }
    </div>
  `,
  styles: `
    :host {
      display: block;
    }

    .plot {
      display: flex;
      align-items: flex-end;
      gap: 8px;
      height: 180px;
      // Espacio arriba para que la etiqueta de valor de la barra más alta no se corte.
      padding-top: 20px;
    }

    .group {
      flex: 1 1 0;
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 6px;
      min-width: 0;
      height: 100%;
    }

    .bars {
      flex: 1;
      display: flex;
      align-items: flex-end;
      justify-content: center;
      gap: 3px;
      width: 100%;
    }

    .bar {
      position: relative;
      width: 14px;
      max-width: 40%;
      min-height: 2px;
      border-radius: 6px 6px 2px 2px;
      background: var(--mat-sys-primary);
      transition: height 0.2s ease;
    }

    .bar--tertiary {
      background: var(--mat-sys-tertiary);
    }

    // Valor encima de la barra (sólo cuando es > 0; ver template).
    .bar__value {
      position: absolute;
      top: -16px;
      left: 50%;
      transform: translateX(-50%);
      font: var(--mat-sys-label-small);
      font-weight: 600;
      color: var(--mat-sys-on-surface);
      white-space: nowrap;
    }

    .label {
      font: var(--mat-sys-label-small);
      color: var(--mat-sys-on-surface-variant);
      white-space: nowrap;
    }

    .legend {
      display: flex;
      gap: 16px;
      margin-top: 14px;
    }

    .legend__item {
      display: inline-flex;
      align-items: center;
      gap: 6px;
      font: var(--mat-sys-body-small);
      color: var(--mat-sys-on-surface-variant);
    }

    .legend__swatch {
      width: 12px;
      height: 12px;
      border-radius: 4px;
      background: var(--mat-sys-primary);
    }

    .legend__swatch--tertiary {
      background: var(--mat-sys-tertiary);
    }
  `,
})
export class TbiBarChartComponent {
  readonly categories = input.required<string[]>();
  readonly series = input.required<TbiBarSeries[]>();

  /** Máximo global de todas las series, para escalar las alturas (mínimo 1 para no dividir por 0). */
  private readonly max = computed(() => Math.max(1, ...this.series().flatMap((s) => s.values)));

  protected readonly ariaLabel = computed(() =>
    this.series()
      .map((s) => `${s.name}: ${s.values.join(', ')}`)
      .join('. '),
  );

  protected pct(value: number): number {
    return (value / this.max()) * 100;
  }
}
