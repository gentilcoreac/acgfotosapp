import { HttpParams } from '@angular/common/http';
import { QueryParams } from '../models/query-params.model';

/**
 * Convierte `QueryParams` en `HttpParams`, ignorando valores vacíos
 * (`null`, `undefined`, `''`). Reemplaza al `urlParam()` manual del original:
 * `HttpParams` encodea correctamente y evita el chequeo frágil de `[object Object]`.
 */
export function toHttpParams(query?: QueryParams): HttpParams {
  let params = new HttpParams();
  if (!query) {
    return params;
  }
  for (const [key, value] of Object.entries(query)) {
    if (value === null || value === undefined || value === '') {
      continue;
    }
    params = params.set(key, String(value));
  }
  return params;
}
