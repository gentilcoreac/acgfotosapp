import { HttpClient, HttpResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { QueryParams } from '../models/query-params.model';
import { QueryResult } from '../models/query-result.model';
import { AppConfigService } from '../config';
import { toHttpParams } from './http-params.util';

/**
 * Cliente HTTP tipado de bajo nivel. Resuelve las URLs contra la base de la API
 * (`AppConfigService.buildApiUrl`) y delega en `HttpClient`.
 *
 * Reemplaza a `ApiService<T>` del original:
 * - No setea headers manualmente. `Content-Type` lo infiere Angular para bodies
 *   objeto; `Authorization` / `cultureInfo` / multi-tenant los agrega el
 *   `authInterceptor` (no este cliente). Esto elimina el bug del original, donde
 *   `HttpHeaders.set()` (inmutable) se ignoraba y el header nunca se aplicaba.
 * - Tipado real (sin `any`) y params encodeados vía `HttpParams`.
 *
 * Para CRUD de entidades, preferir `CrudClient` (composición sobre este cliente).
 */
@Injectable({ providedIn: 'root' })
export class ApiClient {
  private readonly http = inject(HttpClient);
  private readonly config = inject(AppConfigService);

  /** GET a `{base}/api/{path}` con query params opcionales. */
  get<T>(path: string, query?: QueryParams): Observable<T> {
    return this.http.get<T>(this.config.buildApiUrl(path), { params: toHttpParams(query) });
  }

  /** GET que devuelve un resultado paginado `QueryResult<T>`. */
  getQueryResult<T>(path: string, query?: QueryParams): Observable<QueryResult<T>> {
    return this.get<QueryResult<T>>(path, query);
  }

  /**
   * POST con body JSON. `withCredentials` habilita el envío/recepción de cookies
   * (necesario para la cookie HttpOnly del refresh token en los endpoints `/auth/*`).
   */
  post<TResponse, TBody = unknown>(
    path: string,
    body?: TBody,
    options?: { withCredentials?: boolean },
  ): Observable<TResponse> {
    return this.http.post<TResponse>(this.config.buildApiUrl(path), body ?? null, {
      withCredentials: options?.withCredentials ?? false,
    });
  }

  /** POST multipart (`FormData`) — subida de archivos. No se setea `Content-Type` a mano: el
   * browser lo infiere de la `FormData` (incluido el boundary); fijarlo lo rompería. */
  postMultipart<TResponse>(path: string, formData: FormData): Observable<TResponse> {
    return this.http.post<TResponse>(this.config.buildApiUrl(path), formData);
  }

  /** GET binario (descarga de archivos). Devuelve la respuesta completa para poder leer el nombre
   * del archivo del header `Content-Disposition` (ver `saveBlobResponse`). */
  getBlob(path: string, query?: QueryParams): Observable<HttpResponse<Blob>> {
    return this.http.get(this.config.buildApiUrl(path), {
      params: toHttpParams(query),
      observe: 'response',
      responseType: 'blob',
    });
  }

  /** POST que responde binario (exports con body de filtros). */
  postBlob<TBody = unknown>(path: string, body?: TBody): Observable<HttpResponse<Blob>> {
    return this.http.post(this.config.buildApiUrl(path), body ?? null, {
      observe: 'response',
      responseType: 'blob',
    });
  }

  /** PUT con body JSON. */
  put<TResponse, TBody = unknown>(path: string, body?: TBody): Observable<TResponse> {
    return this.http.put<TResponse>(this.config.buildApiUrl(path), body ?? null);
  }

  /** DELETE. */
  delete<T = void>(path: string): Observable<T> {
    return this.http.delete<T>(this.config.buildApiUrl(path));
  }
}
