import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { CarritoStore, FamiliaSessionStore, FotoFamilia, TamanoPrecio } from '../../../../core/familia';
import { TbiVisorFotosComponent } from '../../../../shared/ui/tbi-visor-fotos/tbi-visor-fotos.component';
import { AgregarCarritoComponent } from './agregar-carrito.component';
import { FotoFamiliaImgComponent } from './foto-familia-img.component';

export interface FotoFamiliaPreviewDialogData {
  fotos: FotoFamilia[];
  index: number;
  /** Catálogo para `tbi-agregar-carrito`; opcional en los tests que no ejercitan esa parte. */
  tamanosPrecios?: TamanoPrecio[];
}

/**
 * Preview ampliado de la galería de familia, sobre el visor del sistema (`tbi-visor-fotos`, que
 * aporta el marco, contador, cerrar, flechas y teclado). Lo propio de este contexto —y lo que el
 * visor NO conoce a propósito— es que la imagen sea `tbi-foto-familia-img` (derivado CON watermark,
 * sello tileado con el nombre de la familia y fricción anti-copia; el original nunca se expone acá,
 * ADR-01/ADR-06), el nombre legal en la esquina, el tilde de "ya en el carrito" y el selector de
 * tamaño+cantidad inline, para agregar viendo la foto ampliada sin volver a la grilla.
 */
@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatDialogModule, AgregarCarritoComponent, FotoFamiliaImgComponent, TbiVisorFotosComponent],
  template: `
    <mat-dialog-content>
      <tbi-visor-fotos [items]="data.fotos" [(index)]="index" ariaLabel="Foto ampliada">
        <ng-template let-foto>
          <tbi-foto-familia-img
            [fotoId]="foto.id"
            variante="preview"
            fit="contain"
            [ancho]="foto.ancho"
            [alto]="foto.alto"
          />
        </ng-template>

        @if (yaEnCarrito()) {
          <span visorBajoContador class="en-carrito" aria-hidden="true">✓</span>
        }
        @if (nombreFamilia(); as legal) {
          <span visorEsquinaInferiorIzquierda class="legal">{{ legal }}</span>
        }
        <span visorEsquinaInferiorDerecha class="archivo">{{ actual().nombreArchivoOriginal }}</span>
      </tbi-visor-fotos>
    </mat-dialog-content>

    <div class="agregar">
      <tbi-agregar-carrito [fotoId]="actual().id" [tamanosPrecios]="tamanosPrecios()" />
    </div>
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

    /* El visor da la caja; estirar la imagen es de quien la pone (el encapsulado de estilos no deja
       que un componente le dé tamaño al contenido que proyecta otro). */
    tbi-foto-familia-img {
      flex: 1;
      align-self: stretch;
      min-width: 0;
      min-height: 0;
    }

    .archivo,
    .legal {
      padding: 2px 8px;
      border-radius: 6px;
      background: color-mix(in srgb, var(--mat-sys-scrim) 55%, transparent);
      color: white;
      font-size: 0.75rem;
    }

    .archivo {
      font-family: monospace;
    }

    // Mismo aviso que el badge de la grilla (.grilla__en-carrito): esta foto ya tiene copias en el
    // carrito. Va debajo del contador (misma esquina) — no hay una 5ª esquina libre.
    .en-carrito {
      display: flex;
      align-items: center;
      justify-content: center;
      width: 22px;
      height: 22px;
      border-radius: 6px;
      background: var(--mat-sys-primary);
      color: var(--mat-sys-on-primary);
      font-size: 13px;
      font-weight: 700;
      box-shadow: 0 1px 3px rgb(0 0 0 / 30%);
    }

    // Bottom-sheet abajo, no al costado de la imagen: en pantallas anchas al costado se pierde la
    // continuidad visual con la foto. El selector queda pegado a la derecha de esa franja.
    .agregar {
      display: flex;
      justify-content: flex-end;
      flex: none;
      padding: 0.75rem 1rem;
      border-top: 1px solid var(--mat-sys-outline-variant);
    }
  `,
})
export class FotoFamiliaPreviewDialogComponent {
  protected readonly data = inject<FotoFamiliaPreviewDialogData>(MAT_DIALOG_DATA);
  private readonly carrito = inject(CarritoStore);

  /** "Familia de {nombres}" — mismo texto que el sello tileado de `tbi-foto-familia-img`, acá como
   * una sola etiqueta legible en la esquina. */
  protected readonly nombreFamilia = inject(FamiliaSessionStore).nombreFamilia;

  protected readonly index = signal(this.data.index);
  protected readonly actual = computed(() => this.data.fotos[this.index()] ?? this.data.fotos[0]);
  protected readonly tamanosPrecios = computed(() => this.data.tamanosPrecios ?? []);

  /** Mismo aviso que `.grilla__en-carrito` en la grilla. */
  protected readonly yaEnCarrito = computed(() => this.carrito.cantidadTotalDeFoto(this.actual().id) > 0);
}
