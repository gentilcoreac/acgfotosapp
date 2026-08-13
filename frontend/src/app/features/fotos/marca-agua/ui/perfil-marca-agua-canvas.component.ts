import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  computed,
  effect,
  inject,
  input,
  viewChild,
} from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { of } from 'rxjs';
import { MarcaAguaService } from '../data/marca-agua.service';
import { CapaComposicion, componerMarcaAgua } from '../domain/marca-agua-canvas.util';
import {
  ImagenDecodificada,
  MuestraVariante,
  dibujarFotoMuestra,
  dibujarFotoPropia,
} from '../domain/marca-agua-muestra.util';
import { PerfilMarcaAgua } from '../domain/marca-agua.model';

/**
 * Renderiza un perfil de marca de agua compuesto sobre una foto de muestra (spec: "la marca real
 * renderizada", no una descripción). Presentacional puro — el toggle "ver comprimido" y el
 * selector de muestra los maneja el caller (editor/listado).
 */
@Component({
  selector: 'tbi-perfil-marca-agua-canvas',
  changeDetection: ChangeDetectionStrategy.OnPush,
  // Sin bindings de [width]/[height]: `render()` fija `canvas.width`/`canvas.height` a mano, en el
  // mismo lugar y momento donde dibuja (ver el comentario en `render()` — evita la carrera con el
  // binding del template, que podía limpiar el canvas después de haber dibujado).
  template: `<canvas #lienzo [attr.aria-label]="ariaLabel()"></canvas>`,
  styles: `
    :host {
      display: block;
    }
    canvas {
      width: 100%;
      height: auto;
      display: block;
      border-radius: 4px;
    }
  `,
})
export class PerfilMarcaAguaCanvasComponent {
  private readonly service = inject(MarcaAguaService);

  readonly perfil = input.required<PerfilMarcaAgua>();
  readonly variante = input<MuestraVariante>('mixta');
  /** Si viene, se dibuja ESTA foto ("probar con una foto mía", spec 7.5/11.4) en vez de la muestra sintética de `variante`. */
  readonly fotoPropia = input<ImagenDecodificada | null>(null);
  readonly ancho = input(320);
  readonly alto = input(213);
  /** Pasa el render por el encoder WebP antes de mostrarlo (D8: la compresión se come las marcas sutiles). */
  readonly comprimir = input(false);
  readonly calidad = input(55);
  readonly ariaLabel = input('Vista previa de la marca de agua');

  private readonly lienzo = viewChild.required<ElementRef<HTMLCanvasElement>>('lienzo');

  // La clave es qué assets hay que bajar, NO el perfil entero: la colocación (escala, separación,
  // ángulo, opacidad) cambia con cada movimiento de slider y crea objetos nuevos, pero el PNG de una
  // capa es inmutable — cambiar su contenido significa subir otra capa, con otro storageKey. Comparar
  // el perfil entero hacía que cada tick de un slider volviera a bajar todos los assets, por cada
  // vista previa en pantalla.
  private readonly assetsPedidos = computed(() => {
    const perfil = this.perfil();
    return perfil.id == null ? '' : `${perfil.id}:${perfil.capas.map((c) => c.storageKey).join(',')}`;
  });

  private readonly assetsResource = rxResource({
    params: () => this.assetsPedidos(),
    stream: ({ params }) =>
      params === '' ? of(new Map<string, ImageBitmap>()) : this.service.cargarAssets(this.perfil()),
  });

  /** Colocación del momento + imagen ya bajada: lo primero cambia con cada ajuste, lo segundo no. */
  private readonly composicion = computed<CapaComposicion[]>(() => {
    const assets = this.assetsResource.hasValue()
      ? this.assetsResource.value()
      : new Map<string, ImageBitmap>();
    const composicion: CapaComposicion[] = [];
    for (const capa of this.perfil().capas) {
      const imagen = assets.get(capa.storageKey);
      if (imagen) {
        composicion.push({ capa, imagen });
      }
    }
    return composicion;
  });

  // El repintado comprimido termina de forma asincrónica, así que dos ajustes seguidos dejan dos
  // repintados en vuelo y el último en terminar no es necesariamente el más nuevo: sin este número
  // de orden, un resultado viejo pisa al actual y la vista previa queda congelada.
  private ultimoRender = 0;

  constructor() {
    effect(() => {
      const composicion = this.composicion();
      const variante = this.variante();
      const fotoPropia = this.fotoPropia();
      const ancho = this.ancho();
      const alto = this.alto();
      const comprimir = this.comprimir();
      const calidad = this.calidad();
      void this.render(++this.ultimoRender, composicion, variante, fotoPropia, ancho, alto, comprimir, calidad);
    });
  }

  private async render(
    render: number,
    composicion: CapaComposicion[],
    variante: MuestraVariante,
    fotoPropia: ImagenDecodificada | null,
    ancho: number,
    alto: number,
    comprimir: boolean,
    calidad: number,
  ): Promise<void> {
    const canvas = this.lienzo().nativeElement;
    // El tamaño del canvas se fija ACÁ, imperativamente — no como binding de template (bug real
    // encontrado en vivo, 2026-08-12): con un binding `[width]="ancho()"`, cuando este efecto corre
    // de forma inmediata en su primera pasada (assets ya en caché — pasa al abrir
    // `MarcaAguaPreviewAmpliadaDialogComponent` sobre un perfil que el listado ya renderizó), dibuja
    // ANTES de que Angular aplique el binding sobre el elemento recién creado; cuando lo aplica
    // después, redimensionar el canvas limpia su bitmap por spec (aunque el valor final sea el
    // mismo), borrando lo recién pintado. Fijar el tamaño acá, en el mismo lugar donde se dibuja,
    // saca la carrera de en medio.
    if (canvas.width !== ancho) {
      canvas.width = ancho;
    }
    if (canvas.height !== alto) {
      canvas.height = alto;
    }
    const ctx = canvas.getContext('2d');
    if (!ctx) {
      return;
    }

    ctx.clearRect(0, 0, ancho, alto);
    if (fotoPropia) {
      dibujarFotoPropia(ctx, ancho, alto, fotoPropia);
    } else {
      dibujarFotoMuestra(ctx, ancho, alto, variante);
    }
    componerMarcaAgua(ctx, ancho, alto, composicion);

    if (!comprimir) {
      return;
    }

    // D8: la vista previa muestra la imagen YA pasada por el encoder WebP — color sólido y
    // opacidad plana sobreviven a la compresión, la sombra difusa es la primera víctima.
    const blob = await new Promise<Blob | null>((resolve) =>
      canvas.toBlob(resolve, 'image/webp', calidad / 100),
    );
    if (!blob || render !== this.ultimoRender) {
      return;
    }
    const bitmap = await createImageBitmap(blob);
    if (render !== this.ultimoRender) {
      return;
    }
    ctx.clearRect(0, 0, ancho, alto);
    ctx.drawImage(bitmap, 0, 0, ancho, alto);
  }
}
