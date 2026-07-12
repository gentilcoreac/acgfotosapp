import { Injectable, inject } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, map, switchMap, tap } from 'rxjs';
import { AplicacionContextStore } from '../aplicacion-context';
import { ApiClient } from '../http';
import { AccessData } from './models/access-data.model';
import { ImpersonatableUser } from './models/impersonatable-user.model';
import { AllowedRoutesService } from './allowed-routes.service';
import { AuthStore } from './auth.store';

/**
 * Impersonalización (ADR-0002): un usuario **root** opera "como" un usuario concreto de otro tenant.
 *
 * El server re-emite un JWT scopeado al usuario destino (`isRoot=false` + claim `impersonatedBy`) y
 * sostiene la sesión a través de los refresh con una cookie-overlay firmada (por eso `withCredentials`).
 * Acá solo hacemos el **swap del access token** en memoria y limpiamos las cachés que dependen del
 * usuario/tenant activo (rutas permitidas; el menú lateral se recarga solo al cambiar `currentUserName`).
 */
@Injectable({ providedIn: 'root' })
export class ImpersonationService {
  private readonly api = inject(ApiClient);
  private readonly store = inject(AuthStore);
  private readonly allowedRoutes = inject(AllowedRoutesService);
  private readonly aplicacionStore = inject(AplicacionContextStore);
  private readonly router = inject(Router);

  /** Todos los usuarios del tenant (para el selector). Root-only en la API. */
  getImpersonatableUsers(tenantId: number): Observable<ImpersonatableUser[]> {
    return this.api.get<ImpersonatableUser[]>(`auth/impersonation/users/${tenantId}`);
  }

  /** Impersonaliza al usuario `userId` del tenant `tenantId` (root-only en la API). */
  impersonate(tenantId: number, userId: number): Observable<AccessData> {
    return this.api
      .post<AccessData>('auth/impersonation/start', { tenantId, userId }, { withCredentials: true })
      .pipe(switchMap((data) => this.applyContextSwitch(data)));
  }

  /** Vuelve al contexto de root. */
  stop(): Observable<AccessData> {
    return this.api
      .post<AccessData>('auth/impersonation/stop', undefined, { withCredentials: true })
      .pipe(switchMap((data) => this.applyContextSwitch(data)));
  }

  /**
   * Setea la sesión y **espera** a que se resuelvan las apps del usuario efectivo antes de
   * navegar — si navegara antes, el primer request del menú saldría sin `aplicacionId`.
   */
  private applyContextSwitch(data: AccessData): Observable<AccessData> {
    this.store.setSession(data.token, data.validTo);
    this.allowedRoutes.clear();
    // load() es async: durante su ejecución los efectos del contexto anterior (DashboardContextService)
    // pueden disparar getAllowedPaths() con el aplicacionId viejo y envenenar el cache. El segundo
    // clear() post-load invalida esa respuesta tardía vía el contador de generación.
    return this.aplicacionStore.load().pipe(
      tap(() => {
        this.allowedRoutes.clear();
        void this.router.navigateByUrl('/', { replaceUrl: true });
      }),
      map(() => data),
    );
  }
}
