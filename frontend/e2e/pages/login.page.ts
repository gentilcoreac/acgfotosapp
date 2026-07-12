import { type Page, type Locator, expect } from '@playwright/test';

/**
 * Page Object del login. Selectores semánticos: `tbi-text-field` expone su `mat-label`
 * (→ `getByLabel`) y `tbi-button` renderiza un `<button>` con el label como texto (→ `getByRole`).
 */
export class LoginPage {
  readonly userName: Locator;
  readonly password: Locator;
  readonly submit: Locator;
  readonly errors: Locator;
  /** Logo de login del tenant (cuando hay branding por dominio/`?tenant=`). */
  readonly logo: Locator;

  constructor(private readonly page: Page) {
    this.userName = page.getByLabel('Usuario');
    this.password = page.getByLabel('Contraseña');
    this.submit = page.getByRole('button', { name: 'Ingresar' });
    this.errors = page.getByTestId('login-errors');
    this.logo = page.getByTestId('login-logo');
  }

  async goto(): Promise<void> {
    await this.page.goto('/login');
    await expect(this.submit).toBeVisible();
  }

  async login(username: string, password: string): Promise<void> {
    await this.userName.fill(username);
    await this.password.fill(password);
    await this.submit.click();
  }

  /** El login muestra los errores de credenciales en una lista dentro del card. */
  async expectError(): Promise<void> {
    await expect(this.errors).toBeVisible();
  }
}
