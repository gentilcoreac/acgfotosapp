import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { RouterLink } from '@angular/router';
import { AuthService, AuthStore } from '../../core/auth';

/**
 * Página de acceso denegado (403). El `allowedRoutesGuard` redirige acá cuando el usuario intenta
 * entrar por URL a una sección sin permiso. La autorización real la valida la API; esto es solo UX
 * de ruteo (le explica al usuario por qué no llegó, en vez de dejarlo en el home sin contexto).
 *
 * Sigue el patrón "You need access" de las suites enterprise (M365 / Google Workspace): muestra con
 * qué usuario está la sesión (clave bajo impersonalización), sugiere contactar al administrador y
 * ofrece salidas (volver al inicio / cerrar sesión).
 */
@Component({
  selector: 'tbi-acceso-denegado',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatButtonModule, MatIconModule, RouterLink],
  template: `
    <div class="error-page" data-testid="acceso-denegado-page">
      <mat-icon class="error-page__icon">lock</mat-icon>
      <p class="error-page__code">403</p>
      <h1 class="error-page__title">No tenés permiso para ver esta sección</h1>
      @if (userName(); as user) {
        <p class="error-page__text">
          Estás como <strong data-testid="acceso-denegado-user">{{ user }}</strong>
        </p>
      }
      <p class="error-page__text">
        Si creés que es un error, contactá a tu administrador para que te habilite el acceso.
      </p>
      <div class="error-page__actions">
        <a matButton="filled" routerLink="/" data-testid="error-go-home">Ir al inicio</a>
        <button matButton (click)="logout()" data-testid="error-logout">Cerrar sesión</button>
      </div>
    </div>
  `,
  styleUrl: './error-page.scss',
})
export class AccesoDenegadoComponent {
  private readonly auth = inject(AuthService);
  protected readonly userName = inject(AuthStore).currentUserName;

  protected logout(): void {
    this.auth.logout();
  }
}
