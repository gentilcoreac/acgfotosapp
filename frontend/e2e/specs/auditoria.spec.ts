import { expect, test } from '@playwright/test';
import { loginAs } from '../fixtures/auth';
import { ROOT } from '../fixtures/test-data';
import { TablePage } from '../pages/table.page';

/**
 * Catálogo: `Docs/e2e/casos-e2e.md` → grupo "Auditoría". Smoke fino del listado read-only:
 * carga, detalle por fila y filtro. El detalle del contrato lo cubren los specs de componente.
 */
test.describe('Auditoría', () => {
  test.beforeEach(async ({ page }) => {
    await loginAs(page, ROOT);
    await page.goto('/auditoria');
  });

  test('E2E-AUD-01 carga el listado con filtros y filas', async ({ page }) => {
    await expect(page.getByRole('heading', { name: 'Auditoría' })).toBeVisible();
    await expect(page.getByLabel('Desde')).toBeVisible();
    await expect(page.getByLabel('Servicio')).toBeVisible();
    await expect(page.getByTestId('table-row').first()).toBeVisible();
  });

  test('E2E-AUD-02 "Ver detalle" abre el diálogo con el registro', async ({ page }) => {
    const firstRow = page.getByTestId('table-row').first();
    await firstRow.hover();
    await firstRow.getByRole('button', { name: 'Ver detalle' }).click();
    await expect(page.getByTestId('auditoria-detail')).toBeVisible();
    await expect(page.getByRole('heading', { name: 'Detalle de auditoría' })).toBeVisible();
  });

  test('E2E-AUD-03 filtrar por un status inexistente vacía el listado y Limpiar lo restaura', async ({
    page,
  }) => {
    const table = new TablePage(page);
    await page.getByLabel('Resp. HTTP').fill('999');
    await page.getByRole('button', { name: 'Filtrar' }).click();
    await expect(table.emptyState).toBeVisible();
    await page.getByRole('button', { name: 'Limpiar' }).click();
    await expect(page.getByTestId('table-row').first()).toBeVisible();
  });

  test('E2E-AUD-04 filtrar por rango de fechas (Desde = hoy) reduce los resultados', async ({
    page,
  }) => {
    await expect(page.getByTestId('table-row').first()).toBeVisible();
    const totalSinFiltro = await leerTotal(page);
    // Abre el calendario del primer date-picker ("Desde") y elige el día de hoy.
    await page.locator('tbi-date-picker').first().getByRole('button').click();
    await page.locator('.mat-calendar-body-today').click();
    await page.getByRole('button', { name: 'Filtrar' }).click();
    // Hoy siempre tiene menos registros que toda la historia → el total baja (filtro aplicado).
    await expect.poll(() => leerTotal(page)).toBeLessThan(totalSinFiltro);
  });
});

/** Total de registros del paginator ("1 – 10 de 8234" → 8234). */
async function leerTotal(page: import('@playwright/test').Page): Promise<number> {
  const label = (await page.locator('.mat-mdc-paginator-range-label').innerText()).trim();
  const nums = label.match(/\d+/g);
  return nums ? Number(nums[nums.length - 1]) : NaN;
}
