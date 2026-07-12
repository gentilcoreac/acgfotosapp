import { EditableEntity } from '../../../shared/forms/edit-component-base';

/**
 * Grupo de usuarios (ABM de `api/general/grupos`).
 *
 * Contrato de la API (feature nuevo, alineado a `roles`):
 * - El **listado** (`getAllByCriteria`) devuelve `GrupoHeaderDto`: `id`, `nombre` y
 *   `cantidadMiembros` (contado en SQL, sin cargar los usuarios).
 * - El **detalle** (`getById` → `grupos/{id}`) devuelve `GrupoDto` con `usuarioGrupos` poblado.
 * - El **guardado** (`grupos/update`) recibe `GrupoInputDto` (`id`, `nombre`, `usuarioIds`).
 *   Los miembros se mandan como **ids** (`usuarioIds`), no como el DTO del join: la API
 *   sincroniza la colección a partir de esos ids (ver `GrupoAppService.SyncCollections`).
 */
export interface Grupo extends EditableEntity {
  id?: number;
  nombre: string;
  /** Sólo **listado** (`GrupoHeaderDto`): cantidad de miembros (contada en SQL). */
  cantidadMiembros?: number;
  /** Sólo **respuesta** (`getById`): colección poblada del join. Para guardar se usa `usuarioIds`. */
  usuarioGrupos?: UsuarioGrupo[];
  /** Sólo **request** (`grupos/update`): ids de los usuarios miembros. */
  usuarioIds?: number[];
  /** Sólo **respuesta** (`getById`): roles que otorga el grupo. Para guardar se usa `rolIds`. */
  grupoRoles?: GrupoRol[];
  /** Sólo **request** (`grupos/update`): ids de los roles que otorga el grupo (seguridad por grupo). */
  rolIds?: number[];
}

/** Fila del join Usuario-Grupo, tal como llega en el detalle (`getById`). Sólo lectura en el ABM. */
export interface UsuarioGrupo {
  id?: number;
  grupoId?: number;
  usuarioId: number;
  /** Datos de display del miembro (sólo en el detalle), para armar los chips del selector en edición. */
  usuarioUserName?: string;
  usuarioNombre?: string;
  usuarioApellido?: string;
  /** Licencia activa del miembro (sólo detalle). Para avisar si el grupo mezcla licencias (§11.2). */
  usuarioTipoLicenciaActivaId?: number | null;
}

/** Fila del join Grupo-Rol, tal como llega en el detalle (`getById`). Sólo lectura en el ABM. */
export interface GrupoRol {
  id?: number;
  grupoId?: number;
  rolId: number;
}

/** Opción de rol para los checkboxes del ABM (`api/general/roles/del-tenant`). */
export interface RolOption {
  id: number;
  descripcion: string;
  /**
   * Licencia(s) del tenant que incluyen este rol (§11.1). Sirven para mostrar chips junto al rol,
   * así el admin entiende de qué licencia viene cada uno. Un rol puede estar en varias.
   */
  licencias?: TipoLicenciaTag[];
}

/** Etiqueta mínima de una licencia (id + descripción) para los chips del selector de roles. */
export interface TipoLicenciaTag {
  id: number;
  descripcion: string;
}
