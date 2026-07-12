import { test, expect } from '@playwright/test';
import { loginAs } from '../fixtures/auth';
import { ShellPage } from '../pages/shell.page';
import { ROOT } from '../fixtures/test-data';

/**
 * Catálogo: `Docs/e2e/casos-e2e.md` → grupo G (Theming / branding).
 *
 * Estos dos casos prueban que el theming **se aplica una vez logueado** y que el toggle de modo
 * funciona, contra el entorno actual (root). El branding de un tenant CONCRETO (colores/logo propios,
 * re-tema al impersonar = THEME-01/02/03) necesita el seed dedicado con un tenant con branding → ver
 * `Docs/e2e/README.md` §5/§9.
 */
test.beforeEach(async ({ page }) => {
  await loginAs(page, ROOT);
});

test('E2E-THEME-04 theming aplicado tras login (M3 + favicon + título + brand)', async ({ page }) => {
  // 1) Variables de sistema M3 activas (los estilos se cargaron).
  const primary = await page.evaluate(() =>
    getComputedStyle(document.documentElement).getPropertyValue('--mat-sys-primary').trim(),
  );
  expect(primary).not.toBe('');

  // 2) color-scheme aplicado en <html> (light | dark).
  const scheme = await page.evaluate(() => document.documentElement.style.colorScheme);
  expect(['light', 'dark']).toContain(scheme);

  // 3) Favicon (imagen) presente con href.
  await expect(page.locator('link[rel~="icon"]').first()).toHaveAttribute('href', /.+/);

  // 4) Título de la pestaña seteado.
  await expect(page).toHaveTitle(/.+/);

  // 5) Marca en el toolbar: logo (imagen) o título de texto.
  await expect(new ShellPage(page).brand).toBeVisible();
});

test('E2E-THEME-05 toggle claro/oscuro cambia el color-scheme', async ({ page }) => {
  const shell = new ShellPage(page);
  const before = await page.evaluate(() => document.documentElement.style.colorScheme);

  await shell.themeToggle.click();

  await expect.poll(() => page.evaluate(() => document.documentElement.style.colorScheme)).not.toBe(before);
  // El toggle sigue presente reflejando el nuevo modo (su aria-label alterna).
  await expect(shell.themeToggle).toBeVisible();
});
