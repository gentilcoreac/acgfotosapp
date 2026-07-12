import { ChangeDetectionStrategy, Component, input, model, output } from '@angular/core';
import { FormValueControl, ValidationError } from '@angular/forms/signals';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';

/** Opción de `tbi-select`. */
export interface TbiSelectOption<V> {
  value: V;
  label: string;
}

/** Grupo de opciones (`mat-optgroup`) — p.ej. períodos de tiempo agrupados por año. */
export interface TbiSelectOptionGroup<V> {
  label: string;
  options: TbiSelectOption<V>[];
}

/** Mapea entidades `{ id, ... }` a opciones del select (evita repetir el mismo `map` en cada ABM). */
export function toSelectOptions<T extends { id: V }, V>(
  items: readonly T[],
  label: (item: T) => string,
): TbiSelectOption<V>[] {
  return items.map((item) => ({ value: item.id, label: label(item) }));
}

/**
 * Select del design system: `FormValueControl` nativo de Signal Forms sobre `mat-form-field` +
 * `mat-select`. Se usa con `[formField]`. Aísla Material detrás del wrapper `tbi-*`.
 *
 * **Estado de error:** igual que `tbi-text-field`, el `mat-select` interno no tiene `ngControl`
 * propio → su `errorStateMatcher` sería código muerto. Por eso el error se renderiza acá (`hasError()`
 * lee los inputs `errors`/`touched`/`dirty` sincronizados por `[formField]`).
 */
@Component({
  selector: 'tbi-select',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatFormFieldModule, MatSelectModule],
  template: `
    <mat-form-field appearance="outline" subscriptSizing="dynamic">
      <mat-label>{{ label() }}</mat-label>
      <mat-select
        [value]="value()"
        [disabled]="disabled()"
        [required]="required()"
        [attr.aria-invalid]="hasError()"
        (selectionChange)="handleChange($event.value)"
        (openedChange)="$event || touch.emit()"
      >
        @if (groups(); as optionGroups) {
          @for (group of optionGroups; track group.label) {
            <mat-optgroup [label]="group.label">
              @for (option of group.options; track option.value) {
                <mat-option [value]="option.value">{{ option.label }}</mat-option>
              }
            </mat-optgroup>
          }
        } @else {
          @for (option of options(); track option.value) {
            <mat-option [value]="option.value">{{ option.label }}</mat-option>
          }
        }
      </mat-select>
    </mat-form-field>
    @if (hasError()) {
      <p class="tbi-select__error" role="alert">{{ errorText() }}</p>
    }
  `,
  styles: `
    :host {
      display: block;
    }
    mat-form-field {
      width: 100%;
    }
    .tbi-select__error {
      margin: 0.25rem 1rem 0;
      color: var(--mat-sys-error);
      font: var(--mat-sys-body-small);
    }
  `,
})
export class TbiSelectComponent<V> implements FormValueControl<V | null> {
  readonly label = input<string>('');
  /** Opciones planas. Con `groups` seteado se ignoran (por eso deja de ser `required`). */
  readonly options = input<TbiSelectOption<V>[]>([]);
  /** Opciones agrupadas (`mat-optgroup`); tiene prioridad sobre `options`. */
  readonly groups = input<TbiSelectOptionGroup<V>[] | null>(null);
  readonly errorMessage = input<string>('Campo inválido');

  readonly value = model<V | null>(null);
  readonly disabled = input<boolean>(false);
  readonly required = input<boolean>(false);
  readonly touched = input<boolean>(false);
  readonly dirty = input<boolean>(false);
  readonly errors = input<readonly ValidationError.WithOptionalFieldTree[]>([]);

  readonly touch = output<void>();

  /** `true` si el field tiene errores y el usuario ya lo tocó o modificó (incluye el submit). */
  protected hasError(): boolean {
    return this.errors().length > 0 && (this.touched() || this.dirty());
  }

  /** Mensaje del validador que falla ahora mismo (schema); `errorMessage` es solo el fallback. */
  protected errorText(): string {
    return this.errors()[0]?.message ?? this.errorMessage();
  }

  handleChange(value: V | null): void {
    this.value.set(value);
  }
}
