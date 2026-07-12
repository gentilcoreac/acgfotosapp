import {
  DynamicColor,
  Hct,
  MaterialDynamicColors,
  SchemeTonalSpot,
  argbFromHex,
  hexFromArgb,
} from '@material/material-color-utilities';

/**
 * Tokens de color del sistema M3 que emite `mat.theme()`
 * (ver `@angular/material/core/tokens/m3/_md-sys-color.scss`). Son la fuente de verdad en kebab; el
 * accessor de `MaterialDynamicColors` se deriva a camelCase (`on-primary-container` → `onPrimaryContainer`).
 */
const SYS_COLOR_TOKENS = [
  'primary',
  'on-primary',
  'primary-container',
  'on-primary-container',
  'inverse-primary',
  'primary-fixed',
  'primary-fixed-dim',
  'on-primary-fixed',
  'on-primary-fixed-variant',
  'secondary',
  'on-secondary',
  'secondary-container',
  'on-secondary-container',
  'secondary-fixed',
  'secondary-fixed-dim',
  'on-secondary-fixed',
  'on-secondary-fixed-variant',
  'tertiary',
  'on-tertiary',
  'tertiary-container',
  'on-tertiary-container',
  'tertiary-fixed',
  'tertiary-fixed-dim',
  'on-tertiary-fixed',
  'on-tertiary-fixed-variant',
  'error',
  'on-error',
  'error-container',
  'on-error-container',
  'background',
  'on-background',
  'surface',
  'on-surface',
  'surface-variant',
  'on-surface-variant',
  'surface-dim',
  'surface-bright',
  'surface-container-lowest',
  'surface-container-low',
  'surface-container',
  'surface-container-high',
  'surface-container-highest',
  'outline',
  'outline-variant',
  'inverse-surface',
  'inverse-on-surface',
  'shadow',
  'scrim',
  'surface-tint',
] as const;

/** Indexable view de `MaterialDynamicColors` (sus métodos de rol devuelven un `DynamicColor`). */
type DynamicColorRoles = Record<string, () => DynamicColor>;

function kebabToCamel(token: string): string {
  return token.replace(/-([a-z])/g, (_, c: string) => c.toUpperCase());
}

/** Esquema M3 "tonal spot" (el default de Material) desde un color semilla, en light o dark. */
function schemeFromSeed(seedHex: string, isDark: boolean): SchemeTonalSpot {
  return new SchemeTonalSpot(Hct.fromInt(argbFromHex(seedHex)), isDark, 0);
}

/**
 * Genera el set de variables CSS `--mat-sys-*` de color para el tenant a partir de sus dos colores
 * de marca: un esquema M3 **light** desde `lightHex` y uno **dark** desde `darkHex`. Cada token sale
 * como `light-dark(claro, oscuro)`, igual que los emite `mat.theme()`, para que el toggle del
 * `ThemeStore` (que setea `color-scheme` en `<html>`) resuelva el valor correcto sin recalcular nada.
 *
 * El resultado se setea en `documentElement.style` (inline gana sobre el stylesheet base) → recolorea
 * todos los componentes Material de forma coherente (no solo el primario, como hacía el original).
 */
export function buildTenantSysVars(lightHex: string, darkHex: string): Record<string, string> {
  const light = schemeFromSeed(lightHex, false);
  const dark = schemeFromSeed(darkHex, true);
  const roles = new MaterialDynamicColors() as unknown as DynamicColorRoles;

  const vars: Record<string, string> = {};
  for (const token of SYS_COLOR_TOKENS) {
    const color = roles[kebabToCamel(token)]();
    const lightHexValue = hexFromArgb(color.getArgb(light));
    const darkHexValue = hexFromArgb(color.getArgb(dark));
    vars[`--mat-sys-${token}`] = `light-dark(${lightHexValue}, ${darkHexValue})`;
  }
  return vars;
}
