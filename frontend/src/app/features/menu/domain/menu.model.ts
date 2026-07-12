import { EditableEntity } from '../../../shared/forms/edit-component-base';

/** Opción de aplicación para los selects/filtros del ABM (listado `api/general/aplicaciones`). */
export interface AplicacionOption {
  id: number;
  nombre: string;
}

/** Permiso de una aplicación (de `aplicaciones/{id}.permisos`), para el select de permiso. */
export interface PermisoDeAplicacion {
  id: number;
  nombre: string;
}

/** Menú de una aplicación, para el select de "menú padre" (filtrado por la app elegida). */
export interface MenuOption {
  id: number;
  nombre: string;
  codigo: string;
}

/**
 * Menú (ABM de `api/general/menus`, DTO `MenuDto`).
 *
 * Análisis de contrato (API actual ↔ ABM original `ClienteOriginal`):
 * - CRUD estándar (`EntityApiControllerBase`): el listado va por `MenuRepository.PaginateWithPermisoAsync`
 *   (filtra por `aplicacionId` opcional e incluye `Permiso` → `permisoNombre`); detalle/guardado/borrado
 *   estándar. Los endpoints `menus/principal`, `menus/dashboard` y `menus/allowed-routes` son del menú
 *   **runtime** (no del ABM) y no se consumen acá.
 * - **`tipoMenuId` / `tipoMenuDescripcion` están muertos**: `MenuDto` los declara pero la entidad `Menu`
 *   (y su EFConfig) no tienen ese campo, así que el mapper los deja en default. El ABM original mostraba
 *   una columna "Type" siempre vacía. Por eso se omiten acá (lista y edit).
 * - **`aplicacionDescripcion` no llega en el listado**: el repo del list sólo hace `.Include(Permiso)`,
 *   no `Aplicacion`, así que en la lista viene null. Se omite como columna; sí queda `permisoNombre`.
 * - **`orden`** se valida `NotEmpty()` en el server (un `long`): la API rechaza `orden = 0`. El form lo
 *   exige entero ≥ 1.
 * - **`aplicacionId`** es nullable en la API, pero el ABM original lo exigía: mantenemos ese criterio
 *   (requerido). `permisoId` y `menuPadreId` son **opcionales** y **dependen de la aplicación** elegida
 *   (mismo patrón dependiente que `parametro`/`permiso`: deshabilitados hasta elegir app, listas filtradas).
 * - `menuHijos` (colección del DTO) es sólo respuesta del árbol; el ABM no la edita ni la envía.
 */
export interface Menu extends EditableEntity {
  id?: number;
  nombre: string;
  codigo: string;
  /** "Activo". */
  estado: boolean;
  /** Ícono (nombre de Material Icon o ruta). Opcional, máx. 100. */
  imagenWeb: string | null;
  /** Orden de despliegue. Entero ≥ 1 (la API lo valida `NotEmpty`). */
  orden: number;
  /** Menú padre (jerarquía). Opcional; se elige entre los menús de la misma aplicación. */
  menuPadreId?: number | null;
  /** Permiso requerido para ver el menú. Opcional; filtrado por la aplicación elegida. */
  permisoId?: number | null;
  /** Sólo respuesta (listado/detalle): nombre del permiso asociado. */
  permisoNombre?: string;
  aplicacionId?: number | null;
  /** Sólo respuesta; **no** viene en el listado (el repo no incluye `Aplicacion`). */
  aplicacionDescripcion?: string;
  /** Visible en el menú lateral (aside). */
  visibleSideMenu: boolean;
  /** Visible como acceso directo en el dashboard/home. */
  visibleDash: boolean;
  /** Ruta de Angular a la que navega el ítem. */
  routePath: string | null;
}

/**
 * Acceso directo del home (`GET menus/dashboard`): un menú marcado `VisibleDash`, ya **filtrado por
 * los permisos del usuario** en el backend. `routePath` null = contenedor sin pantalla (se descarta).
 * El label/ícono se resuelven por `codigo` (igual que el sidenav) vía `MENU_LABELS`/`MENU_ICON_BY_CODE`.
 */
export interface MenuDash {
  id: number;
  codigo: string;
  nombre: string;
  imagenWeb: string | null;
  routePath: string | null;
  orden: number;
}
