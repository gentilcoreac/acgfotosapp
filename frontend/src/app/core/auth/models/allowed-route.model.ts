/**
 * Ruta que el usuario logueado puede navegar, según sus permisos (menú). La devuelve
 * `GET api/general/menus/allowed-routes` y la consume `allowedRoutesGuard` para autorizar
 * la navegación del front.
 *
 * `routePath` es la ruta del front (p. ej. `/menus`), unificada en el backend con las rutas
 * registradas acá; las entradas contenedoras (sin pantalla propia) traen `routePath` nulo.
 */
export interface AllowedRoute {
  readonly id: number;
  readonly nombre: string;
  readonly codigo: string;
  readonly routePath: string | null;
}
