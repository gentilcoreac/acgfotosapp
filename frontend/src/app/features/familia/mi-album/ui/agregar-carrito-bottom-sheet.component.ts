import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { MAT_BOTTOM_SHEET_DATA, MatBottomSheetRef } from '@angular/material/bottom-sheet';
import { TamanoPrecio } from '../../../../core/familia';
import { AgregarCarritoComponent } from './agregar-carrito.component';
import { FotoFamiliaImgComponent } from './foto-familia-img.component';

export interface AgregarCarritoBottomSheetData {
  fotoId: number;
  tamanosPrecios: TamanoPrecio[];
  /** Abre el visor de la grilla (`MiAlbumComponent.verPreview`) posicionado en esta foto: este
   * bottom sheet no abre un visor propio para su miniatura, delega en el del contexto que lo llamó
   * (D2, openspec/changes/visor-fotos-cobertura-total) — cierra el sheet antes de delegar. */
  onAmpliar: () => void;
}

/**
 * Envoltorio del `MatBottomSheet` que abre la grilla (`MiAlbumComponent.agregarAlCarrito`): además de
 * `tbi-agregar-carrito`, muestra una miniatura de la foto que se está agregando (mobile-first: la
 * grilla queda tapada por el sheet, sin la miniatura no hay forma de confirmar CUÁL foto es) — click
 * en la miniatura la amplía.
 */
@Component({
  selector: 'tbi-agregar-carrito-bottom-sheet',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [AgregarCarritoComponent, FotoFamiliaImgComponent],
  template: `
    <div class="agregar-carrito-sheet">
      <button
        type="button"
        class="agregar-carrito-sheet__thumb-btn"
        aria-label="Ampliar foto"
        (click)="ampliar()"
      >
        <tbi-foto-familia-img class="agregar-carrito-sheet__thumb" [fotoId]="data.fotoId" />
      </button>
      <tbi-agregar-carrito [fotoId]="data.fotoId" [tamanosPrecios]="data.tamanosPrecios" />
    </div>
  `,
  styles: `
    .agregar-carrito-sheet {
      display: flex;
      align-items: center;
      gap: 0.85rem;
      padding: 1rem 1.25rem 1.5rem;
    }

    .agregar-carrito-sheet__thumb-btn {
      flex: none;
      width: 56px;
      height: 56px;
      padding: 0;
      border: none;
      border-radius: 8px;
      overflow: hidden;
      cursor: pointer;

      &:focus-visible {
        outline: 2px solid var(--mat-sys-primary);
        outline-offset: 2px;
      }
    }

    .agregar-carrito-sheet__thumb {
      width: 100%;
      height: 100%;
    }
  `,
})
export class AgregarCarritoBottomSheetComponent {
  protected readonly data = inject<AgregarCarritoBottomSheetData>(MAT_BOTTOM_SHEET_DATA);
  private readonly sheetRef = inject(MatBottomSheetRef);

  protected ampliar(): void {
    this.sheetRef.dismiss();
    this.data.onAmpliar();
  }
}
