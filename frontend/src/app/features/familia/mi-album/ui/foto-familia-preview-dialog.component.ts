import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { CarritoStore, FamiliaSessionStore, FotoFamilia, TamanoPrecio } from '../../../../core/familia';
import { AgregarCarritoComponent } from './agregar-carrito.component';
import { FotoFamiliaImgComponent } from './foto-familia-img.component';

export interface FotoFamiliaPreviewDialogData {
  fotos: FotoFamilia[];
  index: number;
  /** Catálogo para `tbi-agregar-carrito`; opcional en los tests que no ejercitan esa parte. */
  tamanosPrecios?: TamanoPrecio[];
}

/**
 * Preview ampliado de una foto de la galería de familia (el derivado CON watermark — el original
 * nunca se expone acá, ADR-01/ADR-06). Casi pantalla completa (ver `verPreview` en
 * `MiAlbumComponent`), la foto se muestra ENTERA (contain). Carrusel: flechas + flechas de teclado
 * para pasar de foto en foto sin volver a la grilla, da la vuelta al llegar a una punta. Sin
 * "Descargar original": eso es exclusivo del admin.
 *
 * Selector de tamaño+cantidad inline (Fase 2, Carrito): agregar viendo la foto ampliada, sin volver
 * a la grilla — mismo `tbi-agregar-carrito` que usa la grilla (ver `MiAlbumComponent`), no hace su
 * propio fetch del catálogo.
 */
@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatButtonModule, MatDialogModule, MatIconModule, AgregarCarritoComponent, FotoFamiliaImgComponent],
  host: {
    '(document:keydown.arrowleft)': 'anterior()',
    '(document:keydown.arrowright)': 'siguiente()',
  },
  template: `
    <mat-dialog-content>
      @if (data.fotos.length > 1) {
        <span class="contador">{{ index() + 1 }} / {{ data.fotos.length }}</span>
      }
      @if (yaEnCarrito()) {
        <span class="en-carrito" aria-hidden="true">✓</span>
      }

      <button class="cerrar" matIconButton mat-dialog-close aria-label="Cerrar">
        <mat-icon>close</mat-icon>
      </button>

      @if (data.fotos.length > 1) {
        <button
          class="nav nav--prev"
          matIconButton
          aria-label="Foto anterior"
          (click)="anterior()"
        >
          <mat-icon>chevron_left</mat-icon>
        </button>
      }

      <tbi-foto-familia-img class="preview" [fotoId]="actual().id" variante="preview" fit="contain" />

      @if (nombreFamilia(); as legal) {
        <span class="legal">{{ legal }}</span>
      }
      <span class="archivo">{{ actual().nombreArchivoOriginal }}</span>

      @if (data.fotos.length > 1) {
        <button
          class="nav nav--next"
          matIconButton
          aria-label="Foto siguiente"
          (click)="siguiente()"
        >
          <mat-icon>chevron_right</mat-icon>
        </button>
      }
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
      position: relative;
      flex: 1;
      display: flex;
      overflow: hidden;
      max-height: none;
      padding: 0;
      min-height: 0;
    }

    .preview {
      flex: 1;
      min-width: 0;
      min-height: 0;
    }

    // Las 4 esquinas siguen el prototipo (docs/ClaudeDesign/PropuestaDisenioExperienciaCompra):
    // cerrar arriba-derecha, paginación arriba-izquierda, nombre legal abajo-izquierda, archivo abajo-derecha.
    .archivo {
      position: absolute;
      right: 12px;
      bottom: 12px;
      z-index: 1;
      padding: 2px 8px;
      border-radius: 6px;
      background: color-mix(in srgb, var(--mat-sys-scrim) 55%, transparent);
      color: white;
      font-family: monospace;
      font-size: 0.75rem;
    }

    .legal {
      position: absolute;
      left: 12px;
      bottom: 12px;
      z-index: 1;
      padding: 2px 8px;
      border-radius: 6px;
      background: color-mix(in srgb, var(--mat-sys-scrim) 55%, transparent);
      color: white;
      font-size: 0.75rem;
    }

    .cerrar {
      position: absolute;
      top: 8px;
      right: 8px;
      z-index: 2;
      background: color-mix(in srgb, var(--mat-sys-scrim) 45%, transparent);
      color: white;
      transition: background-color 0.15s ease;

      &:hover {
        background: color-mix(in srgb, var(--mat-sys-scrim) 65%, transparent);
      }
    }

    .contador {
      position: absolute;
      top: 12px;
      left: 12px;
      z-index: 2;
      padding: 2px 10px;
      border-radius: 999px;
      background: color-mix(in srgb, var(--mat-sys-scrim) 45%, transparent);
      color: white;
      font-size: 0.75rem;
    }

    // Mismo aviso que el badge de la grilla (.grilla__en-carrito): esta foto ya tiene copias en el
    // carrito. Apilado debajo del contador (misma esquina) — no hay una 5ª esquina libre.
    .en-carrito {
      position: absolute;
      top: 44px;
      left: 12px;
      z-index: 2;
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

    .nav {
      position: absolute;
      top: 50%;
      z-index: 1;
      transform: translateY(-50%);
      background: color-mix(in srgb, var(--mat-sys-scrim) 45%, transparent);
      color: white;
      transition: background-color 0.15s ease;

      &:hover {
        background: color-mix(in srgb, var(--mat-sys-scrim) 65%, transparent);
      }

      &:active {
        background: color-mix(in srgb, var(--mat-sys-scrim) 80%, transparent);
      }

      &--prev {
        left: 4px;
      }

      &--next {
        right: 4px;
      }
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

  /** "Familia de {nombres}" — mismo texto que el overlay tileado de `tbi-foto-familia-img`, acá como
   * una sola etiqueta legible en la esquina (ver comentario de estilos: layout de 4 esquinas del prototipo). */
  protected readonly nombreFamilia = inject(FamiliaSessionStore).nombreFamilia;

  protected readonly index = signal(this.data.index);
  protected readonly actual = computed(() => this.data.fotos[this.index()]);
  protected readonly tamanosPrecios = computed(() => this.data.tamanosPrecios ?? []);

  /** Mismo aviso que `.grilla__en-carrito` en la grilla. */
  protected readonly yaEnCarrito = computed(() => this.carrito.cantidadTotalDeFoto(this.actual().id) > 0);

  protected anterior(): void {
    const total = this.data.fotos.length;
    this.index.update((i) => (i - 1 + total) % total);
  }

  protected siguiente(): void {
    const total = this.data.fotos.length;
    this.index.update((i) => (i + 1) % total);
  }
}
