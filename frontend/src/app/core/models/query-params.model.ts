/** Valores admitidos como parámetros de query (filtros). */
export type QueryParamValue = string | number | boolean;

/**
 * Parámetros de listado: paginación, orden, búsqueda y filtros arbitrarios.
 *
 * Reemplaza al `QueryParamsModel` (clase) del original, que leía el default de
 * `pageSize` desde el `environment` global. Acá es una interfaz pura; el default
 * de `pageSize` lo resuelve `CrudClient` desde `AppConfigService`.
 */
export interface QueryParams {
  /** Texto de búsqueda libre. */
  searchText?: string;
  /** Campo por el cual ordenar. */
  orderBy?: string;
  /** Orden descendente (true) o ascendente (false). */
  descendingOrder?: boolean;
  /** Página (base 0). */
  page?: number;
  /** Tamaño de página. */
  pageSize?: number;
  /** Filtros adicionales específicos de cada listado. */
  [key: string]: QueryParamValue | null | undefined;
}
