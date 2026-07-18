import { HttpErrorResponse, HttpInterceptorFn, HttpRequest } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, switchMap, throwError } from 'rxjs';
import { AplicacionContextStore } from '../aplicacion-context';
import { FamiliaSessionStore } from '../familia';
import { AuthService } from './auth.service';
import { AuthStore } from './auth.store';

/** Endpoints anónimos: no llevan `Authorization` ni disparan refresh ante 401. */
const ANONYMOUS_URL =
  /\/(auth\/token|auth\/refresh|auth\/olvide-password|auth\/resetear-password|auth\/confirmar-cuenta|version|fotos\/canje)/;

/** Lleva `Authorization` pero NO dispara refresh ante 401 (evita recursión en el propio logout). */
const NO_REFRESH_ON_401 = /\/auth\/logout/;

/**
 * Galería de familia (ADR-11): lleva el JWT de `FamiliaSessionStore`, NUNCA el de plataforma — son
 * sesiones distintas y pueden convivir en el mismo navegador (el fotógrafo probando su propio evento).
 */
const FAMILIA_URL = /\/fotos\/familia\//;

/**
 * Interceptor de autenticación (funcional).
 *
 * - Agrega `Authorization: Bearer <token>` en requests no anónimos.
 * - Ante `401`, hace un refresh silencioso (serializado en `AuthService`) y
 *   **reintenta** el request original con el token nuevo. Si el refresh falla,
 *   cierra sesión.
 * - Agrega `aplicacionId` (dinámico desde `AplicacionContextStore`) en requests autenticados
 *   cuando hay una aplicación activa resuelta (root y no-root por igual, como el cliente original).
 */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const store = inject(AuthStore);
  const authService = inject(AuthService);
  const aplicacionStore = inject(AplicacionContextStore);
  const familiaStore = inject(FamiliaSessionStore);

  if (ANONYMOUS_URL.test(req.url)) {
    return next(req);
  }

  if (FAMILIA_URL.test(req.url)) {
    // Sin refresh (no existe para la sesión de familia, ADR-11): si el token de 30 min venció, el
    // 401 pasa tal cual — el guard de `/mi-album` manda de vuelta a `/canje` en la próxima navegación.
    return next(withBearer(req, familiaStore.token(), null));
  }

  const aplicacionId = aplicacionStore.selectedId();
  return next(withBearer(req, store.accessToken(), aplicacionId)).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status !== 401 || NO_REFRESH_ON_401.test(req.url)) {
        return throwError(() => error);
      }
      return authService.refreshSession().pipe(
        // El `catchError` va ANTES del `switchMap` para que solo capture fallos del **refresh**, no
        // del request reintentado. Solo cerramos sesión si el refresh fue **rechazado de verdad**
        // (401/403 = refresh token inválido/expirado). Ante un blip de la API (error de red/CORS =
        // status 0, o 5xx) NO deslogueamos: la sesión puede seguir vigente y el usuario se recupera
        // solo en el próximo intento / F5, sin quedar pateado al login por un hipo del backend.
        catchError((refreshError: HttpErrorResponse) => {
          if (refreshError.status === 401 || refreshError.status === 403) {
            authService.logout();
          }
          return throwError(() => error);
        }),
        switchMap(() => next(withBearer(req, store.accessToken(), aplicacionId))),
      );
    }),
  );
};

function withBearer(
  req: HttpRequest<unknown>,
  token: string | null,
  aplicacionId: number | null,
): HttpRequest<unknown> {
  const setHeaders: Record<string, string> = {};
  if (token) setHeaders['Authorization'] = `Bearer ${token}`;
  if (aplicacionId != null) setHeaders['aplicacionId'] = String(aplicacionId);
  return req.clone({ setHeaders });
}
