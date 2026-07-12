import { EditableEntity } from '../../../shared/forms/edit-component-base';

/**
 * Asignación de un Rol a un Tipo de Licencia (join `gen_TipoLicenciaRoles`), tal como llega
 * en el detalle (`getById`). Sólo lectura en el ABM: para guardar se usa `rolIds`.
 */
export interface TipoLicenciaRole {
  id?: number;
  rolId: number;
  rolDescripcion?: string;
}

/** Opción de rol para los checkboxes del ABM (listado `api/general/roles`). */
export interface RolOption {
  id: number;
  descripcion: string;
}

/**
 * Tipo de licencia (ABM de `api/general/tipos-licencia`).
 */
export interface TipoLicencia extends EditableEntity {
  id?: number;
  codigoTipoLicencia: string;
  descripcion: string;
  /** Editable sólo por root, vía endpoint dedicado (no viaja en el update). */
  esDefaultParaNuevoTenant?: boolean;
  /** Sólo **respuesta** (`getById`): colección poblada del join. Para guardar se usa `rolIds`. */
  tipoLicenciaRoles?: TipoLicenciaRole[];
  /** Sólo **request** (`tipos-licencia/update`): ids de los roles asignados. */
  rolIds?: number[];
}
