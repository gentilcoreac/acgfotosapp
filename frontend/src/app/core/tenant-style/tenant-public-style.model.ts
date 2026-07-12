/**
 * Estilo público del tenant (respuesta de `GET api/general/tenants/public-style/{valueToFilter}`,
 * `[AllowAnonymous]`). Mismo shape que el `GetTenantPublicStyleOutput` de la API.
 *
 * Las URLs de logos/favicon/fondos vienen **absolutas** (`http(s)://host:port/...`) → se usan directo.
 * Los campos de Office (`colorHeaderSheet`, etc.) no aplican a este front y no se modelan.
 */
export interface TenantPublicStyle {
  codigo: string;
  tituloWeb: string | null;
  hostName: string | null;

  // Colores de marca (hex). Generan la paleta M3 en runtime (ver theme-palette).
  colorPrimarioDark: string | null;
  colorPrimarioLight: string | null;
  darkModeByDefault: boolean;

  // Logos / favicon / fondos (URLs absolutas).
  logoLoginLightUrl: string | null;
  logoLoginDarkUrl: string | null;
  logoHeaderLightUrl: string | null;
  logoHeaderDarkUrl: string | null;
  faviconUrl: string | null;
  imagenFondoLoginLightUrl: string | null;
  imagenFondoLoginDarkUrl: string | null;

  // CSS custom del tenant (escape hatch; se consume en Fase 2b).
  styleSheetCssUrl: string | null;

  // Layout del login (0 Centered / 1 Right / 2 Left). Se consume en Fase 2b.
  tipoLayoutLogin: number;
}
