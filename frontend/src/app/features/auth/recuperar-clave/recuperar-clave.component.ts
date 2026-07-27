import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import {
  AbstractControl,
  NonNullableFormBuilder,
  ReactiveFormsModule,
  ValidationErrors,
  Validators,
} from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService, ResetPassword } from '../../../core/auth';
import { ApiError, errorMessages } from '../../../core/models';
import { NotificationService } from '../../../shared/feedback/notification.service';
import { TbiButtonComponent } from '../../../shared/ui/tbi-button/tbi-button.component';
import { TbiTextFieldComponent } from '../../../shared/ui/tbi-text-field/tbi-text-field.component';

// TODO (Fase 4 - i18n): textos en español por ahora.
@Component({
  selector: 'tbi-recuperar-clave',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, TbiTextFieldComponent, TbiButtonComponent],
  templateUrl: './recuperar-clave.component.html',
  styleUrl: './recuperar-clave.component.scss',
})
export class RecuperarClaveComponent {
  private readonly fb = inject(NonNullableFormBuilder);
  private readonly auth = inject(AuthService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly notify = inject(NotificationService);

  protected readonly loading = signal(false);
  protected readonly errors = signal<string[]>([]);
  /** Parámetros del link del mail; la API resuelve por `userName` (acepta usuario o email), no por `userId`. */
  private readonly params = this.route.snapshot.queryParamMap;
  protected readonly linkInvalido = !this.params.get('userName') || !this.params.get('code');

  // Misma política que confirmar-cuenta (IdentityHelper): 12+ caracteres, mayúscula, minúscula,
  // dígito y símbolo. La API además exige 4 chars únicos; ese caso lo reporta el backend en errors().
  protected readonly form = this.fb.group(
    {
      password: [
        '',
        [
          Validators.required,
          Validators.minLength(12),
          Validators.pattern(/^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9]).+$/),
        ],
      ],
      confirmPassword: ['', [Validators.required]],
    },
    { validators: RecuperarClaveComponent.passwordsMatch },
  );

  /** Valida que ambas contraseñas coincidan (error a nivel grupo). */
  private static passwordsMatch(group: AbstractControl): ValidationErrors | null {
    const password = group.get('password')?.value;
    const confirm = group.get('confirmPassword')?.value;
    return password === confirm ? null : { mismatch: true };
  }

  protected submit(): void {
    if (this.linkInvalido) {
      this.notify.error('El enlace no es válido o está incompleto.');
      return;
    }
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading.set(true);
    this.errors.set([]);
    // `code` llega URL-encodeado por la API; Angular lo decodifica una vez al leer el query param.
    const model: ResetPassword = {
      emailOrUsername: this.params.get('userName') ?? '',
      code: this.params.get('code') ?? '',
      password: this.form.controls.password.value,
      confirmPassword: this.form.controls.confirmPassword.value,
    };

    this.auth.resetearPassword(model).subscribe({
      next: (mensaje) => {
        this.loading.set(false);
        this.notify.success(mensaje || 'Contraseña actualizada. Ya podés iniciar sesión.');
        void this.router.navigateByUrl('/login');
      },
      error: (error: ApiError) => {
        this.loading.set(false);
        this.errors.set(errorMessages(error, ['No se pudo restablecer la contraseña.']));
      },
    });
  }
}
