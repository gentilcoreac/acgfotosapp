import { test, expect } from '@playwright/test';
import { loginAs } from '../fixtures/auth';
import { ShellPage } from '../pages/shell.page';
import { USER_B, USER_C } from '../fixtures/test-data';

/**
 * Catálogo: `Docs/e2e/casos-e2e.md` → grupo C (Aplicación activa). El selector de app es **solo para
 * no-root**. Requiere `AcgFotos_TestE2E`: `userb` multi-app (General + Aplicacion B) y `userc` single-app.
 * APP-04 (root: header no vacío → menú carga) queda cubierto por AUTH-01.
 */

test('E2E-APP-01 switch de app: refleja la app nueva y recarga el menú', async ({ page }) => {
  await loginAs(page, USER_B);
  const shell = new ShellPage(page);
  await expect(shell.appSelector).toBeVisible(); // multi-app → selector visible
  await expect(shell.appSelector).toContainText('General'); // app por defecto

  let principalCount = 0;
  page.on('request', (r) => {
    if (r.url().includes('/menus/principal')) principalCount++;
  });

  await shell.appSelector.click();
  await page.getByRole('option', { name: 'Aplicacion B' }).click();

  await expect(shell.appSelector).toContainText('Aplicacion B'); // refleja la nueva
  await expect.poll(() => principalCount).toBeGreaterThanOrEqual(1); // el menú recargó
});

test('E2E-APP-02 la app seleccionada persiste en F5', async ({ page }) => {
  await loginAs(page, USER_B);
  const shell = new ShellPage(page);
  await shell.appSelector.click();
  await page.getByRole('option', { name: 'Aplicacion B' }).click();
  await expect(shell.appSelector).toContainText('Aplicacion B');

  await page.reload();

  await expect(shell.appSelector).toContainText('Aplicacion B'); // persistió (localStorage)
});

test('E2E-APP-03 usuario single-app no ve el selector', async ({ page }) => {
  await loginAs(page, USER_C);
  await expect(new ShellPage(page).appSelector).toBeHidden();
});
