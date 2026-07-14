import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { saveBlobResponse } from '../../../../core/http';
import { FotosService } from '../data/fotos.service';
import { Foto } from '../domain/foto.model';
import { FotoImgComponent } from './foto-img.component';

/**
 * Preview ampliado de una foto (el derivado de 1200px CON watermark — el original nunca se
 * renderiza, ADR-01). Desde acá se puede descargar el original limpio (flujo admin, para imprimir).
 */
@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatButtonModule, MatDialogModule, MatIconModule, FotoImgComponent],
  template: `
    <h2 mat-dialog-title class="titulo">{{ data.nombreArchivoOriginal }}</h2>
    <mat-dialog-content>
      <tbi-foto-img
        class="preview"
        [fotoId]="data.id"
        variante="preview"
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
    .titulo {
      max-width: 70vw;
      overflow: hidden;
      text-overflow: ellipsis;
      white-space: nowrap;
    }

    .preview {
      width: min(72vw, 900px);
      height: min(66vh, 700px);
    }
  `,
})
export class FotoPreviewDialogComponent {
  private readonly fotosService = inject(FotosService);
  protected readonly data = inject<Foto>(MAT_DIALOG_DATA);

  protected descargarOriginal(): void {
    this.fotosService
      .descargarOriginal(this.data.id)
      .subscribe((response) => saveBlobResponse(response, this.data.nombreArchivoOriginal));
  }
}
