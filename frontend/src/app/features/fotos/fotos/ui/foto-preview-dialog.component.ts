import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { saveBlobResponse } from '../../../../core/http';
import { TbiVisorFotosComponent } from '../../../../shared/ui/tbi-visor-fotos/tbi-visor-fotos.component';
import { FotosService, VarianteDerivado } from '../data/fotos.service';
import { FotoImgComponent } from './foto-img.component';

/** Lo mínimo que el visor admin necesita de una foto: cualquier objeto con id+nombre sirve (la galería
 * pasa `Foto` completa; el admin de pedidos, la línea de un pedido). */
export interface FotoPreviewItem {
  id: number;
  nombreArchivoOriginal: string;
}

/**
 * Data del diálogo. `fotos`+`index` habilitan el recorrido; `varianteInicial` decide qué se ve
 * primero (default `'preview'`, el derivado con watermark — el admin de pedidos abre en `'original'`,
 * que es la que le importa para el laboratorio).
 */
export interface FotoPreviewDialogData {
  fotos: FotoPreviewItem[];
  index: number;
  varianteInicial?: VarianteDerivado;
}

/**
 * Preview ampliado del ADMIN, sobre el visor del sistema (`tbi-visor-fotos`, que aporta cerrar,
 * contador, flechas y teclado). Lo propio de este contexto es el toggle entre el derivado CON
 * watermark (lo que ve la familia, ADR-01) y el "Original" limpio, más la descarga: ambos son
 * exclusivos del admin, que ya tiene acceso al original por ADR-06.
 */
@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatButtonModule, MatDialogModule, MatIconModule, FotoImgComponent, TbiVisorFotosComponent],
  template: `
    <tbi-visor-fotos [items]="data.fotos" [(index)]="index" ariaLabel="Vista ampliada de la foto">
      <ng-template let-foto>
        <tbi-foto-img
          [fotoId]="foto.id"
          [variante]="variante()"
          fit="contain"
          [alt]="foto.nombreArchivoOriginal"
        />
      </ng-template>

      <span visorEsquinaInferiorDerecha class="archivo">{{ actual().nombreArchivoOriginal }}</span>

      <div visorFooter class="acciones">
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
        <button matButton (click)="descargarOriginal()">
          <mat-icon>download</mat-icon>
          Descargar original
        </button>
      </div>
    </tbi-visor-fotos>
  `,
  styles: `
    :host {
      display: flex;
      flex-direction: column;
      height: 100%;
    }

    .archivo {
      padding: 2px 8px;
      border-radius: 6px;
      background: color-mix(in srgb, var(--mat-sys-scrim) 55%, transparent);
      color: white;
      font-family: monospace;
      font-size: 0.75rem;
    }

    .acciones {
      display: flex;
      align-items: center;
      gap: 1rem;
      width: 100%;
      justify-content: space-between;
    }

    .toggle {
      display: flex;
      gap: 0.5rem;
    }

    .toggle__activo {
      background: var(--mat-sys-secondary-container);
      color: var(--mat-sys-on-secondary-container);
    }
  `,
})
export class FotoPreviewDialogComponent {
  private readonly fotosService = inject(FotosService);
  protected readonly data = inject<FotoPreviewDialogData>(MAT_DIALOG_DATA);

  protected readonly index = signal(this.data.index);
  protected readonly actual = computed(() => this.data.fotos[this.index()] ?? this.data.fotos[0]);
  protected readonly variante = signal<VarianteDerivado>(this.data.varianteInicial ?? 'preview');

  protected descargarOriginal(): void {
    const foto = this.actual();
    this.fotosService
      .descargarOriginal(foto.id)
      .subscribe((response) => saveBlobResponse(response, foto.nombreArchivoOriginal));
  }
}
