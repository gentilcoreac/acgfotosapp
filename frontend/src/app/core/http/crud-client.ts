import { inject } from '@angular/core';
import { Observable } from 'rxjs';
import { QueryParams } from '../models/query-params.model';
import { QueryResult } from '../models/query-result.model';
import { ApiClient } from './api-client';

/** pageSize grande para `getAll()` cuando se necesita el listado completo. */
const ALL_PAGE_SIZE = 19999;

/**
 * CRUD genérico para una entidad de un módulo de la API
 * (`{base}/api/{module}/{entity}`). Reemplaza la herencia de `ApiService<T>`
 * del original por composición.
 *
 * Crear con el helper `injectCrudClient` desde un servicio:
 *
 * ```ts
 * @Injectable({ providedIn: 'root' })
 * export class RolesService {
 *   private readonly api = injectCrudClient<Rol>('Role');
 *   getAll(query: QueryParams) { return this.api.getAllByCriteria(query); }
 * }
 * ```
 */
export class CrudClient<T> {
  constructor(
    private readonly api: ApiClient,
    private readonly entity: string,
    private readonly module = 'general',
  ) {}

  /** Path base de la entidad relativo a `/api`: `{module}/{entity}`. */
  private get basePath(): string {
    return `${this.module}/${this.entity}`;
  }

  /** Listado paginado/filtrado. */
  getAllByCriteria(query: QueryParams): Observable<QueryResult<T>> {
    return this.api.getQueryResult<T>(this.basePath, query);
  }

  /** Listado completo (sin paginar). Usar con criterio. */
  getAll(): Observable<QueryResult<T>> {
    return this.getAllByCriteria({ page: 0, pageSize: ALL_PAGE_SIZE });
  }

  /** Obtiene una entidad por id. */
  getById(id: number | string): Observable<T> {
    return this.api.get<T>(`${this.basePath}/${id}`);
  }

  /** Crea o actualiza la entidad (la API resuelve por el id del body). */
  save(entity: T): Observable<T> {
    return this.api.post<T, T>(`${this.basePath}/update`, entity);
  }

  /** Elimina por id. */
  delete(id: number | string): Observable<void> {
    return this.api.delete<void>(`${this.basePath}/${id}`);
  }
}

/**
 * Crea un `CrudClient<T>` resolviendo `ApiClient` del contexto de inyección.
 * Llamar en el inicializador de un campo de servicio (corre en injection context).
 */
export function injectCrudClient<T>(entity: string, module = 'general'): CrudClient<T> {
  return new CrudClient<T>(inject(ApiClient), entity, module);
}
