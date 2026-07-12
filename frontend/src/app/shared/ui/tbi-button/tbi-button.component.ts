import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

/**
 * Botón del design system, sobre Angular Material (`matButton="filled"`), con estado de
 * carga (spinner). `type="submit"` dispara el submit del `<form>` contenedor.
 * El wrapper `tbi-*` aísla Material: la API pública no expone tipos de Material.
 */
@Component({
  selector: 'tbi-button',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatButtonModule, MatProgressSpinnerModule],
  template: `
    <button matButton="filled" [type]="type()" [disabled]="disabled() || loading()">
      <span class="tbi-button__content">
        @if (loading()) {
          <mat-progress-spinner
            class="tbi-button__spinner"
            data-testid="button-spinner"
            mode="indeterminate"
            [diameter]="18"
          />
        }
        {{ loading() ? loadingText() : label() }}
      </span>
    </button>
  `,
  styleUrl: './tbi-button.component.scss',
})
export class TbiButtonComponent {
  readonly label = input.required<string>();
  readonly loadingText = input<string>('Procesando…');
  readonly loading = input<boolean>(false);
  readonly disabled = input<boolean>(false);
  readonly type = input<'button' | 'submit'>('button');
}
