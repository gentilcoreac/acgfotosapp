import { type Page, type Locator, expect } from '@playwright/test';

/**
 * Page Object del diálogo de impersonalización (root → elegir tenant → elegir usuario → confirmar).
 * El diálogo usa `mat-select` nativos (no `tbi-select`); las opciones se renderizan en un overlay del
 * CDK a nivel de `page` (fuera del diálogo), por eso los `getByRole('option')` van sobre `page`.
 */
export class ImpersonationDialogPage {
  readonly dialog: Locator;
  readonly tenantSelect: Locator;
  readonly userSelect: Locator;
  readonly confirmButton: Locator;
  readonly error: Locator;

  constructor(private readonly page: Page) {
    this.dialog = page.getByRole('dialog');
    this.tenantSelect = this.dialog.getByLabel('Tenant');
    this.userSelect = this.dialog.getByLabel('Usuario');
    this.confirmButton = this.dialog.getByRole('button', { name: 'Impersonalizar' });
    this.error = this.dialog.getByTestId('impersonate-error');
  }

  async selectTenant(nombre: string): Promise<void> {
    await this.openAndPick(this.tenantSelect, nombre);
  }

  async selectUser(userName: string): Promise<void> {
    // El select de usuario se habilita recién cuando cargaron los usuarios del tenant.
    await this.openAndPick(this.userSelect, new RegExp(userName));
  }

  /**
   * Abre un `mat-select` y elige una opción, robusto ante la animación de apertura del diálogo:
   * espera que el diálogo esté visible y que el panel (listbox) abra antes de clickear la opción.
   */
  private async openAndPick(select: Locator, name: string | RegExp): Promise<void> {
    await expect(this.dialog).toBeVisible();
    await select.click();
    await expect(this.page.getByRole('listbox')).toBeVisible();
    await this.page.getByRole('option', { name }).click();
  }

  /** Confirma e impersonaliza; el diálogo se cierra al resolver el swap de contexto. */
  async confirm(): Promise<void> {
    await this.confirmButton.click();
    await expect(this.dialog).toBeHidden();
  }
}
