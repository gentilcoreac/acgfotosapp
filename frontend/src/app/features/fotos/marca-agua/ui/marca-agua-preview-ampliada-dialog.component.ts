import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { TbiVisorFotosComponent } from '../../../../shared/ui/tbi-visor-fotos/tbi-visor-fotos.component';
import { ImagenDecodificada, MuestraVariante } from '../domain/marca-agua-muestra.util';
import { PerfilMarcaAgua } from '../domain/marca-agua.model';
import { PerfilMarcaAguaCanvasComponent } from './perfil-marca-agua-canvas.component';

export interface MarcaAguaPreviewAmpliadaDialogData {
  perfil: PerfilMarcaAgua;
  variante: MuestraVariante;
  fotoPropia: ImagenDecodificada | null;
  comprimir: boolean;
  calidad: number;
  ancho: number;
  alto: number;
  ariaLabel: string;
}

/**
 * Amplía UNA muestra renderizada del perfil de marca de agua (listado o editor). No es una foto de
 * un evento —es el canvas de vista previa, re-renderizado a mayor tamaño— así que es una imagen
 * aislada, sin recorrido (openspec/changes/visor-fotos-cobertura-total, D3/D4): el visor con una
 * colección de un solo ítem ya oculta contador y flechas por sí solo.
 */
@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatDialogModule, TbiVisorFotosComponent, PerfilMarcaAguaCanvasComponent],
  template: `
    <mat-dialog-content>
      <tbi-visor-fotos [items]="items" [ariaLabel]="data.ariaLabel">
        <ng-template let-item>
          <tbi-perfil-marca-agua-canvas
            [perfil]="item.perfil"
            [variante]="item.variante"
            [fotoPropia]="item.fotoPropia"
            [comprimir]="item.comprimir"
            [calidad]="item.calidad"
            [ancho]="item.ancho"
            [alto]="item.alto"
            [ariaLabel]="item.ariaLabel"
          />
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

    tbi-perfil-marca-agua-canvas {
      flex: 1;
      align-self: stretch;
      min-width: 0;
      min-height: 0;
      max-width: 100%;
      max-height: 100%;
    }
  `,
})
export class MarcaAguaPreviewAmpliadaDialogComponent {
  protected readonly data = inject<MarcaAguaPreviewAmpliadaDialogData>(MAT_DIALOG_DATA);
  // Bug real encontrado en vivo (2026-08-12): `[items]="[data]"` en el template crea un array NUEVO
  // en cada chequeo de CD → la referencia cambia → `NgTemplateOutlet` recrea el `<canvas>` proyectado
  // una y otra vez → el dibujo async (bajar assets, decodificar, componer) nunca llega a terminar
  // antes de que lo destruyan de nuevo → el diálogo queda con el canvas en blanco para siempre.
  // Referencia estable acá evita la recreación.
  protected readonly items = [this.data];
}
