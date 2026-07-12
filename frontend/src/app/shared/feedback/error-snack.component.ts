import { Component, inject, signal, ChangeDetectionStrategy } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MAT_SNACK_BAR_DATA, MatSnackBarRef } from '@angular/material/snack-bar';

/** Datos que `NotificationService` le pasa al snackbar de error vía `openFromComponent`. */
export interface ErrorSnackData {
  /** Titular (una línea). Siempre visible. */
  message: string;
  /** Detalle accionable (validaciones). Si hay, aparece el botón "Ver detalle". */
  detail: string[];
  /** Id de correlación para soporte (errores técnicos): se muestra como "ref" copiable. */
  traceId?: string;
}

/**
 * Snackbar de error con detalle desplegable. Para validaciones de negocio muestra el titular y, bajo
 * "Ver detalle", la lista completa de reglas (legible, en vez de un `join(' · ')` cortado). Para
 * errores técnicos muestra el titular y un "ref" copiable que matchea el audit log del backend.
 * Lo abre `NotificationService.error` cuando hay detalle o traceId; los errores simples siguen
 * usando el snackbar de una línea.
 */
@Component({
  selector: 'tbi-error-snack',
  standalone: true,
  imports: [MatButtonModule, MatIconModule],
  template: `
    <div class="snack" data-testid="error-snack">
      <div class="snack__head">
        <mat-icon class="snack__icon" aria-hidden="true">error_outline</mat-icon>
        <span class="snack__msg">{{ data.message }}</span>
      </div>

      @if (hasDetail && expanded()) {
        <ul class="snack__detail" data-testid="error-snack-detail">
          @for (item of data.detail; track item) {
            <li>{{ item }}</li>
          }
        </ul>
      }

      @if (data.traceId) {
        <div class="snack__ref" data-testid="error-snack-ref">ref: {{ data.traceId }}</div>
      }

      <div class="snack__actions">
        @if (hasDetail) {
          <button mat-button type="button" data-testid="error-snack-toggle" (click)="toggle()">
            {{ expanded() ? 'Ocultar detalle' : 'Ver detalle' }}
          </button>
        }
        @if (hasDetail || data.traceId) {
          <button mat-button type="button" data-testid="error-snack-copy" (click)="copy()">
            {{ copied() ? 'Copiado' : 'Copiar' }}
          </button>
        }
        <button mat-button type="button" data-testid="error-snack-close" (click)="close()">
          Cerrar
        </button>
      </div>
    </div>
  `,
  changeDetection: ChangeDetectionStrategy.Eager,
  styles: [
    `
      .snack {
        display: flex;
        flex-direction: column;
        gap: 8px;
      }
      .snack__head {
        display: flex;
        align-items: flex-start;
        gap: 8px;
      }
      .snack__icon {
        flex: 0 0 auto;
        font-size: 20px;
        width: 20px;
        height: 20px;
        line-height: 20px;
      }
      .snack__msg {
        font: var(--mat-sys-body-medium);
        font-weight: 500;
      }
      .snack__detail {
        margin: 0;
        padding-left: 28px;
        display: flex;
        flex-direction: column;
        gap: 4px;
        font: var(--mat-sys-body-small);
        max-height: 40vh;
        overflow-y: auto;
      }
      .snack__ref {
        padding-left: 28px;
        font: var(--mat-sys-body-small);
        opacity: 0.8;
        font-family: var(--mat-sys-monospace, monospace);
      }
      .snack__actions {
        display: flex;
        justify-content: flex-end;
        gap: 4px;
        flex-wrap: wrap;
      }
    `,
  ],
})
export class ErrorSnackComponent {
  protected readonly data = inject<ErrorSnackData>(MAT_SNACK_BAR_DATA);
  private readonly ref = inject<MatSnackBarRef<ErrorSnackComponent>>(MatSnackBarRef);

  protected readonly expanded = signal(false);
  protected readonly copied = signal(false);

  protected get hasDetail(): boolean {
    return this.data.detail.length > 0;
  }

  protected toggle(): void {
    this.expanded.update((value) => !value);
  }

  protected close(): void {
    this.ref.dismiss();
  }

  protected async copy(): Promise<void> {
    const parts = [this.data.message, ...this.data.detail];
    if (this.data.traceId) {
      parts.push(`ref: ${this.data.traceId}`);
    }
    try {
      await navigator.clipboard.writeText(parts.join('\n'));
      this.copied.set(true);
    } catch {
      // Clipboard no disponible (permisos / contexto no seguro): no rompemos el toast.
    }
  }
}
