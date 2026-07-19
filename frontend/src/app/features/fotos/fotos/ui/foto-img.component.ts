import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { blobObjectUrl } from '../../../../shared/util/blob-object-url';
import { FotosService, VarianteDerivado } from '../data/fotos.service';

/**
 * Imagen protegida de una foto (thumb o preview con watermark). Los endpoints de imagen requieren
 * el bearer, así que no se puede apuntar un `<img src>` directo a la API: se baja el blob
 * autenticado y se muestra vía object URL (revocado al cambiar de foto y al destruir — el
 * `onCleanup` del effect cubre ambos). Mientras carga muestra spinner; si falla (p. ej. la foto
 * todavía no está Lista), un placeholder.
 */
@Component({
  selector: 'tbi-foto-img',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatIconModule, MatProgressSpinnerModule],
  template: `
    @if (url(); as u) {
      <img [src]="u" [alt]="alt()" [style.object-fit]="fit()" />
    } @else if (cargando()) {
      <div class="placeholder"><mat-spinner diameter="28" /></div>
    } @else {
      <div class="placeholder"><mat-icon>image_not_supported</mat-icon></div>
    }
  `,
  styles: `
    :host {
      display: block;
    }

    img {
      display: block;
      width: 100%;
      height: 100%;
    }

    .placeholder {
      display: flex;
      align-items: center;
      justify-content: center;
      width: 100%;
      height: 100%;
      color: var(--mat-sys-on-surface-variant);
      background: var(--mat-sys-surface-variant);
    }
  `,
})
export class FotoImgComponent {
  private readonly fotosService = inject(FotosService);

  readonly fotoId = input.required<number>();
  readonly variante = input<VarianteDerivado>('thumb');
  readonly alt = input('');
  /** `cover` para la grilla (recorta), `contain` para el preview ampliado (entero). */
  readonly fit = input<'cover' | 'contain'>('cover');

  private readonly blobResource = rxResource({
    params: () => ({ id: this.fotoId(), variante: this.variante() }),
    stream: ({ params }) => this.fotosService.derivado(params.id, params.variante),
  });

  protected readonly cargando = computed(() => this.blobResource.isLoading());

  // value() LANZA si el resource está en error (p. ej. 404 de una foto aún no procesada):
  // hasValue() es el guard correcto.
  protected readonly url = blobObjectUrl(() =>
    this.blobResource.hasValue() ? this.blobResource.value() : null,
  );
}
