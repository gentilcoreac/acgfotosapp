import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../../core/auth';
import { ApiError, errorMessages } from '../../../core/models';
import { TbiButtonComponent } from '../../../shared/ui/tbi-button/tbi-button.component';
import { TbiTextFieldComponent } from '../../../shared/ui/tbi-text-field/tbi-text-field.component';

// TODO (Fase 4 - i18n): textos en español por ahora.
@Component({
  selector: 'tbi-olvide-password',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, RouterLink, TbiTextFieldComponent, TbiButtonComponent],
  templateUrl: './olvide-password.component.html',
  styleUrl: './olvide-password.component.scss',
})
export class OlvidePasswordComponent {
  private readonly fb = inject(NonNullableFormBuilder);
  private readonly auth = inject(AuthService);

  protected readonly loading = signal(false);
  protected readonly errors = signal<string[]>([]);
  protected readonly enviado = signal(false);

  protected readonly form = this.fb.group({
    emailOrUsername: ['', [Validators.required]],
  });

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading.set(true);
    this.errors.set([]);
    this.auth.olvidePassword(this.form.getRawValue()).subscribe({
      next: () => {
        this.loading.set(false);
        this.enviado.set(true);
      },
      error: (error: ApiError) => {
        this.loading.set(false);
        this.errors.set(errorMessages(error, ['No se pudo enviar el correo de recuperación.']));
      },
    });
  }
}
