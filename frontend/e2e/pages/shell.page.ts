import { type Page, type Locator, expect } from '@playwright/test';

/**
 * Page Object del layout autenticado (toolbar + sidenav). Encapsula las señales visibles de
 * "estoy logueado y con qué contexto": botón de logout, indicador de rol, menú lateral, banner
 * de impersonación, selector de aplicación.
 *
 * Selectores: se prefiere `getByRole`/`getByLabel` cuando el nombre accesible es estable; para los
 * elementos que antes dependían de clases CSS de layout (`.layout__*`) o de texto/markup interno se
 * usan los `data-testid` de los wrappers `tbi-*`/layout (estables ante refactors de markup y Material).
 */
export class ShellPage {
  readonly logoutButton: Locator;
  readonly impersonateButton: Locator;
  readonly toggleMenuButton: Locator;
  readonly navLinks: Locator;
  readonly userName: Locator;
  readonly rootRole: Locator;
  readonly impersonationBanner: Locator;
  /** Botón "Salir" del banner de impersonación (vuelve a root). */
  readonly stopImpersonationButton: Locator;
  readonly appSelector: Locator;
  /** Marca del toolbar: el logo del tenant (imagen) o, si no hay, el título de texto. */
  readonly brand: Locator;
  /** Logo del header (imagen del tenant), cuando hay branding con logo. */
  readonly headerLogo: Locator;
  /** Botón de modo claro/oscuro del toolbar (su aria-label alterna según el modo activo). */
  readonly themeToggle: Locator;

  constructor(private readonly page: Page) {
    // Logout del shell por testid: la página de acceso denegado (/sin-permisos) también tiene un
    // botón "Cerrar sesión", así que el nombre accesible ya no es único → getByTestId desambigua.
    this.logoutButton = page.getByTestId('logout');
    this.impersonateButton = page.getByRole('button', { name: 'Impersonalizar' });
    this.toggleMenuButton = page.getByRole('button', { name: 'Alternar menú' });
    // Combobox con aria-label (no getByLabel: con el panel abierto el listbox comparte el label).
    this.appSelector = page.getByRole('combobox', { name: 'Aplicación activa' });
    this.navLinks = page.locator('mat-nav-list a[mat-list-item]');
    // Antes dependían de clases de layout / texto interno → ahora data-testid.
    this.userName = page.getByTestId('shell-user-name');
    this.rootRole = page
      .getByTestId('shell-user-role')
      .filter({ hasText: 'Administrador del sistema' });
    this.impersonationBanner = page.getByTestId('impersonation-banner');
    this.stopImpersonationButton = page.getByTestId('stop-impersonation');
    this.headerLogo = page.getByTestId('shell-logo');
    this.brand = page.getByTestId('shell-logo').or(page.getByTestId('shell-title')).first();
    // El aria-label del toggle alterna (claro/oscuro) → el testid es el ancla estable.
    this.themeToggle = page.getByTestId('theme-toggle');
  }

  /** Locator del ítem de menú para una ruta (`/usuarios`, `/tenants`, …). */
  navLink(route: string): Locator {
    return this.page.getByTestId(`nav-${route}`);
  }

  /**
   * Señal mínima e inequívoca de sesión activa: el shell con su botón de logout, fuera de /login.
   * Timeout amplio: el primer login (sobre todo no-root, que resuelve menú+apps) puede ser lento en
   * frío; un login que realmente falla nunca muestra el botón, así que igual falla (sin enmascarar).
   */
  async expectLoggedIn(): Promise<void> {
    await expect(this.logoutButton).toBeVisible({ timeout: 30_000 });
    expect(this.page.url()).not.toContain('/login');
  }

  /** Contexto de root: indicador de rol + ícono de impersonar + menú poblado. */
  async expectRootContext(): Promise<void> {
    await expect(this.rootRole).toBeVisible();
    await expect(this.impersonateButton).toBeVisible();
    await expect(this.navLinks.first()).toBeVisible();
    expect(await this.navLinks.count()).toBeGreaterThan(1);
  }

  async logout(): Promise<void> {
    await this.logoutButton.click();
  }
}
