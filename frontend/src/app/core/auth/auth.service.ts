import { Injectable, inject } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, finalize, map, shareReplay, switchMap, tap } from 'rxjs';
import { AplicacionContextStore } from '../aplicacion-context';
import { ApiClient } from '../http';
import { AccessData } from './models/access-data.model';
import { ConfirmAccount } from './models/confirm-account.model';
import { Credentials } from './models/credentials.model';
import { AllowedRoutesService } from './allowed-routes.service';
import { AuthStore } from './auth.store';

/**
 * Operaciones de autenticación. Coordina la API y el `AuthStore`.
 *
 * El access token vive solo en memoria (`AuthStore`); el refresh token viaja en
 * cookie HttpOnly (la maneja el navegador), por eso los `/auth/*` van con
 * `withCredentials`.
 *
 * Nota: los headers `cultureInfo` y multi-tenant se agregarán cuando existan
 * `CultureService`/`TenantStore`.
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly api = inject(ApiClient);
  private readonly store = inject(AuthStore);
  private readonly allowedRoutes = inject(AllowedRoutesService);
  private readonly aplicacionStore = inject(AplicacionContextStore);
  private readonly router = inject(Router);

  /**
   * Refresh en vuelo compartido: si varios requests fallan con 401 a la vez,
   * comparten un único `/auth/refresh` (evita rotaciones concurrentes que
   * dispararían la detección de replay en la API).
   */
  private refresh$: Observable<AccessData> | null = null;

  /**
   * Login con credenciales; setea la sesión y **espera** a que se resuelva la aplicación activa
   * antes de completar — si no, el primer request del menú saldría sin `aplicacionId`.
   */
  login(credentials: Credentials): Observable<AccessData> {
    return this.api
      .post<AccessData, Credentials>('auth/token', credentials, { withCredentials: true })
      .pipe(
        tap((data) => this.store.setSession(data.token, data.validTo)),
        switchMap((data) => this.aplicacionStore.load().pipe(map(() => data))),
      );
  }

  /**
   * Confirma la cuenta (activación por mail): el usuario define su contraseña a partir del link
   * recibido. Endpoint anónimo (`/auth/confirmar-cuenta`, sin token). Devuelve el mensaje de éxito.
   */
  confirmarCuenta(model: ConfirmAccount): Observable<string> {
    return this.api.post<string, ConfirmAccount>('auth/confirmar-cuenta', model);
  }

  /** Cierra la sesión: revoca el refresh en el server (best-effort) y limpia local. */
  logout(): void {
    this.api.post('auth/logout', undefined, { withCredentials: true }).subscribe({
      next: () => this.finishLogout(),
      error: () => this.finishLogout(),
    });
  }

  /**
   * Renovación silenciosa usando la cookie HttpOnly del refresh token. Serializada
   * con `shareReplay` para que haya un único `/auth/refresh` en vuelo.
   */
  refreshSession(): Observable<AccessData> {
    this.refresh$ ??= this.api
      .post<AccessData>('auth/refresh', undefined, { withCredentials: true })
      .pipe(
        tap((data) => this.store.setSession(data.token, data.validTo)),
        shareReplay(1),
        finalize(() => (this.refresh$ = null)),
      );
    return this.refresh$;
  }

  private finishLogout(): void {
    this.store.clearSession();
    this.allowedRoutes.clear();
    this.aplicacionStore.clear();
    void this.router.navigateByUrl('/login', { replaceUrl: true });
  }
}
