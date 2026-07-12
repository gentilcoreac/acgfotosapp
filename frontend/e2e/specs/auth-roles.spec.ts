import { test, expect } from '@playwright/test';
import { loginAs } from '../fixtures/auth';
import { LoginPage } from '../pages/login.page';
import { ShellPage } from '../pages/shell.page';
import { ROOT, USER_B, USER_SIN_LIC } from '../fixtures/test-data';

/**
 * Catálogo: `Docs/e2e/casos-e2e.md` → grupos A (AUTH-02/03) y B (AUTHZ).
 *
 * Requieren la base de tests dedicada `AcgFotos_TestE2E` con usuarios no-root (ver
 * `Docs/e2e/base-de-tests.md`): `userb` (no-root tenant 2, licencia adminCliente + Rol 2 → menú
 * acotado a "Administración Cliente") y `usersinlic` (no-root tenant 2, sin licencia).
 */

test('E2E-AUTH-02 login no-root con licencia → entra como no-root (menú acotado)', async ({
  page,
}) => {
  await loginAs(page, USER_B);
  const shell = new ShellPage(page);
  await shell.expectLoggedIn();
  await expect(shell.rootRole).toBeHidden(); // no es root
  await expect(shell.impersonateButton).toBeHidden(); // no-root no impersona
  await expect(shell.navLinks.first()).toBeVisible(); // menú no vacío
});

test('E2E-AUTH-03 login no-root SIN licencia → bloqueado, queda en /login', async ({ page }) => {
  const login = new LoginPage(page);
  await login.goto();
  await login.login(USER_SIN_LIC.username, USER_SIN_LIC.password);
  await login.expectError(); // mensaje de licencia; no entra
  expect(page.url()).toContain('/login');
});

test('E2E-AUTHZ-01 ruta no permitida por URL directa → página de acceso denegado', async ({
  page,
}) => {
  await loginAs(page, USER_B);
  await page.goto('/endpoints'); // PermisoRoot: userb no lo tiene
  // El allowedRoutesGuard redirige a /sin-permisos (no al home): le explica al usuario por qué no llegó.
  await page.waitForURL((url) => url.pathname === '/sin-permisos');
  await expect(page.getByTestId('acceso-denegado-page')).toBeVisible();
  await expect(new ShellPage(page).logoutButton).toBeVisible(); // sigue logueado
});

test('E2E-AUTHZ-02 el menú solo trae ítems permitidos (no los root-only)', async ({ page }) => {
  await loginAs(page, USER_B);
  const shell = new ShellPage(page);
  // userb (Rol Administrador Cliente) ve "Administración Cliente" (Usuarios), NO los menús root-only.
  await expect(shell.navLink('/usuarios')).toBeVisible();
  await expect(shell.navLink('/tenants')).toHaveCount(0);
  await expect(shell.navLink('/roles')).toHaveCount(0);
  await expect(shell.navLink('/permisos')).toHaveCount(0);
});

test('E2E-AUTHZ-03 /principal falla (429) → no muestra el catálogo completo', async ({ page }) => {
  await page.route('**/menus/principal*', (route) =>
    route.fulfill({ status: 429, contentType: 'application/json', body: '{}' }),
  );
  await loginAs(page, ROOT);
  // Con principal caído, el menú cae al fallback (no el set completo) → los ítems del catálogo no están.
  await expect(new ShellPage(page).navLink('/tenants')).toHaveCount(0);
});
