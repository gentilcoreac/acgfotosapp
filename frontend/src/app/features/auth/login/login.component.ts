import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { AuthService } from '../../../core/auth';
import { AppConfigService } from '../../../core/config';
import { ApiError, errorMessages } from '../../../core/models';
import { TenantStyleStore } from '../../../core/tenant-style';
import { ThemeStore } from '../../../core/theme';
import { TbiButtonComponent } from '../../../shared/ui/tbi-button/tbi-button.component';
import { TbiTextFieldComponent } from '../../../shared/ui/tbi-text-field/tbi-text-field.component';

// TODO (Fase 4 - i18n): reemplazar los textos en español por LocalizePipe + CultureService.
@Component({
  selector: 'tbi-login',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, RouterLink, TbiTextFieldComponent, TbiButtonComponent],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss',
})
export class LoginComponent {
  private readonly fb = inject(NonNullableFormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly config = inject(AppConfigService).config();
  private readonly tenantStyle = inject(TenantStyleStore);
  private readonly theme = inject(ThemeStore);

  /** Marca y datos de pie, de la config runtime. */
  readonly appTitle = this.config.appTitle;
  readonly version = this.config.version;
  readonly year = this.config.year;

  /** Branding del login del tenant resuelto en el bootstrap (por dominio/`?tenant=`). */
  readonly layout = computed(() => this.tenantStyle.style()?.tipoLayoutLogin ?? 0);
  readonly isSplit = computed(() => this.layout() === 1 || this.layout() === 2);

  /** Logo de login según el modo (o null → cae al brand de texto). */
  readonly loginLogoUrl = computed<string | null>(() => {
    const style = this.tenantStyle.style();
    if (!style) {
      return null;
    }
    return (this.theme.isDark() ? style.logoLoginDarkUrl : style.logoLoginLightUrl) ?? null;
  });

  /** Imagen de fondo del login según el modo. */
  readonly backgroundUrl = computed<string | null>(() => {
    const style = this.tenantStyle.style();
    if (!style) {
      return null;
    }
    return (
      (this.theme.isDark() ? style.imagenFondoLoginDarkUrl : style.imagenFondoLoginLightUrl) ?? null
    );
  });

  /** Fondo full-bleed solo en el layout centrado (en split la imagen va en su propio pane). */
  readonly centeredBg = computed<string | null>(() => {
    const url = this.backgroundUrl();
    return !this.isSplit() && url ? `url("${url}")` : null;
  });

  readonly loading = signal(false);
  readonly errors = signal<string[]>([]);

  readonly form = this.fb.group({
    userName: ['', [Validators.required]],
    password: ['', [Validators.required]],
  });

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.loading.set(true);
    this.errors.set([]);
    this.auth
      .login(this.form.getRawValue())
      // finalize apaga el spinner siempre (éxito o error). Antes loading.set(false) estaba solo
      // en el handler de error, así que tras un login OK el spinner podía quedar colgado.
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: () => {
          void this.router.navigateByUrl('/');
        },
        error: (error: ApiError) => {
          this.errors.set(errorMessages(error, ['ErrorUnexpected']));
        },
      });
  }
}
