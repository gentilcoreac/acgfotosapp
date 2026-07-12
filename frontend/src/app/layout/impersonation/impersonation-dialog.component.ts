import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { ImpersonatableUser, ImpersonationService } from '../../core/auth';
import { TenantService } from '../../features/tenant/data/tenant.service';

interface TenantOption {
  id: number;
  nombre: string;
}

/**
 * Selector de impersonalización (ADR-0002): root elige un **tenant** y luego un **usuario** de ese
 * tenant (sus administradores) para operar como él. Al confirmar llama a `auth/impersonate` y cierra.
 */
@Component({
  selector: 'tbi-impersonation-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatDialogModule, MatFormFieldModule, MatSelectModule, MatButtonModule, MatProgressSpinnerModule],
  template: `
    <h2 mat-dialog-title>Impersonalizar</h2>
    <mat-dialog-content>
      <p class="impersonate__hint">Operá como un usuario de otro tenant para dar soporte.</p>

      <mat-form-field appearance="outline" class="impersonate__field">
        <mat-label>Tenant</mat-label>
        <mat-select [value]="selectedTenantId()" (selectionChange)="onTenantChange($event.value)">
          @for (t of tenants(); track t.id) {
            <mat-option [value]="t.id">{{ t.nombre }}</mat-option>
          }
        </mat-select>
      </mat-form-field>

      <mat-form-field appearance="outline" class="impersonate__field">
        <mat-label>Usuario</mat-label>
        <mat-select
          [value]="selectedUserId()"
          (selectionChange)="selectedUserId.set($event.value)"
          [disabled]="selectedTenantId() === null || loadingUsers()"
        >
          @for (u of users(); track u.id) {
            <mat-option [value]="u.id">{{ u.userName }} — {{ u.nombre }} {{ u.apellido }}</mat-option>
          }
        </mat-select>
        @if (loadingUsers()) {
          <mat-hint>Cargando usuarios…</mat-hint>
        } @else if (selectedTenantId() !== null && users().length === 0) {
          <mat-hint>El tenant no tiene usuarios para impersonalizar.</mat-hint>
        }
      </mat-form-field>

      @if (error()) {
        <p class="impersonate__error" data-testid="impersonate-error">{{ error() }}</p>
      }
    </mat-dialog-content>

    <mat-dialog-actions align="end">
      <button matButton type="button" (click)="cancel()" [disabled]="submitting()">Cancelar</button>
      <button matButton="filled" type="button" (click)="confirm()" [disabled]="!canConfirm() || submitting()">
        @if (submitting()) {
          <mat-progress-spinner mode="indeterminate" diameter="18" />
        } @else {
          Impersonalizar
        }
      </button>
    </mat-dialog-actions>
  `,
  styles: `
    .impersonate__hint {
      margin: 0 0 1rem;
      color: var(--mat-sys-on-surface-variant, rgba(0, 0, 0, 0.6));
    }
    .impersonate__field {
      width: 100%;
    }
    .impersonate__error {
      color: var(--mat-sys-error, #b3261e);
      margin: 0.5rem 0 0;
    }
  `,
})
export class ImpersonationDialogComponent {
  private readonly tenantService = inject(TenantService);
  private readonly impersonation = inject(ImpersonationService);
  private readonly dialogRef = inject(MatDialogRef<ImpersonationDialogComponent, boolean>);

  protected readonly tenants = signal<TenantOption[]>([]);
  protected readonly users = signal<ImpersonatableUser[]>([]);
  protected readonly loadingUsers = signal(false);
  protected readonly submitting = signal(false);
  protected readonly error = signal<string | null>(null);

  protected readonly selectedTenantId = signal<number | null>(null);
  protected readonly selectedUserId = signal<number | null>(null);

  protected readonly canConfirm = computed(() => this.selectedTenantId() !== null && this.selectedUserId() !== null);

  constructor() {
    this.tenantService.crud.getAll().subscribe({
      next: (result) =>
        this.tenants.set(
          result.items
            .filter((t): t is typeof t & { id: number } => t.id != null)
            .map((t) => ({ id: t.id, nombre: t.nombre })),
        ),
      error: () => this.error.set('No se pudieron cargar los tenants. Intentá de nuevo más tarde.'),
    });
  }

  protected onTenantChange(tenantId: number): void {
    this.selectedTenantId.set(tenantId);
    this.selectedUserId.set(null);
    this.users.set([]);
    this.error.set(null);
    this.loadingUsers.set(true);
    this.impersonation.getImpersonatableUsers(tenantId).subscribe({
      next: (users) => {
        this.users.set(users);
        this.loadingUsers.set(false);
      },
      error: () => {
        this.loadingUsers.set(false);
        this.error.set('No se pudieron cargar los usuarios del tenant seleccionado.');
      },
    });
  }

  protected confirm(): void {
    const tenantId = this.selectedTenantId();
    const userId = this.selectedUserId();
    if (tenantId === null || userId === null) {
      return;
    }
    this.submitting.set(true);
    this.error.set(null);
    this.impersonation.impersonate(tenantId, userId).subscribe({
      next: () => this.dialogRef.close(true),
      error: () => {
        this.submitting.set(false);
        this.error.set('No se pudo impersonalizar. Verificá el tenant y el usuario seleccionados.');
      },
    });
  }

  protected cancel(): void {
    this.dialogRef.close(false);
  }
}
