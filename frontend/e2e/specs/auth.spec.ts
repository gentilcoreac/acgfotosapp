import { test, expect } from '@playwright/test';
import { LoginPage } from '../pages/login.page';
import { ShellPage } from '../pages/shell.page';
import { ROOT } from '../fixtures/test-data';

/**
 * Catálogo: `Docs/e2e/casos-e2e.md` → grupo A (Autenticación y arranque).
 * Specs que arrancan **anónimos** (sin sesión): cada test parte de un contexto limpio (no hay
 * storageState por defecto en la config).
 */

test('E2E-AUTH-01 login root → home + menú completo', async ({ page }) => {
  const login = new LoginPage(page);
  await login.goto();
  await login.login(ROOT.username, ROOT.password);

  const shell = new ShellPage(page);
  await shell.expectLoggedIn();
  await shell.expectRootContext();
});

test('E2E-AUTH-04 credenciales inválidas → error visible, no navega', async ({ page }) => {
  const login = new LoginPage(page);
  await login.goto();
  await login.login(ROOT.username, 'clave-incorrecta-xyz');

  await login.expectError();
  expect(page.url()).toContain('/login');
});

test('E2E-AUTH-05 submit vacío → marca "Requerido" y NO dispara request de login', async ({ page }) => {
  // Espía: ningún POST a auth/token debe salir si el formulario es inválido.
  let tokenPosted = false;
  page.on('request', (req) => {
    if (req.method() === 'POST' && req.url().includes('/auth/token')) {
      tokenPosted = true;
    }
  });

  const login = new LoginPage(page);
  await login.goto();
  await login.submit.click(); // submit con el form vacío

  // El feedback de validación aparece (fix OnPush + CVA en tbi-text-field: ambos campos requeridos).
  await expect(page.getByText('Requerido')).toHaveCount(2);
  expect(page.url()).toContain('/login'); // no navega
  expect(tokenPosted).toBe(false); // no pega a la API
});

test('E2E-AUTH-07 ruta protegida sin sesión → redirige a /login', async ({ page }) => {
  await page.goto('/usuarios');

  await page.waitForURL('**/login');
  await expect(new LoginPage(page).submit).toBeVisible();
});
