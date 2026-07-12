import { ChangeDetectionStrategy, Component, input, model, output } from '@angular/core';
import { FormValueControl, ValidationError } from '@angular/forms/signals';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';

/**
 * Campo de texto multilínea del design system: mismo contrato `FormValueControl` que
 * `tbi-text-field`, con `<textarea>` en vez de `<input>`. Pensado para expresiones DAX largas
 * (`Measure.expression`) — el legacy usaba un textarea de 4 líneas para ese campo.
 */
@Component({
  selector: 'tbi-textarea',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatFormFieldModule, MatInputModule],
  template: `
    <mat-form-field appearance="outline" subscriptSizing="dynamic">
      <mat-label>{{ label() }}</mat-label>
      <textarea
        matInput
        [rows]="rows()"
        [value]="value() ?? ''"
        [disabled]="disabled()"
        [required]="required()"
        [attr.aria-invalid]="hasError()"
        (input)="handleInput($event)"
        (blur)="touch.emit()"
      ></textarea>
    </mat-form-field>
    @if (hasError()) {
      <p class="tbi-textarea__error" role="alert">{{ errorText() }}</p>
    }
  `,
  styles: `
    :host {
      display: block;
    }
    mat-form-field {
      width: 100%;
    }
    .tbi-textarea__error {
      margin: 0.25rem 1rem 0;
      color: var(--mat-sys-error);
      font: var(--mat-sys-body-small);
    }
  `,
})
export class TbiTextareaComponent implements FormValueControl<string | null> {
  readonly label = input<string>('');
  readonly rows = input<number>(4);
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
    this.value.set((event.target as HTMLTextAreaElement).value);
  }
}
