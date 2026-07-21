import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { saveBlobResponse } from '../../../../core/http';
import { FotosService, VarianteDerivado } from '../data/fotos.service';
import { FotoImgComponent } from './foto-img.component';

/** Data mínima que necesita el diálogo — cualquier objeto con `id`+`nombreArchivoOriginal` sirve
 * (así lo puede abrir tanto la galería, con un `Foto` completo, como el admin de pedidos, con la
 * línea de un pedido). `varianteInicial` decide qué vista se ve primero (default `'preview'`, el
 * derivado con watermark — la galería no la pasa, así que no cambia). */
export interface FotoPreviewDialogData {
  id: number;
  nombreArchivoOriginal: string;
  varianteInicial?: VarianteDerivado;
}

/**
 * Preview ampliado de una foto, con un toggle entre el derivado CON watermark (lo que ve la
 * familia, ADR-01) y el "Original" (sin watermark, ADR-06 — el original sigue sin servirse jamás a
 * las familias, esto es solo para el admin que ya lo puede descargar). Cuál se ve primero depende
 * de quién abre el diálogo (`varianteInicial`): la galería por defecto sigue mostrando la vista del
 * cliente primero; el admin de pedidos pidió ver la ORIGINAL primero (es la que le importa para el
 * laboratorio), con la del cliente como toggle secundario. El diálogo se abre casi a pantalla
 * completa y la foto se muestra ENTERA (contain): nunca hay scroll lateral.
 */
@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatButtonModule, MatDialogModule, MatIconModule, FotoImgComponent],
  template: `
    <h2 mat-dialog-title class="titulo">{{ data.nombreArchivoOriginal }}</h2>
    <mat-dialog-content>
      <div class="toggle" role="group" aria-label="Vista">
        <button
          matButton
          type="button"
          [class.toggle__activo]="variante() === 'preview'"
          (click)="variante.set('preview')"
        >
          Vista del cliente
        </button>
        <button
          matButton
          type="button"
          [class.toggle__activo]="variante() === 'original'"
          (click)="variante.set('original')"
        >
          Original
        </button>
      </div>
      <tbi-foto-img
        class="preview"
        [fotoId]="data.id"
        [variante]="variante()"
        fit="contain"
        [alt]="data.nombreArchivoOriginal"
      />
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button matButton (click)="descargarOriginal()">
        <mat-icon>download</mat-icon>
        Descargar original
      </button>
      <button matButton mat-dialog-close>Cerrar</button>
    </mat-dialog-actions>
  `,
  styles: `
    /* El diálogo se abre con height fijo: el host llena el panel y el contenido flexea,
       así la imagen toma todo el alto disponible entre título y acciones. */
    :host {
      display: flex;
      flex-direction: column;
      height: 100%;
    }

    .titulo {
      max-width: 85vw;
      overflow: hidden;
      text-overflow: ellipsis;
      white-space: nowrap;
    }

    mat-dialog-content {
      flex: 1;
      display: flex;
      flex-direction: column;
      overflow: hidden;
      max-height: none;
    }

    .toggle {
      display: flex;
      gap: 0.5rem;
      flex: none;
      margin-bottom: 0.5rem;
    }

    .toggle__activo {
      background: var(--mat-sys-secondary-container);
      color: var(--mat-sys-on-secondary-container);
    }

    .preview {
      flex: 1;
      min-width: 0;
      min-height: 0;
    }
  `,
})
export class FotoPreviewDialogComponent {
  private readonly fotosService = inject(FotosService);
  protected readonly data = inject<FotoPreviewDialogData>(MAT_DIALOG_DATA);

  protected readonly variante = signal<VarianteDerivado>(this.data.varianteInicial ?? 'preview');

  protected descargarOriginal(): void {
    this.fotosService
      .descargarOriginal(this.data.id)
      .subscribe((response) => saveBlobResponse(response, this.data.nombreArchivoOriginal));
  }
}
