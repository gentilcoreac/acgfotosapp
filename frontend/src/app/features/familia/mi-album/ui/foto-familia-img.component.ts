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
import {
  FamiliaGaleriaService,
  FamiliaSessionStore,
  VarianteDerivadoFamilia,
} from '../../../../core/familia';

/**
 * Tamaño del tile del patrón repetido (grilla, no una sola marca centrada): imita el criterio del
 * watermark horneado del backend (`docs/05-notas-abiertas.md`, "más marcas, cada una más discreta").
 */
const MARCA_TILE_WIDTH = 150;
const MARCA_TILE_HEIGHT = 70;

/**
 * Imagen protegida de la galería de familia (thumb o preview con watermark). Mismo patrón que
 * `tbi-foto-img` (admin, `features/fotos/fotos/ui/foto-img.component.ts`) pero contra
 * `FamiliaGaleriaService`: el endpoint requiere el bearer de la sesión de familia, así que no sirve
 * un `<img src>` directo — se baja el blob y se muestra vía object URL (revocado al cambiar de foto
 * y al destruir).
 *
 * Segunda marca de agua (ADR acordado 2026-07-16, docs/05-notas-abiertas.md): capa CSS con
 * "Familia de {participante(s)}" tileada sobre la imagen. El nombre sale de `FamiliaSessionStore`
 * (la sesión canjeada), NO del derivado — por eso cubre también las fotos grupales, que no tienen
 * un participante propio para hornear en el back. No reemplaza el watermark horneado (ese es el que
 * sobrevive a devtools/blob); esta capa apunta al ataque más común, la captura de pantalla: se lleva
 * el nombre de la familia responsable puesto.
 */
@Component({
  selector: 'tbi-foto-familia-img',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatIconModule, MatProgressSpinnerModule],
  template: `
    @if (url(); as u) {
      <img [src]="u" [alt]="alt()" [style.object-fit]="fit()" />
      @if (marcaBackground(); as bg) {
        <div class="marca-familia" aria-hidden="true" [style.background-image]="bg"></div>
      }
    } @else if (cargando()) {
      <div class="placeholder"><mat-spinner diameter="28" /></div>
    } @else {
      <div class="placeholder"><mat-icon>image_not_supported</mat-icon></div>
    }
  `,
  styles: `
    :host {
      display: block;
      position: relative;
      overflow: hidden;
    }

    img {
      display: block;
      width: 100%;
      height: 100%;
    }

    .marca-familia {
      position: absolute;
      inset: 0;
      z-index: 1;
      background-repeat: repeat;
      background-position: center;
      background-size: 150px 70px; /* = MARCA_TILE_WIDTH x MARCA_TILE_HEIGHT */
      pointer-events: none;
      user-select: none;
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
  private readonly sessionStore = inject(FamiliaSessionStore);

  readonly fotoId = input.required<number>();
  readonly variante = input<VarianteDerivadoFamilia>('thumb');
  readonly alt = input('');
  /** `cover` para la grilla (recorta), `contain` para el preview ampliado (entero). */
  readonly fit = input<'cover' | 'contain'>('cover');

  /** `null` si por algún motivo no hay sesión activa (no debería pasar: sin sesión no hay fotos). */
  protected readonly marcaTexto = computed<string | null>(() => {
    const nombres = this.sessionStore
      .participantes()
      .map((p) => p.nombre)
      .join(' y ');
    return nombres ? `Familia de ${nombres}` : null;
  });

  protected readonly marcaBackground = computed<string | null>(() => {
    const texto = this.marcaTexto();
    return texto ? `url("${this.buildTileDataUri(texto)}")` : null;
  });

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

  /**
   * SVG chico con el texto rotado, como `background-image` en vez de tiled con nodos DOM: una foto
   * de la grilla se pinta muchas veces por sesión (una por miniatura), así que evitar spans extra por
   * tile importa para gama baja. El navegador repite este SVG solo (`background-repeat: repeat`,
   * ver `.marca-familia` en los estilos) — no hace falta calcular cuántos tiles entran.
   */
  private buildTileDataUri(texto: string): string {
    const escapado = texto
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/>/g, '&gt;');
    const cx = MARCA_TILE_WIDTH / 2;
    const cy = MARCA_TILE_HEIGHT / 2;
    const svg =
      `<svg xmlns="http://www.w3.org/2000/svg" width="${MARCA_TILE_WIDTH}" height="${MARCA_TILE_HEIGHT}">` +
      `<text x="${cx}" y="${cy}" transform="rotate(-18 ${cx} ${cy})" text-anchor="middle" ` +
      `dominant-baseline="middle" font-family="sans-serif" font-size="12" fill="white" ` +
      `fill-opacity="0.45" stroke="black" stroke-opacity="0.25" stroke-width="0.5">${escapado}</text>` +
      `</svg>`;
    return `data:image/svg+xml,${encodeURIComponent(svg)}`;
  }
}
