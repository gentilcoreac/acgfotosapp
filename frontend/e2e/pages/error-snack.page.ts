import { type Page, type Locator } from '@playwright/test';

/**
 * Page Object del toast de error. `NotificationService` abre DOS variantes, ambas con la `panelClass`
 * **`tbi-snack--error`**: el snackbar simple de una línea (errores con solo `message`) y el
 * `ErrorSnackComponent` rico (cuando hay `errors[]` o `traceId`: "Ver detalle" / "ref" / "Copiar").
 *
 * Por eso `root` = la `panelClass` (clase propia y estable de la app, cubre AMBAS variantes → sirve para
 * contar toasts / coalescencia). Los sub-elementos del toast rico van por `data-testid` (solo existen en
 * `ErrorSnackComponent`).
 */
export class ErrorSnackPage {
  /** Raíz del/los toast(s) de error (simple o rico). Contar para verificar coalescencia. */
  readonly root: Locator;
  /** Botón "Ver detalle / Ocultar detalle" (toast rico: solo si el error trae `errors[]`). */
  readonly toggle: Locator;
  /** Botón "Copiar" (toast rico: cuando hay detalle o `traceId`). */
  readonly copy: Locator;
  /** "ref: {traceId}" — referencia técnica copiable (toast rico, sin detalle). */
  readonly ref: Locator;
  /** Lista de reglas de validación desplegada bajo "Ver detalle" (toast rico). */
  readonly detail: Locator;

  constructor(private readonly page: Page) {
    this.root = page.locator('.tbi-snack--error');
    this.toggle = page.getByTestId('error-snack-toggle');
    this.copy = page.getByTestId('error-snack-copy');
    this.ref = page.getByTestId('error-snack-ref');
    this.detail = page.getByTestId('error-snack-detail');
  }

  /** Texto (título o detalle) dentro del toast. */
  text(value: string): Locator {
    return this.root.getByText(value);
  }
}
