import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { FotoFamilia, TamanoPrecio } from '../../../../core/familia';
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
 * `MiAlbumComponent`), la foto se muestra ENTERA (contain). Carrusel (pedido 2026-07-19): flechas +
 * flechas de teclado para pasar de foto en foto sin volver a la grilla, da la vuelta al llegar a una
 * punta. Sin "Descargar original": eso es exclusivo del admin.
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

    <mat-dialog-actions align="end">
      @if (data.fotos.length > 1) {
        <span class="contador">{{ index() + 1 }} / {{ data.fotos.length }}</span>
      }
      <button matButton mat-dialog-close>Cerrar</button>
    </mat-dialog-actions>
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

    .agregar {
      flex: none;
      padding: 0.75rem 1rem;
      border-top: 1px solid var(--mat-sys-outline-variant);
    }

    mat-dialog-actions {
      align-items: center;
    }

    .contador {
      margin-right: auto;
      color: var(--mat-sys-on-surface-variant);
      font-size: 0.8125rem;
    }
  `,
})
export class FotoFamiliaPreviewDialogComponent {
  protected readonly data = inject<FotoFamiliaPreviewDialogData>(MAT_DIALOG_DATA);

  protected readonly index = signal(this.data.index);
  protected readonly actual = computed(() => this.data.fotos[this.index()]);
  protected readonly tamanosPrecios = computed(() => this.data.tamanosPrecios ?? []);

  protected anterior(): void {
    const total = this.data.fotos.length;
    this.index.update((i) => (i - 1 + total) % total);
  }

  protected siguiente(): void {
    const total = this.data.fotos.length;
    this.index.update((i) => (i + 1) % total);
  }
}
