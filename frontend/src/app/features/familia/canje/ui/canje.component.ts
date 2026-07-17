import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { finalize } from 'rxjs';
import { ApiError, errorMessages } from '../../../../core/models';
import { FamiliaService } from '../../../../core/familia';
import { AppConfigService } from '../../../../core/config';
import { TbiButtonComponent } from '../../../../shared/ui/tbi-button/tbi-button.component';
import { TbiTextFieldComponent } from '../../../../shared/ui/tbi-text-field/tbi-text-field.component';

/**
 * Canje de código de acceso (ADR-02): sin registro, mobile-first, fuera del layout admin. Llega
 * por dos caminos — `/canje` (form manual) y `/canje/:codigo` (link del QR de la tarjeta, que
 * precarga el código y dispara el canje solo).
 */
@Component({
  selector: 'tbi-canje',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, TbiTextFieldComponent, TbiButtonComponent],
  templateUrl: './canje.component.html',
  styleUrl: './canje.component.scss',
})
export class CanjeComponent {
  private readonly fb = inject(NonNullableFormBuilder);
  private readonly familia = inject(FamiliaService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  readonly appTitle = inject(AppConfigService).config().appTitle;

  readonly loading = signal(false);
  readonly errors = signal<string[]>([]);

  readonly form = this.fb.group({
    codigo: ['', [Validators.required]],
  });

  constructor() {
    const codigoDeLink = this.route.snapshot.paramMap.get('codigo');
    if (codigoDeLink) {
      this.form.patchValue({ codigo: codigoDeLink });
      this.submit();
    }
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.loading.set(true);
    this.errors.set([]);
    this.familia
      .canjear(this.form.getRawValue().codigo)
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: () => {
          void this.router.navigateByUrl('/mi-album');
        },
        error: (error: ApiError) => {
          this.errors.set(errorMessages(error, ['No se pudo validar el código.']));
        },
      });
  }
}
