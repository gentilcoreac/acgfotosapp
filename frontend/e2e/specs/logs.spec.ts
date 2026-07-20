import { expect, test } from '@playwright/test';
import { loginAs } from '../fixtures/auth';
import { ROOT } from '../fixtures/test-data';

/**
 * Catálogo: `Docs/e2e/casos-e2e.md` → grupo "Logs". Pantalla root cross-tenant (`logInfo/AllTenants`).
 * El seed (e2e-extras) siembra 2 filas de log en tenants distintos.
 */
test.describe('Logs', () => {
  test.beforeEach(async ({ page }) => {
    await loginAs(page, ROOT);
    await page.goto('/logs');
  });

  test('E2E-LOG-01 carga el log de aplicación con filas', async ({ page }) => {
    await expect(page.getByRole('heading', { name: 'Logs' })).toBeVisible();
    await expect(page.getByTestId('table-row').first()).toBeVisible();
  });

  test('E2E-LOG-02 "Ver detalle" abre el diálogo con la entrada', async ({ page }) => {
    const firstRow = page.getByTestId('table-row').first();
    await firstRow.hover();
    await firstRow.getByRole('button', { name: 'Ver detalle' }).click();
    await expect(page.getByTestId('log-detail')).toBeVisible();
    await expect(page.getByRole('heading', { name: 'Detalle del log' })).toBeVisible();
  });

  test('E2E-LOG-03 buscar por mensaje filtra el listado (server-side)', async ({ page }) => {
    // Los 2 logs sembrados: "Error de prueba E2E (tenant 1)" y "Aviso de prueba E2E (tenant 2)".
    await page.getByTestId('table-search').fill('Aviso');
    const rows = page.getByTestId('table-row');
    await expect(rows).toHaveCount(1);
    await expect(rows.first()).toContainText('Aviso');
  });

  test('E2E-LOG-04 el botón Filtrar (nivel) recarga; Limpiar restaura', async ({ page }) => {
    // Conteo relativo, no fijo: LogInfo (cross-tenant) acumula filas durante la corrida de la suite.
    const rows = page.getByTestId('table-row');
    await expect(rows.first()).toBeVisible(); // count() no auto-espera: esperar la carga antes de contar
    const totalInicial = await rows.count();

    // Filtrar por nivel Warning → queda el Warning sembrado (tenant 2, "Aviso...").
    await page.getByLabel('Nivel').click();
    await page.getByRole('option', { name: 'Warning' }).click();
    await page.getByRole('button', { name: 'Filtrar' }).click();
    await expect(rows.filter({ hasText: 'Aviso' })).toHaveCount(1);
    await expect(rows).not.toHaveCount(totalInicial);

    // Limpiar restaura el listado completo.
    await page.getByRole('button', { name: 'Limpiar' }).click();
    await expect(rows).toHaveCount(totalInicial);
  });
});
