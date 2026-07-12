import { ChangeDetectionStrategy, Component, inject, input, signal } from '@angular/core';
import { ControlValueAccessor, NgControl } from '@angular/forms';

/**
 * Input compacto para **edición inline en celdas de tabla** (`ControlValueAccessor`).
 *
 * A diferencia de `tbi-text-field` (que envuelve un `mat-form-field` con su propio padding/altura
 * y **agranda la fila**), éste es un input nativo minimalista pensado para no alterar el alto de la
 * fila: edición sutil, borde discreto que resalta sólo al enfocar. Se usa con reactive forms
 * (`[formControl]`), igual que el resto de los wrappers `tbi-*`.
 *
 * El valor se maneja como **string** (no usa el `NumberValueAccessor` de Angular), así que
 * `inputMode="numeric"` ofrece teclado numérico **sin** convertir el valor a `number` — útil para
 * parámetros enteros que la API igual recibe como texto. Reutilizable en cualquier grilla con
 * edición inline.
 */
@Component({
  selector: 'tbi-cell-input',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <input
      class="tbi-cell-input__field"
      type="text"
      [attr.inputmode]="inputMode()"
      [value]="value()"
      [disabled]="disabled()"
      [attr.aria-label]="ariaLabel()"
      (input)="handleInput($event)"
      (blur)="onTouched()"
    />
  `,
  styles: `
    :host {
      display: inline-block;
      width: 100%;
    }
    .tbi-cell-input__field {
      width: 100%;
      box-sizing: border-box;
      padding: 0.25rem 0.5rem;
      font: inherit;
      color: var(--mat-sys-on-surface);
      background: var(--mat-sys-surface);
      border: 1px solid var(--mat-sys-outline-variant);
      border-radius: 4px;
      outline: none;
    }
    .tbi-cell-input__field:focus {
      border-color: var(--mat-sys-primary);
    }
    .tbi-cell-input__field:disabled {
      opacity: 0.6;
      cursor: not-allowed;
    }
  `,
})
export class TbiCellInputComponent implements ControlValueAccessor {
  readonly ariaLabel = input<string>('Valor');
  /** Modo de teclado del input. `numeric`/`decimal` para números (el valor sigue siendo string). */
  readonly inputMode = input<'text' | 'numeric' | 'decimal'>('text');

  readonly value = signal('');
  readonly disabled = signal(false);

  // self+optional + asignación manual de valueAccessor: evita el ciclo de DI que provoca proveer
  // NG_VALUE_ACCESSOR e inyectar NgControl a la vez (mismo patrón que `tbi-text-field`/`tbi-select`).
  private readonly ngControl = inject(NgControl, { self: true, optional: true });

  private onChange: (value: string) => void = () => undefined;
  protected onTouched: () => void = () => undefined;

  constructor() {
    if (this.ngControl) {
      this.ngControl.valueAccessor = this;
    }
  }

  handleInput(event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    this.value.set(value);
    this.onChange(value);
  }

  writeValue(value: string): void {
    this.value.set(value ?? '');
  }

  registerOnChange(fn: (value: string) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this.disabled.set(isDisabled);
  }
}
