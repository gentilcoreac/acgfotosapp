import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { TbiVisorFotosComponent } from '../../../../shared/ui/tbi-visor-fotos/tbi-visor-fotos.component';
import { ResultadoComparador, formatearPeso } from '../domain/comparador-tamanos.util';

/** Umbral bajo el cual la impresión en 10×15 se ve visiblemente borrosa (mismo valor que el inline). */
const DPI_ACEPTABLE = 150;

interface ResultadoVista extends ResultadoComparador {
  previewUrl: string;
}

export interface ComparadorAmpliadoDialogData {
  resultados: ResultadoVista[];
  index: number;
}

/**
 * Ampliación del comparador de tamaños: mismo `tbi-visor-fotos` que ya recorre las 5 muestras
 * inline, pero a pantalla casi completa — la vista inline queda acotada (`.comparador__visor`,
 * alto fijo) para convivir con el resto de la pantalla; esto es lo que responde al pedido de
 * "click para ampliar" (openspec/changes/visor-fotos-cobertura-total).
 */
@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DecimalPipe, MatDialogModule, TbiVisorFotosComponent],
  template: `
    <mat-dialog-content>
      <tbi-visor-fotos [items]="data.resultados" [index]="data.index" ariaLabel="Comparador de tamaños ampliado">
        <ng-template let-r>
          <div class="tamano-grande">
            <img class="tamano-grande__preview" [src]="r.previewUrl" alt="" />
            <div class="tamano-grande__datos">
              <span class="tamano-grande__titulo">Lado mayor {{ r.ladoMayorPedido }}px</span>
              <span class="tamano-grande__dato">{{ r.ancho }} × {{ r.alto }}px</span>
              <span class="tamano-grande__dato">{{ r.dpi | number: '1.0-0' }} dpi en 10×15</span>
              <span class="tamano-grande__dato">{{ formatearPeso(r.pesoBytes) }}</span>
              @if (r.dpi < DPI_ACEPTABLE) {
                <span class="tamano-grande__aviso">Se va a ver borroso al imprimir</span>
              }
            </div>
          </div>
        </ng-template>
      </tbi-visor-fotos>
    </mat-dialog-content>
  `,
  styles: `
    :host {
      display: flex;
      flex-direction: column;
      height: 100%;
      outline: none;
    }

    mat-dialog-content {
      flex: 1;
      display: flex;
      overflow: hidden;
      max-height: none;
      min-height: 0;
      padding: 0;
    }

    .tamano-grande {
      display: flex;
      flex-direction: column;
      flex: 1;
      align-self: stretch;
      min-width: 0;
      min-height: 0;
      gap: 0.75rem;
      padding: 1rem;
    }

    .tamano-grande__preview {
      flex: 1;
      min-height: 0;
      width: 100%;
      object-fit: contain;
    }

    .tamano-grande__datos {
      display: flex;
      align-items: baseline;
      flex-wrap: wrap;
      gap: 0.9rem;
      flex: none;
    }

    .tamano-grande__titulo {
      font-weight: 600;
      font-size: 1.1rem;
    }

    .tamano-grande__dato {
      font-size: 0.9rem;
      color: var(--mat-sys-on-surface-variant);
    }

    .tamano-grande__aviso {
      font-size: 0.9rem;
      font-weight: 600;
      color: var(--mat-sys-error);
    }
  `,
})
export class ComparadorAmpliadoDialogComponent {
  protected readonly data = inject<ComparadorAmpliadoDialogData>(MAT_DIALOG_DATA);
  protected readonly formatearPeso = formatearPeso;
  protected readonly DPI_ACEPTABLE = DPI_ACEPTABLE;
}
