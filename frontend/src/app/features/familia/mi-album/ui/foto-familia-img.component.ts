import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
  input,
  signal,
} from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { FamiliaGaleriaService, VarianteDerivadoFamilia } from '../../../../core/familia';

/**
 * Imagen protegida de la galería de familia (thumb o preview con watermark). Mismo patrón que
 * `tbi-foto-img` (admin, `features/fotos/fotos/ui/foto-img.component.ts`) pero contra
 * `FamiliaGaleriaService`: el endpoint requiere el bearer de la sesión de familia, así que no sirve
 * un `<img src>` directo — se baja el blob y se muestra vía object URL (revocado al cambiar de foto
 * y al destruir).
 */
@Component({
  selector: 'tbi-foto-familia-img',
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
export class FotoFamiliaImgComponent {
  private readonly galeriaService = inject(FamiliaGaleriaService);

  readonly fotoId = input.required<number>();
  readonly variante = input<VarianteDerivadoFamilia>('thumb');
  readonly alt = input('');
  /** `cover` para la grilla (recorta), `contain` para el preview ampliado (entero). */
  readonly fit = input<'cover' | 'contain'>('cover');

  private readonly blobResource = rxResource({
    params: () => ({ id: this.fotoId(), variante: this.variante() }),
    stream: ({ params }) => this.galeriaService.derivado(params.id, params.variante),
  });

  protected readonly cargando = computed(() => this.blobResource.isLoading());
  protected readonly url = signal<string | null>(null);

  constructor() {
    effect((onCleanup) => {
      const blob = this.blobResource.hasValue() ? this.blobResource.value() : null;
      const objectUrl = blob ? URL.createObjectURL(blob) : null;
      this.url.set(objectUrl);
      onCleanup(() => {
        if (objectUrl) {
          URL.revokeObjectURL(objectUrl);
        }
      });
    });
  }
}
