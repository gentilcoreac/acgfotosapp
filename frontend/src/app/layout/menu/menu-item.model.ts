/**
 * Ítem de navegación del sidenav. `icon` es un nombre de Material Icon. El menú real se construye
 * desde el backend (`menus/principal`, por permisos del usuario y aplicación activa); este tipo es
 * la forma ya aplanada que consume el sidenav.
 */
export interface MenuItem {
  readonly label: string;
  readonly icon: string;
  readonly route: string;
}

/**
 * Sección del sidenav: un encabezado opcional + sus ítems. La agrupación es **data-driven**: sale
 * del menú padre de primer nivel del árbol del backend (`MenuPadreId = NULL`), no de un mapa
 * hardcodeado en el front. Así, crear/renombrar/reordenar secciones se hace en `gen_Menus` y el
 * sidenav se adecúa solo (los menús son dinámicos). `label = null` ⇒ sin encabezado (p. ej. Inicio).
 */
export interface MenuSection {
  readonly label: string | null;
  readonly items: readonly MenuItem[];
}

/**
 * Nodo del menú que devuelve la API (`GET general/menus/principal`, árbol por permisos del usuario).
 * `imagenWeb` es un Material Icon; `routePath` es la ruta del front; `menuHijos` los hijos.
 */
export interface MenuNode {
  readonly id: number;
  readonly codigo: string;
  readonly nombre: string;
  readonly imagenWeb: string | null;
  readonly routePath: string | null;
  readonly orden: number;
  readonly menuHijos: MenuNode[];
}

/**
 * Puente de etiquetas por `codigo` de menú: el `nombre` del backend es la **clave i18n**
 * (p. ej. `TypesLicenses`, `Log`), y el front nuevo todavía no tiene i18n (Fase 4). Hasta entonces,
 * mostramos estas etiquetas; si el código no está acá, se cae al `nombre` crudo.
 */
export const MENU_LABELS: Readonly<Record<string, string>> = {
  // Encabezados de sección (menús padre nivel-0). Hoy el seed los nombra con códigos i18n
  // (`AcgFotosSettings`, `ApplicationSettings`, `Administracion`); los puenteamos a un nombre legible
  // hasta que se ajusten en `gen_Menus`. Si el código no está acá, se cae al `nombre` crudo.
  AcgFotosSettings: 'Organización',
  ApplicationSettings: 'Administración',
  Administracion: 'Gestión',
  Tenants: 'Tenants',
  Menus: 'Menús',
  Permisos: 'Permisos',
  Roles: 'Roles',
  Parametros: 'Parámetros',
  ParametroValorTenant: 'Parámetros por tenant',
  Aplicaciones: 'Aplicaciones',
  TiposLicencias: 'Tipos de licencia',
  Usuarios: 'Usuarios',
  UsuariosRoot: 'Usuarios',
  Grupos: 'Grupos',
  Endpoints: 'Endpoints',
};
