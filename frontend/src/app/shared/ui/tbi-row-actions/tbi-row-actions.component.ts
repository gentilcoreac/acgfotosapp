import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  computed,
  inject,
  input,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { EMPTY, Observable, finalize, of, switchMap } from 'rxjs';
import { ConfirmData } from '../../feedback/confirm-dialog.component';
import { ConfirmService } from '../../feedback/confirm.service';

/**
 * Acción de fila declarativa para `tbi-table` (vía su input `rowActions`). Reemplaza al
 * `actionsTpl` libre + `tbi-row-action`: una sola fuente de datos que el componente presenta
 * de forma responsive (íconos inline en desktop, menú de 3 puntos en touch) y de la que maneja
 * confirmación y spinner. Usar `handler` para acciones sincrónicas (navegar, abrir diálogo) y
 * `run` para llamadas a la API (muestra spinner mientras el observable está en vuelo).
 */
export interface TbiRowAction<T> {
  /** Ícono de Material Symbols. */
  icon: string;
  /** Etiqueta (tooltip del botón en desktop, texto del ítem en el menú touch). */
  label: string;
  /** Tinte destructivo (rojo), p. ej. Eliminar. */
  danger?: boolean;
  /** Oculta la acción para esta fila (default: visible). */
  hidden?: (row: T) => boolean;
  /** Deshabilita la acción para esta fila. */
  disabled?: (row: T) => boolean;
  /** Confirmación previa; si devuelve `null` no confirma. El spinner no gira durante el diálogo. */
  confirm?: (row: T) => ConfirmData | null;
  /** Acción sincrónica (navegar, abrir diálogo de edición). */
  handler?: (row: T) => void;
  /** Acción API: devuelve el observable de la operación; muestra spinner mientras corre. */
  run?: (row: T) => Observable<unknown>;
}

@Component({
  selector: 'tbi-row-actions',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    MatButtonModule,
    MatIconModule,
    MatMenuModule,
    MatTooltipModule,
    MatProgressSpinnerModule,
  ],
  templateUrl: './tbi-row-actions.component.html',
  styleUrl: './tbi-row-actions.component.scss',
})
export class TbiRowActionsComponent<T> {
  readonly row = input.required<T>();
  readonly actions = input.required<TbiRowAction<T>[]>();

  /** Acción en vuelo (sólo una a la vez): mientras está seteada se muestra el spinner. */
  protected readonly loadingAction = signal<TbiRowAction<T> | null>(null);

  protected readonly visibleActions = computed(() =>
    this.actions().filter((action) => !action.hidden?.(this.row())),
  );

  private readonly destroyRef = inject(DestroyRef);
  private readonly confirmService = inject(ConfirmService);

  protected isDisabled(action: TbiRowAction<T>): boolean {
    return action.disabled?.(this.row()) ?? false;
  }

  protected onAction(action: TbiRowAction<T>): void {
    if (this.loadingAction()) {
      return;
    }
    const confirmData = action.confirm?.(this.row()) ?? null;
    const gate: Observable<boolean> = confirmData
      ? this.confirmService.confirm(confirmData)
      : of(true);

    gate
      .pipe(
        switchMap((confirmed) => {
          if (!confirmed) {
            return EMPTY;
          }
          if (action.run) {
            this.loadingAction.set(action);
            return action.run(this.row()).pipe(finalize(() => this.loadingAction.set(null)));
          }
          action.handler?.(this.row());
          return EMPTY;
        }),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe();
  }
}
