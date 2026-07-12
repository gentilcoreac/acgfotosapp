import { test, expect } from '@playwright/test';
import { loginAs } from '../fixtures/auth';
import { ShellPage } from '../pages/shell.page';
import { ImpersonationDialogPage } from '../pages/impersonation-dialog.page';
import { ROOT } from '../fixtures/test-data';

/**
 * Catálogo: `Docs/e2e/casos-e2e.md` → grupo D (Impersonalización). Requiere `AcgFotos_TestE2E` (root +
 * userb no-root en tenant 2). IMP-05 (ícono solo root / oculto al impersonar) queda cubierto por
 * AUTH-01 (root lo ve) + AUTH-02 (no-root no) + IMP-01 (oculto al impersonar) → no se duplica.
 */
test.beforeEach(async ({ page }) => {
  await loginAs(page, ROOT);
});

/** Abre el diálogo e impersonaliza a userb del Tenant B. */
async function impersonateUserB(page: import('@playwright/test').Page): Promise<void> {
  await new ShellPage(page).impersonateButton.click();
  const dialog = new ImpersonationDialogPage(page);
  await dialog.selectTenant('Tenant B');
  await dialog.selectUser('userb');
  await dialog.confirm();
}

test('E2E-IMP-01 root impersona → banner del destino + menú acotado + ícono oculto', async ({ page }) => {
  await impersonateUserB(page);
  const shell = new ShellPage(page);
  await expect(shell.impersonationBanner).toBeVisible();
  await expect(shell.impersonationBanner).toContainText('userb');
  await expect(shell.impersonateButton).toBeHidden(); // oculto mientras se impersona
  await expect(shell.rootRole).toBeHidden(); // ya no es root
  await expect(shell.navLink('/tenants')).toHaveCount(0); // menú del destino
});

test('E2E-IMP-02 salir de impersonación → root restaurado', async ({ page }) => {
  await impersonateUserB(page);
  const shell = new ShellPage(page);
  await expect(shell.impersonationBanner).toBeVisible();

  await shell.stopImpersonationButton.click();

  await expect(shell.impersonationBanner).toBeHidden();
  await expect(shell.impersonateButton).toBeVisible(); // root de nuevo
  await expect(shell.rootRole).toBeVisible();
});

test('E2E-IMP-03 refresh sostiene la impersonación', async ({ page }) => {
  await impersonateUserB(page);
  const shell = new ShellPage(page);
  await expect(shell.impersonationBanner).toBeVisible();

  await page.reload();

  await expect(shell.impersonationBanner).toBeVisible(); // sigue como userb tras F5
  await expect(shell.impersonationBanner).toContainText('userb');
});

test('E2E-IMP-04 error al cargar usuarios del tenant → mensaje, no queda mudo', async ({ page }) => {
  await page.route('**/auth/impersonation/users/**', (route) =>
    route.fulfill({ status: 500, contentType: 'application/json', body: '{}' }),
  );
  await new ShellPage(page).impersonateButton.click();
  const dialog = new ImpersonationDialogPage(page);
  await dialog.tenantSelect.click();
  await page.getByRole('option', { name: 'Tenant B' }).click();

  await expect(dialog.error).toBeVisible();
});
