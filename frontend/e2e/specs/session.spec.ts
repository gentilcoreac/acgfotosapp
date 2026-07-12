import { test, expect } from '@playwright/test';
import { loginAs } from '../fixtures/auth';
import { LoginPage } from '../pages/login.page';
import { ShellPage } from '../pages/shell.page';
import { ROOT } from '../fixtures/test-data';

/**
 * Catálogo: `Docs/e2e/casos-e2e.md` → grupo A (sesión/guards/restauración).
 * Specs que necesitan sesión: login fresco por test (ver `fixtures/auth.ts`).
 */
test.beforeEach(async ({ page }) => {
  await loginAs(page, ROOT);
});

test('E2E-AUTH-06 anonGuard: logueado en /login → redirige al home', async ({ page }) => {
  await page.goto('/login');

  const shell = new ShellPage(page);
  await expect(shell.logoutButton).toBeVisible();
  expect(page.url()).not.toContain('/login');
});

test('E2E-AUTH-08 logout limpia la sesión y vuelve a /login', async ({ page }) => {
  await new ShellPage(page).logout();

  await page.waitForURL('**/login');
  await expect(new LoginPage(page).submit).toBeVisible();

  // La sesión quedó limpia: una ruta protegida ya no entra (el refresh fue revocado).
  await page.goto('/usuarios');
  await page.waitForURL('**/login');
});

test('E2E-AUTH-09 F5 restaura la sesión (refresh OK), sin re-login', async ({ page }) => {
  await page.reload();

  const shell = new ShellPage(page);
  await expect(shell.logoutButton).toBeVisible();
  expect(page.url()).not.toContain('/login');
});

test('E2E-AUTH-10 F5 con refresh 429 → reconexión y recupera (no patea a login)', async ({ page }) => {
  // El primer refresh (el del bootstrap tras el F5) devuelve 429; los reintentos de la pantalla
  // de reconexión pasan de largo (refresh real) → la app se recupera y entra, sin ir a /login.
  let refreshCount = 0;
  await page.route('**/auth/refresh', async (route) => {
    refreshCount++;
    if (refreshCount === 1) {
      await route.fulfill({
        status: 429,
        contentType: 'application/json',
        body: JSON.stringify({ message: 'Demasiadas solicitudes' }),
      });
    } else {
      await route.continue();
    }
  });

  await page.reload();

  const shell = new ShellPage(page);
  await expect(shell.logoutButton).toBeVisible();
  expect(page.url()).not.toContain('/login');
  expect(refreshCount).toBeGreaterThanOrEqual(2); // hubo 429 + reintento que recuperó
});
