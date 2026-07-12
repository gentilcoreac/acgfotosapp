import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  computed,
  inject,
  signal,
} from '@angular/core';
import {
  AbstractControl,
  FormControl,
  FormGroup,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { TbiButtonComponent } from '../../../shared/ui/tbi-button/tbi-button.component';
import { TbiStatusChipComponent } from '../../../shared/ui/tbi-status-chip/tbi-status-chip.component';
import { TbiTextFieldComponent } from '../../../shared/ui/tbi-text-field/tbi-text-field.component';
import { NotificationService } from '../../../shared/feedback/notification.service';
import { ProfileService } from '../data/profile.service';
import { Perfil } from '../domain/profile.model';

/** Las dos contraseñas nuevas deben coincidir (validador de grupo). */
function passwordsMatch(group: AbstractControl): Record<string, boolean> | null {
  const nueva = group.get('newPassword')?.value;
  const confirma = group.get('newConfirmPassword')?.value;
  return nueva && confirma && nueva !== confirma ? { mismatch: true } : null;
}

/**
 * Pantalla "Mi perfil" (self-service). Muestra los datos propios, permite editar nombre/apellido/
 * teléfono y cambiar la contraseña. `userName` y `email` son read-only (no viajan en el update);
 * el avatar usa la inicial (como el resto del cliente) y `profilePicture` se conserva en el round-trip.
 */
@Component({
  selector: 'tbi-profile',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    ReactiveFormsModule,
    MatCardModule,
    MatIconModule,
    MatProgressSpinnerModule,
    TbiTextFieldComponent,
    TbiButtonComponent,
    TbiStatusChipComponent,
  ],
  templateUrl: './profile.component.html',
  styleUrl: './profile.component.scss',
})
export class ProfileComponent implements OnInit {
  private readonly service = inject(ProfileService);
  private readonly notify = inject(NotificationService);

  protected readonly perfil = signal<Perfil | null>(null);
  protected readonly loading = signal(true);
  protected readonly saving = signal(false);
  protected readonly changingPassword = signal(false);

  protected readonly inicial = computed(() => {
    const p = this.perfil();
    const base = p?.nombre || p?.userName || '?';
    return base.charAt(0).toUpperCase();
  });

  /** Nombre completo para el encabezado; cae al userName si no hay nombre/apellido cargados. */
  protected readonly nombreCompleto = computed(() => {
    const p = this.perfil();
    if (!p) {
      return '';
    }
    const full = `${p.nombre ?? ''} ${p.apellido ?? ''}`.trim();
    return full || p.userName;
  });

  protected readonly datosForm = new FormGroup({
    nombre: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    apellido: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    telefono: new FormControl<string>('', { nonNullable: true }),
  });

  protected readonly passwordForm = new FormGroup(
    {
      currentPassword: new FormControl('', {
        nonNullable: true,
        validators: [Validators.required],
      }),
      newPassword: new FormControl('', {
        nonNullable: true,
        validators: [Validators.required, Validators.minLength(6)],
      }),
      newConfirmPassword: new FormControl('', {
        nonNullable: true,
        validators: [Validators.required],
      }),
    },
    { validators: passwordsMatch },
  );

  ngOnInit(): void {
    this.service.getPerfil().subscribe({
      next: (perfil) => {
        this.perfil.set(perfil);
        this.datosForm.patchValue({
          nombre: perfil.nombre ?? '',
          apellido: perfil.apellido ?? '',
          telefono: perfil.telefono != null ? String(perfil.telefono) : '',
        });
        this.loading.set(false);
      },
      // El errorInterceptor ya notifica; solo dejamos de mostrar el spinner de carga.
      error: () => this.loading.set(false),
    });
  }

  protected save(): void {
    if (this.datosForm.invalid) {
      this.datosForm.markAllAsTouched();
      return;
    }
    const actual = this.perfil();
    if (!actual) {
      return;
    }
    const { nombre, apellido, telefono } = this.datosForm.getRawValue();
    const payload: Perfil = {
      ...actual,
      nombre,
      apellido,
      telefono: telefono ? Number(telefono) : null,
    };
    this.saving.set(true);
    this.service.updateProfile(payload).subscribe({
      next: () => {
        this.perfil.set(payload);
        this.saving.set(false);
        this.notify.success('Perfil actualizado.');
      },
      error: () => this.saving.set(false),
    });
  }

  protected changeMyPassword(): void {
    if (this.passwordForm.invalid) {
      this.passwordForm.markAllAsTouched();
      return;
    }
    this.changingPassword.set(true);
    this.service.changePassword(this.passwordForm.getRawValue()).subscribe({
      next: () => {
        this.changingPassword.set(false);
        this.passwordForm.reset();
        this.notify.success('Contraseña actualizada.');
      },
      error: () => this.changingPassword.set(false),
    });
  }
}
