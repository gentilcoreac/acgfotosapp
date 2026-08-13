import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { TbiVisorFotosComponent } from '../tbi-visor-fotos/tbi-visor-fotos.component';

/** Imagen suelta a ampliar: no es una foto de un evento (QR, canvas renderizado, preview de archivo
 * local), así que no tiene `fotoId` ni backing de ningún repositorio — solo un `src` ya resuelto
 * (data URL u object URL) y su alt. */
export interface ImagenAmpliadaDialogData {
  src: string;
  alt: string;
}

/**
 * Diálogo genérico para ampliar UNA imagen que no pertenece a una colección navegable (visor-fotos,
 * "imagen aislada" — openspec/changes/visor-fotos-cobertura-total). Reusa `tbi-visor-fotos` con una
 * colección de un solo elemento: al tener un único ítem, el visor ya oculta por sí solo el contador y
 * las flechas de recorrido (`hayVarias()`), así que no hace falta ningún modo o flag nuevo.
 */
@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatDialogModule, TbiVisorFotosComponent],
  template: `
    <mat-dialog-content>
      <tbi-visor-fotos [items]="items" ariaLabel="Imagen ampliada">
        <ng-template let-item>
          <img [src]="item.src" [alt]="item.alt" />
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

    img {
      flex: 1;
      align-self: stretch;
      min-width: 0;
      min-height: 0;
      object-fit: contain;
    }
  `,
})
export class ImagenAmpliadaDialogComponent {
  protected readonly data = inject<ImagenAmpliadaDialogData>(MAT_DIALOG_DATA);
  // `[items]="[data]"` en el template crearía un array NUEVO en cada chequeo de change detection:
  // la referencia cambiaría, `NgTemplateOutlet` recrearía el contenido proyectado una y otra vez.
  // Con un `<img>` no se nota (recrear es barato, el navegador ya tiene el data URL decodificado);
  // el bug real apareció con `MarcaAguaPreviewAmpliadaDialogComponent` (mismo patrón, pero
  // proyectando un `<canvas>` cuyo dibujo es async — la recreación constante nunca lo dejaba
  // terminar de pintar). Referencia estable acá también, por las dudas.
  protected readonly items = [this.data];
}
