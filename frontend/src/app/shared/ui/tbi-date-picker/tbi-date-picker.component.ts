import { ChangeDetectionStrategy, Component, computed, input, model, output } from '@angular/core';
import { FormValueControl, ValidationError } from '@angular/forms/signals';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { provideNativeDateAdapter } from '@angular/material/core';

/** `Date` → `yyyy-mm-dd` (lo que se manda como filtro a la API; sin huso para no correr el día). */
function toIso(date: Date | null): string {
  if (!date) {
    return '';
  }
  const y = date.getFullYear();
  const m = String(date.getMonth() + 1).padStart(2, '0');
  const d = String(date.getDate()).padStart(2, '0');
  return `${y}-${m}-${d}`;
}

/** `yyyy-mm-dd` (o ISO con hora) → `Date` local; vacío/ inválido → null. */
function fromIso(value: string | null | undefined): Date | null {
  if (!value) {
    return null;
  }
  const [datePart] = value.split('T');
  const [y, m, d] = datePart.split('-').map(Number);
  if (!y || !m || !d) {
    return null;
  }
  return new Date(y, m - 1, d);
}

/**
 * Selector de fecha del design system: `FormValueControl` nativo de Signal Forms sobre
 * `mat-datepicker`. Se usa con `[formField]`; el valor del field es un **string `yyyy-mm-dd`**
 * (cómodo como filtro de query), no un `Date`. El wrapper `tbi-*` aísla Material y el `DateAdapter`
 * nativo.
 *
 * Error propio (no `mat-error`): igual que `tbi-text-field`, el `matInput` no tiene `ngControl`
 * propio → se renderiza el mensaje acá.
 */
@Component({
  selector: 'tbi-date-picker',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [provideNativeDateAdapter()],
  imports: [MatFormFieldModule, MatInputModule, MatDatepickerModule],
  template: `
    <mat-form-field appearance="outline" subscriptSizing="dynamic">
      <mat-label>{{ label() }}</mat-label>
      <input
        matInput
        [matDatepicker]="picker"
        [value]="dateValue()"
        [min]="minDate()"
        [max]="maxDate()"
        [disabled]="disabled()"
        [required]="required()"
        [attr.aria-invalid]="hasError()"
        (dateChange)="onDateChange($event.value)"
        (blur)="touch.emit()"
      />
      <mat-datepicker-toggle matIconSuffix [for]="picker" />
      <mat-datepicker #picker />
    </mat-form-field>
    @if (hasError()) {
      <p class="tbi-date-picker__error" role="alert">{{ errorText() }}</p>
    }
  `,
  styles: `
    :host {
      display: block;
    }
    mat-form-field {
      width: 100%;
    }
    .tbi-date-picker__error {
      margin: 0.25rem 1rem 0;
      color: var(--mat-sys-error);
      font: var(--mat-sys-body-small);
    }
  `,
})
export class TbiDatePickerComponent implements FormValueControl<string> {
  readonly label = input<string>('');
  readonly errorMessage = input<string>('Fecha inválida');

  readonly value = model<string>('');
  readonly disabled = input<boolean>(false);
  readonly required = input<boolean>(false);
  /** Cotas opcionales del calendario (`yyyy-mm-dd`, mismo formato que `value`). Tipadas
   * `string | undefined` porque `FormValueControl` ya declara `min`/`max` con ese shape. */
  readonly min = input<string | undefined>(undefined);
  readonly max = input<string | undefined>(undefined);
  readonly touched = input<boolean>(false);
  readonly dirty = input<boolean>(false);
  readonly errors = input<readonly ValidationError.WithOptionalFieldTree[]>([]);

  readonly touch = output<void>();

  protected readonly dateValue = computed(() => fromIso(this.value()));
  protected readonly minDate = computed(() => fromIso(this.min()));
  protected readonly maxDate = computed(() => fromIso(this.max()));

  /** `true` si el field tiene errores y el usuario ya lo tocó o modificó (incluye el submit). */
  protected hasError(): boolean {
    return this.errors().length > 0 && (this.touched() || this.dirty());
  }

  /** Mensaje del validador que falla ahora mismo (schema); `errorMessage` es solo el fallback. */
  protected errorText(): string {
    return this.errors()[0]?.message ?? this.errorMessage();
  }

  protected onDateChange(date: Date | null): void {
    this.value.set(toIso(date));
  }
}
