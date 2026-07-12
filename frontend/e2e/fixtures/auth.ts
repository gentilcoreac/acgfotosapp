import { type Page } from '@playwright/test';
import { LoginPage } from '../pages/login.page';
import { ShellPage } from '../pages/shell.page';
import { type Credentials } from './test-data';

/**
 * Loguea por la UI y deja la app en el home autenticado. Se usa en el `beforeEach` de los specs
 * que necesitan sesión.
 *
 * Por qué login por test (y no `storageState` compartido): el refresh token **rota** con detección
 * de replay (ventana de gracia de 10s, por ADR) → una cookie de sesión reutilizada entre tests sería
 * single-use y volvería la suite flaky. Un login fresco por test deja la cookie de refresh viva en el
 * contexto del navegador (la sesión se rehidrata sola en F5/401 dentro de ese mismo test) y aísla a
 * cada test del resto.
 */
export async function loginAs(page: Page, creds: Credentials): Promise<void> {
  const login = new LoginPage(page);
  await login.goto();
  await login.login(creds.username, creds.password);
  await new ShellPage(page).expectLoggedIn();
}
