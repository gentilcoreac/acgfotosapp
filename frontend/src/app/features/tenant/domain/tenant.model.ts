import { EditableEntity } from '../../../shared/forms/edit-component-base';

/**
 * Tenant (ABM de `api/general/tenants`, DTOs `TenantHeaderDto`/`TenantDto`/`TenantInputDto`).
 *
 * Análisis de contrato (API actual ↔ ABM original `ClienteOriginal`):
 * - La API es `ExtendedEntityApiController`: el **listado** (`SearchAsync`) devuelve `TenantHeaderDto`
 *   (cabecera liviana); el **detalle** (`getById`) y el **save** trabajan con `TenantDto`/`TenantInputDto`
 *   (shape completo con estilos, archivos, aplicaciones, licencias y —solo en alta— el usuario admin).
 * - El read path de la API se migró a Mapperly (`TenantMapper`) y el write a `SetValues` +
 *   `SyncCollections` (ADR-0001), rama `refactor/tenant-mapperly`. El contrato de DTOs NO cambió.
 * - `Tenant` NO es multi-tenant (es `EntityBase`): root lista/edita **todos** los tenants desde su
 *   propio contexto, sin impersonalización. El ABM es root-only (UX gateada por `AuthStore.isRoot`).
 * - **No hay borrado**: el ciclo es habilitar/deshabilitar vía `activo` (deshabilitar invalida las
 *   sesiones de los usuarios de ese tenant en la API). La lista no expone delete.
 * - Campos del original que **esta API podada ya no tiene** (y por eso se omiten): provisioning de
 *   bases (SQL/SSAS), `currentFilesStoragePath`/`tenantFiles`, y los colores de planilla Office
 *   (`colorHeaderSheet`, `logoHeaderSheetUrl`, etc.).
 * - `hasError`/`errorDescription` son **solo respuesta** (los gestiona el backend en la creación;
 *   no se mandan en el body — la API los preserva).
 */
export interface Tenant extends EditableEntity {
  id?: number;
  // Básicos
  codigo: string;
  nombre: string;
  tituloWeb: string | null;
  activo: boolean;
  hostName: string | null;

  // Colores / modo
  colorPrimarioDark: string | null;
  colorPrimarioLight: string | null;
  darkModeByDefault: boolean;

  // Estilo (URLs persistidas) — los `*Url` representan el recurso ya subido.
  logoLoginLightUrl: string | null;
  logoLoginDarkUrl: string | null;
  logoHeaderLightUrl: string | null;
  logoHeaderDarkUrl: string | null;
  faviconUrl: string | null;
  imagenFondoLoginLightUrl: string | null;
  imagenFondoLoginDarkUrl: string | null;
  styleSheetCssUrl: string | null;

  // Layout del login (0 Centered / 1 Right / 2 Left)
  tipoLayoutLogin: number;

  // Archivos a subir (base64). Sólo viajan cuando se elige uno nuevo; si no, van en null.
  logoLoginLightFile: TenantResource | null;
  logoLoginDarkFile: TenantResource | null;
  logoHeaderLightFile: TenantResource | null;
  logoHeaderDarkFile: TenantResource | null;
  faviconFile: TenantResource | null;
  imagenFondoLoginLightFile: TenantResource | null;
  imagenFondoLoginDarkFile: TenantResource | null;
  /** CSS custom (base64 puro). La API lo recibe como `byte[]`. */
  styleSheetFile: string | null;

  // Negocio
  /** Usuario administrador del tenant. Solo en **alta** (la API lo crea y le manda el mail). */
  usuario?: TenantAdminUsuario;
  tenantAplicaciones: TenantAplicacionRef[];
  tenantLicenses: TenantLicencia[];

  // Solo respuesta
  hasError?: boolean | null;
  errorDescription?: string | null;
}

/** Recurso de archivo (logos/favicon/fondos). Mismo shape que `TenantResourceDto` de la API. */
export interface TenantResource {
  base64: string;
  name: string;
  extension: string;
  mimeType: string;
}

/** Aplicación asignada al tenant (join `gen_TenantAplicaciones`). En el ABM se edita por `aplicacionId`. */
export interface TenantAplicacionRef {
  id?: number;
  aplicacionId: number;
  aplicacionNombre?: string;
  tenantId?: number;
}

/** Licencia del tenant (detalle `gen_TenantLicencias`). Las fechas viajan como ISO. */
export interface TenantLicencia {
  id?: number;
  tenantId?: number;
  tipoLicenciaId: number;
  cantidad: number;
  startDateTime: string;
  expireDateTime: string;
  modifiedDateTime?: string;
}

/** Usuario administrador que se crea junto con el tenant (solo en alta). */
export interface TenantAdminUsuario {
  nombre: string;
  apellido: string;
  email: string;
  userName: string;
  telefono: number | null;
}

/**
 * Administrador del tenant para mostrar **solo lectura** en el edit (endpoint dedicado
 * `GET general/tenants/{id}/administradores`). NO forma parte del add/edit: nunca se envía.
 */
export interface TenantAdminInfo {
  /** Id del usuario admin — target del selector de impersonalización. */
  id: number;
  nombre: string;
  apellido: string;
  userName: string;
  email: string;
}

/** Opción de aplicación para los checkboxes del ABM (`api/general/aplicaciones`). */
export interface AplicacionOption {
  id: number;
  nombre: string;
}

/** Opción de tipo de licencia para el select de la subgrilla (`api/general/tipos-licencia`). */
export interface TipoLicenciaOption {
  id: number;
  descripcion: string;
  /** Licencia que se precarga (y no se puede quitar) al dar de alta un tenant. */
  esDefaultParaNuevoTenant: boolean;
}
