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
  template: `<canvas #lienzo [width]="ancho()" [height]="alto()" [attr.aria-label]="ariaLabel()"></canvas>`,
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
    return this.perfil()
      .capas.map((capa) => ({ capa, imagen: assets.get(capa.storageKey) }))
      .filter((c): c is CapaComposicion => c.imagen != null);
  });

  constructor() {
    effect(() => {
      const composicion = this.composicion();
      const variante = this.variante();
      const fotoPropia = this.fotoPropia();
      const ancho = this.ancho();
      const alto = this.alto();
      const comprimir = this.comprimir();
      const calidad = this.calidad();
      void this.render(composicion, variante, fotoPropia, ancho, alto, comprimir, calidad);
    });
  }

  private async render(
    composicion: CapaComposicion[],
    variante: MuestraVariante,
    fotoPropia: ImagenDecodificada | null,
    ancho: number,
    alto: number,
    comprimir: boolean,
    calidad: number,
  ): Promise<void> {
    const canvas = this.lienzo().nativeElement;
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
    if (!blob) {
      return;
    }
    const bitmap = await createImageBitmap(blob);
    ctx.clearRect(0, 0, ancho, alto);
    ctx.drawImage(bitmap, 0, 0, ancho, alto);
  }
}
