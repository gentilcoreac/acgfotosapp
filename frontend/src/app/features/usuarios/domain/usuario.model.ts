import { EditableEntity } from '../../../shared/forms/edit-component-base';

/**
 * Asignación de un Rol a un Usuario (join `gen_UsuarioRoles`). En el ABM se edita como
 * conjunto de ids; al guardar se manda `{ rolId, usuarioId }` (el shape que acepta la API).
 */
export interface UsuarioRol {
  id?: number;
  usuarioId?: number;
  rolId: number;
  rolDescripcion?: string;
}

/**
 * Asignación de una Aplicación a un Usuario (join `gen_UsuarioAplicaciones`). `default` marca
 * la aplicación de inicio; sólo una puede ser default.
 */
export interface UsuarioAplicacion {
  id?: number;
  usuarioId?: number;
  aplicacionId: number;
  aplicacionNombre?: string;
  default: boolean;
}

/** Licencia activa del usuario (sólo lectura en este ABM: la API no la recibe en el update). */
export interface TipoLicenciaActiva {
  id?: number;
  usuarioId?: number;
  tipoLicenciaId: number;
  isActive: boolean;
}

/** Opción de rol para los checkboxes del ABM (listado `api/general/roles`). */
export interface RolOption {
  id: number;
  descripcion: string;
}

/** Opción de aplicación para el ABM (`api/general/aplicaciones/aplicaciones-tenant`: las del tenant). */
export interface AplicacionOption {
  id: number;
  nombre: string;
}

/** Opción de tipo de licencia para el select del ABM (`api/general/tipos-licencia`). */
export interface TipoLicenciaOption {
  id: number;
  descripcion: string;
}

/** Rol que habilita una licencia (item de `tipoLicenciaRoles`). */
export interface TipoLicenciaRoleItem {
  id?: number;
  rolId: number;
  rolDescripcion?: string;
}

/** Detalle de un tipo de licencia con sus roles (getById de `tipos-licencia`). */
export interface TipoLicenciaConRoles {
  id: number;
  descripcion: string;
  tipoLicenciaRoles?: TipoLicenciaRoleItem[];
}

/**
 * Usuario (ABM de `api/general/usuarios`).
 *
 * El listado (`UsuarioHeaderDto`) trae sólo campos de cabecera; el detalle (`getById`) trae
 * `roles` y `usuarioAplicaciones`. El payload de `update` (`UsuarioInputDto`) acepta sólo:
 * nombre, apellido, email, telefono, bloqueado, roles y usuarioAplicaciones. `administrador`
 * y el bloqueo/desbloqueo se gestionan por endpoints dedicados; la licencia activa no es
 * editable desde acá (no está en el input contract).
 */
export interface Usuario extends EditableEntity {
  id?: number;
  userName?: string;
  nombre: string;
  apellido: string;
  email: string;
  telefono?: number | null;
  bloqueado?: boolean;
  /** Sólo **respuesta**: lo cambia el endpoint dedicado `set-administrador`, no el update. */
  administrador?: boolean;
  /** Sólo **respuesta**: la API lo gestiona (no se manda en el update). */
  emailConfirmed?: boolean;
  ultimoLogin?: string | null;
  tipoLicenciaActiva?: TipoLicenciaActiva | null;
  roles?: UsuarioRol[];
  usuarioAplicaciones?: UsuarioAplicacion[];
}

/** Body del endpoint `bloquear-desbloquear`. */
export interface UserStateInput {
  userName: string;
  state: boolean;
}

/**
 * Actividad de usuarios de un mes (endpoint `usuarios/actividad-mensual`), para el gráfico del
 * homepage: `activos` = usuarios distintos que iniciaron sesión ese mes; `altas` = usuarios creados.
 * `mes` es 1-12.
 */
export interface UsuarioActividadMes {
  anio: number;
  mes: number;
  activos: number;
  altas: number;
}

/**
 * Ventana (en meses) del gráfico de actividad del homepage. Única fuente de la verdad: la usan el
 * servicio (default del request) y la vista (etiqueta + tamaño de la serie). No hardcodear el número
 * en otros lados.
 */
export const ACTIVIDAD_MENSUAL_MESES = 12;

/**
 * Rol **efectivo** del usuario (solo lectura): un rol que tiene, ya sea asignado **directamente**
 * o **heredado de un grupo**. Viene de `GET usuarios/{id}/roles-efectivos` (vista
 * `vw_UsuarioRolesEfectivos`). Un mismo rol puede aparecer repetido con distinto origen.
 */
/**
 * Texto reutilizable que describe cómo se componen los roles de un usuario (directos + grupos,
 * acotados por la licencia). Pensado como valor compartido por los lugares donde se explique el
 * modelo (hoy: tab "Roles efectivos"; a futuro, donde haga falta).
 */
export const ROLES_USUARIO_DESCRIPCION =
  'Los roles del usuario son: Roles asignados directamente al usuario + Roles heredados de sus grupos, siempre limitados por su licencia.';

export interface RolEfectivo {
  rolId: number;
  rolDescripcion: string;
  origen: 'Directo' | 'Grupo';
  grupoId?: number | null;
  grupoNombre?: string | null;
  /**
   * `true` si el rol está habilitado por la **licencia activa** del usuario. Si es `false`, el rol
   * está **bloqueado por licencia**: la API no lo cuenta como efectivo (la licencia es tope duro),
   * y la UI lo muestra marcado. La licencia es el techo de lo que el usuario puede hacer.
   */
  permitidoPorLicencia: boolean;
}
