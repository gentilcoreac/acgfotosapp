import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  inject,
  input,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { Observable, EMPTY, finalize, of, switchMap } from 'rxjs';
import { ConfirmData } from '../../feedback/confirm-dialog.component';
import { ConfirmService } from '../../feedback/confirm.service';

/**
 * Botón de acción de fila (icon-button) con spinner integrado, para acciones que **llaman a
 * la API** (Eliminar y futuras acciones dedicadas) dentro del `actionsTpl` de `tbi-table`.
 * Centraliza el feedback de carga en un solo lugar, así ninguna lista repite el manejo de
 * estado ni se olvida del spinner.
 *
 * Flujo: (1) si se pasa `confirm`, abre el diálogo de confirmación — el spinner NO gira en
 * esta fase; (2) si se confirma (o no hay confirmación), invoca `run()` y muestra el spinner
 * mientras el observable está en vuelo, deshabilitando el botón; (3) al completar/fallar,
 * resetea. La suscripción la maneja el componente (`takeUntilDestroyed`); el consumidor
 * encadena su lógica de resultado con `tap`/`finalize` en el observable que devuelve `run`.
 */
@Component({
  selector: 'tbi-row-action',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatButtonModule, MatIconModule, MatProgressSpinnerModule],
  template: `
    @if (loading()) {
      <mat-progress-spinner mode="indeterminate" [diameter]="22" />
    } @else {
      <button
        matIconButton
        [attr.aria-label]="ariaLabel()"
        [disabled]="disabled()"
        (click)="onClick()"
      >
        <mat-icon>{{ icon() }}</mat-icon>
      </button>
    }
  `,
  styles: `
    :host {
      display: inline-flex;
      align-items: center;
      justify-content: center;
      width: 40px;
      height: 40px;
    }
  `,
})
export class TbiRowActionComponent {
  readonly icon = input.required<string>();
  readonly ariaLabel = input<string>('');
  readonly disabled = input<boolean>(false);
  /** Confirmación opcional previa a la acción (el spinner no gira durante el diálogo). */
  readonly confirm = input<ConfirmData | null>(null);
  /** Devuelve el observable de la operación API. Se invoca tras confirmar (si hay confirm). */
  readonly run = input.required<() => Observable<unknown>>();

  protected readonly loading = signal(false);
  private readonly destroyRef = inject(DestroyRef);
  private readonly confirmService = inject(ConfirmService);

  protected onClick(): void {
    if (this.loading()) {
      return;
    }
    const confirmData = this.confirm();
    const gate: Observable<boolean> = confirmData
      ? this.confirmService.confirm(confirmData)
      : of(true);

    gate
      .pipe(
        switchMap((confirmed) => {
          if (!confirmed) {
            return EMPTY;
          }
          this.loading.set(true);
          return this.run()().pipe(finalize(() => this.loading.set(false)));
        }),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe();
  }
}
