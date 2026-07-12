/**
 * Constantes del seed/entorno para los E2E. Sobreescribibles por env var para apuntar a la
 * base/credenciales que corresponda (dev hoy; base de tests dedicada en cuanto esté lista).
 *
 * ⚠️ Las credenciales por defecto son las del seed de DEV (root). Cuando exista la base de tests
 * dedicada (ver `Docs/e2e/README.md` §5), apuntar acá los usuarios del seed de tests:
 * adminB (admin de tenant), userB (no-root con licencia), userSinLic (no-root sin licencia).
 */
export interface Credentials {
  username: string;
  password: string;
}

export const ROOT: Credentials = {
  username: process.env['E2E_ROOT_USER'] ?? 'root',
  password: process.env['E2E_ROOT_PASS'] ?? 'Root@AcgFotos2026!',
};

/**
 * ⏳ PENDIENTES de la base de tests dedicada (hoy NO existen en el seed de dev, que es todo root).
 * Habilitan los specs `test.fixme` de `specs/auth-roles.spec.ts`. Ver `Docs/e2e/base-de-tests.md`.
 *
 * - USER_B = `userb` del `TestSeed.sql` de la API (no-root, tenant 2 activo, con licencia). Password
 *   real del seed: `Root@AcgFotos2026!` (todos los usuarios sembrados comparten ese hash).
 * - USER_SIN_LIC = aún NO existe en ningún seed; hay que sembrar un no-root SIN licencia en un tenant
 *   ACTIVO (userc no sirve: tiene licencia Visualizador activa). Placeholder hasta sembrarlo.
 */
export const USER_B: Credentials = {
  username: process.env['E2E_USERB_USER'] ?? 'userb',
  password: process.env['E2E_USERB_PASS'] ?? 'Root@AcgFotos2026!',
};

export const USER_SIN_LIC: Credentials = {
  username: process.env['E2E_USER_NOLIC_USER'] ?? 'usersinlic',
  password: process.env['E2E_USER_NOLIC_PASS'] ?? 'Root@AcgFotos2026!',
};

/** No-root del tenant 2 con licencia y UNA sola aplicación → no ve el selector de app (APP-03). */
export const USER_C: Credentials = {
  username: process.env['E2E_USERC_USER'] ?? 'userc',
  password: process.env['E2E_USERC_PASS'] ?? 'Root@AcgFotos2026!',
};
