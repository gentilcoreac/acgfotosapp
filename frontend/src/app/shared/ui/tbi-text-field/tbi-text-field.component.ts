import { ChangeDetectionStrategy, Component, input, model, output } from '@angular/core';
import { FormValueControl, ValidationError } from '@angular/forms/signals';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';

/**
 * Campo de texto del design system: `FormValueControl` nativo de Signal Forms sobre `mat-form-field`
 * + `matInput`. Se usa con `[formField]` (encapsula Material; el wrapper `tbi-*` aísla el UI kit).
 *
 * **Estado de error:** el `matInput` interno no tiene `ngControl` propio, así que `MatInput` nunca
 * recalcula su `errorState` (`errorStateMatcher` sería código muerto) — por eso el error se renderiza
 * acá (`hasError()` lee los inputs `errors`/`touched`/`dirty` que el directive `[formField]`
 * sincroniza) en vez de delegarlo a `mat-error`.
 *
 * OnPush: `errors`/`disabled`/`required`/`touched`/`dirty` son `input()` (señales) sincronizados
 * directamente por `[formField]`, no un `AbstractControl` mutable externo — CD se dispara solo con
 * cada cambio, sin necesitar `Eager`.
 */
@Component({
  selector: 'tbi-text-field',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatFormFieldModule, MatInputModule],
  template: `
    <mat-form-field appearance="outline" subscriptSizing="dynamic">
      <mat-label>{{ label() }}</mat-label>
      <input
        matInput
        [type]="type()"
        [value]="value() ?? ''"
        [disabled]="disabled()"
        [required]="required()"
        [autocomplete]="autocomplete()"
        [attr.inputmode]="inputmode()"
        [attr.aria-invalid]="hasError()"
        (input)="handleInput($event)"
        (blur)="touch.emit()"
      />
    </mat-form-field>
    @if (hasError()) {
      <p class="tbi-text-field__error" role="alert">{{ errorText() }}</p>
    }
  `,
  styles: `
    :host {
      display: block;
    }
    mat-form-field {
      width: 100%;
    }
    .tbi-text-field__error {
      margin: 0.25rem 1rem 0;
      color: var(--mat-sys-error);
      font: var(--mat-sys-body-small);
    }
  `,
})
export class TbiTextFieldComponent implements FormValueControl<string | null> {
  readonly label = input<string>('');
  readonly type = input<'text' | 'password' | 'email' | 'number'>('text');
  readonly autocomplete = input<string>('off');
  /** Teclado virtual sugerido (p. ej. `numeric` para teléfonos con `type="text"`). */
  readonly inputmode = input<'text' | 'numeric' | 'decimal' | 'tel' | 'email' | 'url' | null>(null);
  readonly errorMessage = input<string>('Campo inválido');

  readonly value = model<string | null>('');
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

  handleInput(event: Event): void {
    this.value.set((event.target as HTMLInputElement).value);
  }
}
