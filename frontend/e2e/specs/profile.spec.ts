import { expect, test } from '@playwright/test';
import { loginAs } from '../fixtures/auth';
import { ROOT } from '../fixtures/test-data';

/**
 * Catálogo: `Docs/e2e/casos-e2e.md` → grupo "Perfil" (self-service). Smoke fino: la pantalla es
 * alcanzable desde el avatar del shell, muestra los datos propios, guarda cambios y valida el cambio
 * de contraseña. El detalle del contrato lo cubren los specs de componente/servicio.
 */
test.describe('Perfil', () => {
  test.beforeEach(async ({ page }) => {
    await loginAs(page, ROOT);
  });

  test('E2E-PROFILE-01 desde el avatar del shell se llega a /perfil con los datos propios', async ({
    page,
  }) => {
    await page.getByTestId('shell-profile-link').click();
    await page.waitForURL((url) => url.pathname === '/perfil');
    await expect(page.getByTestId('profile-page')).toBeVisible();
    await expect(page.getByTestId('profile-user-name')).toContainText('root');
  });

  test('E2E-PROFILE-02 editar el teléfono y guardar muestra confirmación', async ({ page }) => {
    await page.goto('/perfil');
    await expect(page.getByTestId('profile-page')).toBeVisible();
    // Valor determinista → idempotente entre corridas.
    await page.getByLabel('Teléfono').fill('3410000000');
    await page.getByRole('button', { name: 'Guardar' }).click();
    await expect(page.locator('.tbi-snack--success')).toBeVisible();
  });

  test('E2E-PROFILE-03 cambiar contraseña valida que las nuevas coincidan', async ({ page }) => {
    await page.goto('/perfil');
    await page.getByLabel('Contraseña actual').fill('cualquiera');
    await page.getByLabel('Nueva contraseña', { exact: true }).fill('nueva123');
    await page.getByLabel('Repetir nueva contraseña').fill('distinta123');
    await page.getByRole('button', { name: 'Cambiar contraseña' }).click();
    await expect(page.getByTestId('profile-password-mismatch')).toBeVisible();
    // No se cambió nada: seguimos en /perfil.
    expect(new URL(page.url()).pathname).toBe('/perfil');
  });
});
