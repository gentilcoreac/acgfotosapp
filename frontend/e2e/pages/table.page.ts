import { type Page, type Locator, expect } from '@playwright/test';

/**
 * Page Object del `tbi-table` (buscador server-side, menú de columnas, filas con acciones).
 * Reutilizable por cualquier listado. Las acciones de fila son inline reveladas al hover (desktop),
 * con `aria-label` = la etiqueta de la acción.
 */
export class TablePage {
  readonly searchInput: Locator;
  readonly columnsButton: Locator;
  readonly emptyState: Locator;
  readonly nextPageButton: Locator;
  readonly rangeLabel: Locator;

  constructor(private readonly page: Page) {
    this.searchInput = page.getByTestId('table-search');
    this.columnsButton = page.getByRole('button', { name: 'Columnas' });
    this.emptyState = page.getByTestId('table-empty');
    // El paginator es Material puro (no hay wrapper donde colgar un testid sin tocar su markup
    // interno): se usan sus clases públicas, estables entre upgrades.
    this.nextPageButton = page.locator('.mat-mdc-paginator-navigation-next');
    this.rangeLabel = page.locator('.mat-mdc-paginator-range-label');
  }

  /** Pasa a la página siguiente y espera a que el listado cargue (el rango cambia → fin del fetch). */
  async nextPage(): Promise<void> {
    const before = (await this.rangeLabel.innerText()).trim();
    await this.nextPageButton.click();
    await expect(this.rangeLabel).not.toHaveText(before);
  }

  /**
   * Índice del primer registro mostrado, leído del rango del paginator ("11 – 20 de 19" → 11).
   * Evita acoplar las aserciones a un `pageSize` concreto.
   */
  async rangeStart(): Promise<number> {
    const label = (await this.rangeLabel.innerText()).trim();
    const match = label.match(/\d+/);
    return match ? Number(match[0]) : NaN;
  }

  /** Usuario (2ª columna) de la primera fila de datos visible. */
  async firstRowUserName(): Promise<string> {
    return (await this.page.getByTestId('table-row').first().locator('td').nth(1).innerText()).trim();
  }

  /** Búsqueda libre (server-side, con debounce). */
  async search(text: string): Promise<void> {
    await this.searchInput.fill(text);
  }

  /** Fila que contiene el texto dado (p. ej. un userName). */
  row(text: string): Locator {
    return this.page.getByRole('row').filter({ hasText: text });
  }

  /** Encabezado de columna por su texto. */
  headerCell(text: string): Locator {
    return this.page.getByRole('columnheader', { name: text, exact: true });
  }

  /** Dispara una acción de fila (Editar/Eliminar/…): hover de la fila + click del ícono por aria-label. */
  async rowAction(rowText: string, actionLabel: string): Promise<void> {
    const row = this.row(rowText);
    await row.hover();
    await row.getByRole('button', { name: actionLabel }).click();
  }

  /** Muestra/oculta una columna opcional desde el menú "Columnas" (cierra el menú al terminar). */
  async toggleColumn(header: string): Promise<void> {
    await this.columnsButton.click();
    await this.page.getByRole('menuitem', { name: header }).click();
    await this.page.keyboard.press('Escape'); // el ítem hace stopPropagation → el menú queda abierto
  }
}
