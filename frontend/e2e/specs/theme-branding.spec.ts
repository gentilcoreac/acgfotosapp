import { test, expect } from '@playwright/test';
import { loginAs } from '../fixtures/auth';
import { LoginPage } from '../pages/login.page';
import { ShellPage } from '../pages/shell.page';
import { ImpersonationDialogPage } from '../pages/impersonation-dialog.page';
import { ROOT, USER_B } from '../fixtures/test-data';

/**
 * Catálogo: `Docs/e2e/casos-e2e.md` → grupo G (branding por tenant). Requiere `AcgFotos_TestE2E` con el
 * **tenant 2 brandeado** (colores #7B1FA2 + logos `tenant-b-*`) y el tenant root con su logo `acgfotos.com`.
 * Las URLs de logo no existen (404); se afirma el **atributo src** (el binding), no que la imagen cargue.
 */

test('E2E-THEME-01 tema del tenant aplicado al loguear (logo + colores)', async ({ page }) => {
  await loginAs(page, USER_B); // userb es del tenant 2 (brandeado)
  const shell = new ShellPage(page);

  // Logo del header = el del tenant.
  await expect(shell.headerLogo).toHaveAttribute('src', /tenant-b-header/);
  // Override de paleta del tenant aplicado inline en <html> (--mat-sys-*).
  await expect
    .poll(() => page.evaluate(() => document.documentElement.style.getPropertyValue('--mat-sys-primary').trim()))
    .not.toBe('');
});

test('E2E-THEME-02 branding del login por tenant (?tenant=)', async ({ page }) => {
  await page.goto('/login?tenant=tenant-b'); // resuelve el estilo público del tenant 2
  await expect(new LoginPage(page).logo).toHaveAttribute('src', /tenant-b-login/);
});

test('E2E-THEME-03 re-tema al impersonar y revierte al salir', async ({ page }) => {
  await loginAs(page, ROOT);
  const shell = new ShellPage(page);
  await expect(shell.headerLogo).toHaveAttribute('src', /acgfotos\.com/); // branding root

  await shell.impersonateButton.click();
  const dialog = new ImpersonationDialogPage(page);
  await dialog.selectTenant('Tenant B');
  await dialog.selectUser('userb');
  await dialog.confirm();
  // El re-tema (getHeaderStyle del destino) es async; margen amplio por la latencia bajo carga.
  await expect(shell.headerLogo).toHaveAttribute('src', /tenant-b-header/, { timeout: 20_000 }); // destino

  await shell.stopImpersonationButton.click();
  await expect(shell.headerLogo).toHaveAttribute('src', /acgfotos\.com/, { timeout: 20_000 }); // vuelve a root
});
