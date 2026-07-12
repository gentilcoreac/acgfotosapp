/**
 * Resultado paginado de un listado de la API.
 *
 * Reemplaza al `QueryResultsModel<T>` del original. Tipado real en `items`.
 */
export interface QueryResult<T> {
  /** Items de la página actual. */
  items: T[];
  /** Total de registros que matchean el criterio (para el paginador). */
  totalCount: number;
  /** Mensaje de error opcional devuelto por la API. */
  errorMessage?: string;
}
