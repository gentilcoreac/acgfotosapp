import { ChangeDetectionStrategy, Component, computed, input, model, output } from '@angular/core';
import { FormValueControl, ValidationError } from '@angular/forms/signals';

/**
 * Control numérico del design system: `FormValueControl` nativo de Signal Forms sobre
 * `input[type=range]` + lectura del valor en vivo. Pensado para parámetros de colocación (escala,
 * margen, ángulo, opacidad) donde ver el número mientras se arrastra importa más que tipearlo.
 *
 * Mismo criterio de error que `tbi-text-field`/`tbi-select`: el input nativo no tiene `ngControl`
 * propio, así que el estado de error se renderiza acá leyendo `errors`/`touched`/`dirty`.
 */
@Component({
  selector: 'tbi-slider',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="tbi-slider__label">
      <span>{{ label() }}</span>
      <span class="tbi-slider__val">{{ valorMostrado() }}</span>
    </div>
    <input
      type="range"
      [min]="minimo()"
      [max]="maximo()"
      [step]="step()"
      [value]="value() ?? 0"
      [disabled]="disabled()"
      [attr.aria-invalid]="hasError()"
      [attr.aria-label]="label()"
      (input)="handleInput($event)"
      (blur)="touch.emit()"
    />
    @if (hasError()) {
      <p class="tbi-slider__error" role="alert">{{ errorText() }}</p>
    }
  `,
  styles: `
    :host {
      display: block;
    }
    .tbi-slider__label {
      display: flex;
      justify-content: space-between;
      align-items: baseline;
      gap: 0.5rem;
      font: var(--mat-sys-body-medium);
      margin-bottom: 0.2rem;
    }
    .tbi-slider__val {
      font-variant-numeric: tabular-nums;
      color: var(--mat-sys-on-surface-variant);
      font-size: 0.85em;
    }
    input[type='range'] {
      width: 100%;
      accent-color: var(--mat-sys-primary);
      margin: 0.15rem 0;
    }
    .tbi-slider__error {
      margin: 0.25rem 0 0;
      color: var(--mat-sys-error);
      font: var(--mat-sys-body-small);
    }
  `,
})
export class TbiSliderComponent implements FormValueControl<number> {
  readonly label = input<string>('');
  readonly minimo = input<number>(0);
  readonly maximo = input<number>(100);
  readonly step = input<number>(1);
  /** Sufijo del valor mostrado (ej. `%`, `°`). */
  readonly unidad = input<string>('');
  readonly errorMessage = input<string>('Valor inválido');

  readonly value = model<number>(0);
  readonly disabled = input<boolean>(false);
  readonly required = input<boolean>(false);
  readonly touched = input<boolean>(false);
  readonly dirty = input<boolean>(false);
  readonly errors = input<readonly ValidationError.WithOptionalFieldTree[]>([]);

  readonly touch = output<void>();

  protected readonly valorMostrado = computed(() => `${this.value() ?? 0}${this.unidad()}`);

  protected hasError(): boolean {
    return this.errors().length > 0 && (this.touched() || this.dirty());
  }

  protected errorText(): string {
    return this.errors()[0]?.message ?? this.errorMessage();
  }

  handleInput(event: Event): void {
    this.value.set(Number((event.target as HTMLInputElement).value));
  }
}
