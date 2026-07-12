import { EditableEntity } from '../../../shared/forms/edit-component-base';

/**
 * Asignación de un endpoint de API a un permiso (join `gen_PermisoEndpoints`), tal como llega
 * en el detalle (`getById`). Sólo lectura en el ABM: para guardar se manda `endpoints` con `endpointId`.
 */
export interface PermisoEndpoint {
  id?: number;
  endpointId: number;
  endpointHttpMethodRoute?: string;
  descripcion?: string;
}

/** Opción de aplicación para el select del ABM (listado `api/general/aplicaciones`). */
export interface AplicacionOption {
  id: number;
  nombre: string;
}

/**
 * Permiso de una aplicación (de `aplicaciones/{id}.permisos`), con su `permisoPadreId` para
 * armar el árbol de "permiso padre" filtrado por la aplicación elegida (como el ABM original).
 */
export interface PermisoDeAplicacion {
  id: number;
  nombre: string;
  permisoPadreId?: number | null;
}

/**
 * Ítem jerárquico genérico de la API (`HierarchicalItem<long>`), usado para el árbol de
 * Endpoints (`endpoints/hierarchical-items`: Módulo→Controller→Endpoint). Los nodos Módulo y
 * Controller vienen con `id = 0` (sólo las hojas/endpoints traen el id real).
 */
export interface ApiHierarchicalItem {
  id: number;
  name: string;
  children?: ApiHierarchicalItem[];
}

/**
 * Permiso (ABM de `api/general/permisos`).
 *
 * Contrato de la API (ver `PermisoAppService` / ADR-0001):
 * - El **listado** (`getAllByCriteria`) devuelve `PermisoHeaderDto`: id, nombre, codigoPermiso,
 *   descripcion, activo, aplicacionDescripcion, permisoPadreDescripcion (sin `endpoints`).
 * - El **detalle** (`getById`) devuelve `PermisoDto` con `endpoints[]` poblado.
 * - El **guardado** (`permisos/update`) recibe `PermisoInputDto`: id, nombre, **codigoPermiso**,
 *   descripcion, activo, permisoPadreId, aplicacionId, **endpoints: [{ endpointId }]**. La API
 *   sincroniza la colección. `codigoPermiso` es editable en alta y edición (clave de negocio).
 *   `PermisoInputDto` **NO** acepta `esRestringido` (mass-assignment): se cambia por
 *   `POST permisos/{id}/set-es-restringido` (sólo root).
 */
export interface Permiso extends EditableEntity {
  id?: number;
  nombre: string;
  /** Clave de negocio; editable en alta y edición (la usan JWT claims y atributos [Permiso]). */
  codigoPermiso?: string;
  descripcion: string;
  activo: boolean;
  aplicacionId?: number | null;
  aplicacionDescripcion?: string;
  permisoPadreId?: number | null;
  permisoPadreDescripcion?: string;
  /** Editable sólo por root, vía endpoint dedicado (no viaja en el update). */
  esRestringido?: boolean;
  /** Sólo **respuesta** (`getById`): join poblado. Para guardar se usa el mismo campo con `{ endpointId }`. */
  endpoints?: PermisoEndpoint[];
}
