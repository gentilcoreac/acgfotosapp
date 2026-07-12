import { HttpErrorResponse } from '@angular/common/http';
import {
  EnvironmentProviders,
  inject,
  makeEnvironmentProviders,
  provideAppInitializer,
} from '@angular/core';
import { catchError, firstValueFrom, of, switchMap } from 'rxjs';
import { AplicacionContextStore } from '../aplicacion-context';
import { AppConfigService } from '../config';
import { AuthService } from './auth.service';
import { AuthStore, ReconnectReason } from './auth.store';

/**
 * Clasifica el fallo del refresh. Devuelve la causa transitoria (conviene reintentar, no ir a
 * login) o `null` si es un fallo "de verdad" (401/403/sin cookie). Distinguir 429 de API caída
 * importa: la pantalla de reconexión muestra un mensaje distinto según la causa.
 */
function transientReason(status: number): ReconnectReason | null {
  if (status === 429) {
    return 'rate-limit';
  }
  if (status === 0) {
    return 'network';
  }
  if (status >= 500) {
    return 'server';
  }
  return null;
}

/**
 * Recupera la sesión al arrancar la app (F5 / pestaña nueva) usando la cookie
 * HttpOnly del refresh token.
 *
 * - Refresh OK → restaura sesión + carga aplicaciones.
 * - Refresh con 401/sin cookie → usuario anónimo, sigue a login.
 * - Refresh con fallo **transitorio** (429 rate limit / red / 5xx) → marca `reconnecting`: el
 *   `authGuard` manda a la pantalla de reconexión (la sesión probablemente sigue válida; reintentar
 *   en vez de patear a login). Resuelve igual el initializer para no bloquear el arranque.
 *
 * Espera a que la config esté cargada (comparte el `load()` idempotente de
 * `provideAppConfig`) porque el refresh necesita la URL base de la API.
 */
export function provideAuthSessionRestore(): EnvironmentProviders {
  return makeEnvironmentProviders([
    provideAppInitializer(() => {
      const config = inject(AppConfigService);
      const authService = inject(AuthService);
      const aplicacionStore = inject(AplicacionContextStore);
      const authStore = inject(AuthStore);
      return config.load().then(() =>
        firstValueFrom(
          authService.refreshSession().pipe(
            switchMap(() => aplicacionStore.load()),
            catchError((err: HttpErrorResponse) => {
              const reason = transientReason(err?.status ?? 0);
              if (reason) {
                authStore.setReconnecting(true, reason);
              }
              return of(null);
            }),
          ),
        ),
      );
    }),
  ]);
}
