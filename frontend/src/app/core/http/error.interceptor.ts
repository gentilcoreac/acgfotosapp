import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, from, switchMap, throwError } from 'rxjs';
import { NotificationService } from '../../shared/feedback/notification.service';
import { ApiError } from '../models/api-error.model';

/**
 * Mensaje por defecto según el estado, cuando la API no manda cuerpo de error. Hoy se muestran tal
 * cual (i18n diferido, Fase 4); el 429 va en texto legible porque es el caso más visible para el
 * usuario (rate limiting: muchos logins/refresh seguidos) y debe ser claro, no una clave cruda.
 */
const FALLBACK_MESSAGE_BY_STATUS: Record<number, string> = {
  0: 'ErrorConnectionTimeOut',
  403: 'ErrorAccessDenied',
  404: 'ErrorPageNotFound',
  429: 'Demasiadas solicitudes. Esperá unos segundos e intentá nuevamente.',
  500: 'ErrorInternalServer',
};

/**
 * Manejo centralizado de errores HTTP (funcional). Reemplaza a
 * `HttpErrorHandlerInterceptor` + `responseHandler` del original.
 *
 * - Normaliza el error de la API a `ApiError` (`{ status, message, errors, traceId? }`).
 * - Parsea errores en `Blob` (cuando `responseType: 'blob'`).
 * - **Notifica el error con un toast global** (throttled): única fuente de aviso de errores, así los
 *   componentes no repiten `notify.error` (evita doble toast) y los fallos "de fondo" (widgets del
 *   home, 429, etc.) siempre se ven. Los formularios (login, diálogos de edición) además muestran el
 *   error inline en su propia tarjeta.
 * - Ante `404` redirige a `/404` (sin toast: la página ya comunica).
 * - **No** toca el `401`: lo deja pasar para que lo maneje el `authInterceptor` (refresh/logout).
 */
export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const router = inject(Router);
  const notification = inject(NotificationService);

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      // El 401 es responsabilidad del authInterceptor (refresh/logout).
      if (error.status === 401) {
        return throwError(() => error);
      }

      // Error serializado en Blob (responseType 'blob'): leerlo y parsearlo.
      if (isJsonBlobError(error)) {
        const blob = error.error as Blob;
        return from(blob.text()).pipe(
          switchMap((text: string) => {
            const apiError = normalize(error, parseJson(text), router);
            notify(notification, apiError);
            return throwError(() => apiError);
          }),
        );
      }

      const apiError = normalize(error, error.error, router);
      notify(notification, apiError);
      return throwError(() => apiError);
    }),
  );
};

/**
 * Toast global del error, salvo el 404 (la página /404 ya comunica). La coalescencia (no encadenar
 * toasts ante ráfagas, ni doblar con el toast del componente) la maneja `NotificationService.error`.
 * Pasa titular + detalle + traceId: el toast decide cómo mostrarlos (Ver detalle / ref).
 */
function notify(notification: NotificationService, apiError: ApiError): void {
  if (apiError.status === 404) {
    return;
  }
  notification.error(apiError.message, apiError.errors, apiError.traceId);
}

function isJsonBlobError(error: HttpErrorResponse): boolean {
  return error.error instanceof Blob && error.error.type === 'application/json';
}

function parseJson(text: string): unknown {
  try {
    return JSON.parse(text);
  } catch {
    return text;
  }
}

function normalize(error: HttpErrorResponse, body: unknown, router: Router): ApiError {
  if (error.status === 404) {
    void router.navigate(['/404']);
  }
  return { status: error.status, ...extract(error.status, body) };
}

/**
 * Desarma el cuerpo del error a `{ message, errors, traceId }`. El camino principal es la forma
 * estándar de la API `{ message, errors, traceId? }`; se mantienen los fallbacks (string[], string
 * suelto, sin cuerpo) por robustez ante respuestas viejas o de infra (proxies, 429 sin body).
 */
function extract(status: number, body: unknown): Pick<ApiError, 'message' | 'errors' | 'traceId'> {
  // Forma estándar: objeto { message, errors, traceId }.
  if (body && typeof body === 'object' && !Array.isArray(body)) {
    const record = body as Record<string, unknown>;
    const errors = Array.isArray(record['errors'])
      ? record['errors'].map((item) => String(item))
      : [];
    const rawMessage = typeof record['message'] === 'string' ? record['message'] : '';
    const message = rawMessage || errors[0] || fallbackMessage(status);
    const traceId = typeof record['traceId'] === 'string' ? record['traceId'] : undefined;
    // Si el titular se tomó de errors[0] (no vino `message`), no lo repetimos en el detalle.
    const detail = rawMessage ? errors : errors.slice(1);
    return { message, errors: detail, traceId };
  }
  // Legacy: array de strings (el primero es el titular; el resto, detalle).
  if (Array.isArray(body) && body.every((item) => typeof item === 'string')) {
    const arr = body as string[];
    return { message: arr[0] ?? fallbackMessage(status), errors: arr.slice(1) };
  }
  // Legacy: string suelto.
  if (typeof body === 'string' && body.length > 0) {
    return { message: body, errors: [] };
  }
  // Sin cuerpo útil: mensaje por defecto según el status.
  return { message: fallbackMessage(status), errors: [] };
}

function fallbackMessage(status: number): string {
  return FALLBACK_MESSAGE_BY_STATUS[status] ?? 'ErrorUnexpected';
}
