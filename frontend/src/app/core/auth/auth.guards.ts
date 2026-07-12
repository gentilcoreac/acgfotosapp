import { inject } from '@angular/core';
import { CanActivateChildFn, CanMatchFn, Router } from '@angular/router';
import { map } from 'rxjs';
import { AllowedRoutesService } from './allowed-routes.service';
import { AuthStore } from './auth.store';

/**
 * Permite la ruta si hay sesión válida. Si no:
 * - `reconnecting` (la restauración falló por 429/red/5xx, sesión probablemente válida) → manda a la
 *   pantalla de reconexión con `returnUrl` para volver acá al reconectar.
 * - en otro caso → `/login`.
 * Reemplaza a `IsAuthenticatedGuard` (mejora: redirige en vez de devolver false).
 */
export const authGuard: CanMatchFn = (_route, segments) => {
  const store = inject(AuthStore);
  const router = inject(Router);
  if (store.isAuthenticated()) {
    return true;
  }
  if (store.reconnecting()) {
    const returnUrl = '/' + segments.map((s) => s.path).join('/');
    return router.createUrlTree(['/reconnecting'], { queryParams: { returnUrl } });
  }
  return router.createUrlTree(['/login']);
};

/**
 * Permite la ruta solo a usuarios anónimos (login, recuperar clave); si ya hay
 * sesión, redirige al home. Reemplaza a `IsNotAuthenticatedGuard`.
 */
export const anonGuard: CanMatchFn = () => {
  const store = inject(AuthStore);
  const router = inject(Router);
  return store.isAuthenticated() ? router.createUrlTree(['/']) : true;
};

/**
 * Autoriza la navegación a una ruta hija del layout según los permisos del usuario
 * (`GET menus/allowed-routes`). Si la ruta no está permitida, redirige a `/sin-permisos` (le
 * explica al usuario por qué no llegó, en vez de dejarlo en el home sin contexto).
 *
 * Es UX de ruteo: complementa al sidenav dinámico para que un usuario no llegue por URL directa a
 * una pantalla sin permiso. La autorización real la sigue validando la API en cada endpoint.
 */
export const allowedRoutesGuard: CanActivateChildFn = (_childRoute, state) => {
  const allowed = inject(AllowedRoutesService);
  const router = inject(Router);
  return allowed
    .isAllowed(state.url)
    .pipe(map((ok) => ok || router.createUrlTree(['/sin-permisos'])));
};
