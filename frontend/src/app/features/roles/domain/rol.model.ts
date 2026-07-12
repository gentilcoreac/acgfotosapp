import { EditableEntity } from '../../../shared/forms/edit-component-base';

/**
 * Rol del sistema (ABM de `api/general/roles`).
 *
 * Contrato de la API (refactor):
 * - El **listado** (`getAllByCriteria`) devuelve `RolHeaderDto` recortado: sólo `id`,
 *   `descripcion` y `esDefaultParaNuevoTenant` (sin `rolPermisos`).
 * - El **detalle** (`getById` → `roles/{id}`) devuelve `RolDto` con `rolPermisos` poblado.
 * - El **guardado** (`roles/update`) recibe `RolInputDto` (`id`, `descripcion`, `permisoIds`).
 *   Los permisos se mandan como **ids** (`permisoIds`), no como el DTO completo del join: la API
 *   sincroniza la colección a partir de esos ids (ver `RolAppService.SyncCollections`).
 *   `RolInputDto` **NO** acepta `esDefaultParaNuevoTenant` (protección de mass-assignment): ese
 *   flag se cambia sólo por el endpoint dedicado `POST roles/{id}/set-default-tenant`, que la API
 *   restringe a **root** (`AppContext.IsRoot`). El ABM muestra el toggle sólo a root (UX) y, si
 *   cambia, lo persiste por ese endpoint tras el update (ver `rol-edit.component.ts` → afterSave).
 *   A diferencia del front original, que no permitía editarlo en roles.
 */
export interface Rol extends EditableEntity {
  id?: number;
  descripcion: string;
  /** Editable sólo por root, vía endpoint dedicado (no viaja en `RolInputDto`). */
  esDefaultParaNuevoTenant?: boolean;
  /** Sólo **respuesta** (`getById`): colección poblada del join. Para guardar se usa `permisoIds`. */
  rolPermisos?: RolPermiso[];
  /** Sólo **request** (`roles/update`): ids de los permisos asignados. */
  permisoIds?: number[];
}

/** Fila del join Rol-Permiso, tal como llega en el detalle (`getById`). Sólo lectura en el ABM. */
export interface RolPermiso {
  id?: number;
  rolId?: number;
  permisoId: number;
}
