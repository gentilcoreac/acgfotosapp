import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { MAT_BOTTOM_SHEET_DATA } from '@angular/material/bottom-sheet';
import { TamanoPrecio } from '../../../../core/familia';
import { AgregarCarritoComponent } from './agregar-carrito.component';

export interface AgregarCarritoBottomSheetData {
  fotoId: number;
  tamanosPrecios: TamanoPrecio[];
}

/**
 * Envoltorio del `MatBottomSheet` que abre la grilla (`MiAlbumComponent.agregarAlCarrito`): solo
 * desempaqueta `MAT_BOTTOM_SHEET_DATA` para `tbi-agregar-carrito` — mobile-first, evita navegar
 * fuera de la grilla para agregar varias fotos seguidas.
 */
@Component({
  selector: 'tbi-agregar-carrito-bottom-sheet',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [AgregarCarritoComponent],
  template: `
    <div class="agregar-carrito-sheet">
      <tbi-agregar-carrito [fotoId]="data.fotoId" [tamanosPrecios]="data.tamanosPrecios" />
    </div>
  `,
  styles: `
    .agregar-carrito-sheet {
      padding: 1rem 1.25rem 1.5rem;
    }
  `,
})
export class AgregarCarritoBottomSheetComponent {
  protected readonly data = inject<AgregarCarritoBottomSheetData>(MAT_BOTTOM_SHEET_DATA);
}
