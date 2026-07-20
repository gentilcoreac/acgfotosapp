import { ResourceRef } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { Observable, catchError, of } from 'rxjs';

/**
 * `rxResource` de sólo-lectura sin `params` (fetch estático, ej. un lookup para combos) con
 * `fallback` como sentinel de error: si `fetchFn()` falla, el resource nunca entra en estado
 * error — devuelve `fallback` en vez de que `.value()` relance la excepción.
 */
export function lookupResource<T>(
  fetchFn: () => Observable<T>,
  fallback: T,
): ResourceRef<T | undefined> {
  return rxResource({
    stream: () => fetchFn().pipe(catchError(() => of(fallback))),
  });
}
