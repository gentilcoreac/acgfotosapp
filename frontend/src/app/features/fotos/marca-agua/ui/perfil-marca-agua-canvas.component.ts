import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  effect,
  inject,
  input,
  viewChild,
} from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { of } from 'rxjs';
import { MarcaAguaService } from '../data/marca-agua.service';
import { CapaComposicion, componerMarcaAgua } from '../domain/marca-agua-canvas.util';
import { MuestraVariante, dibujarFotoMuestra } from '../domain/marca-agua-muestra.util';
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
  readonly ancho = input(320);
  readonly alto = input(213);
  /** Pasa el render por el encoder WebP antes de mostrarlo (D8: la compresión se come las marcas sutiles). */
  readonly comprimir = input(false);
  readonly calidad = input(55);
  readonly ariaLabel = input('Vista previa de la marca de agua');

  private readonly lienzo = viewChild.required<ElementRef<HTMLCanvasElement>>('lienzo');

  private readonly composicionResource = rxResource({
    params: () => ({ perfilId: this.perfil().id, capas: this.perfil().capas }),
    stream: ({ params }) =>
      params.perfilId == null ? of<CapaComposicion[]>([]) : this.service.cargarComposicion(this.perfil()),
  });

  constructor() {
    effect(() => {
      const composicion = this.composicionResource.hasValue() ? this.composicionResource.value() : [];
      const variante = this.variante();
      const ancho = this.ancho();
      const alto = this.alto();
      const comprimir = this.comprimir();
      const calidad = this.calidad();
      void this.render(composicion, variante, ancho, alto, comprimir, calidad);
    });
  }

  private async render(
    composicion: CapaComposicion[],
    variante: MuestraVariante,
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
    dibujarFotoMuestra(ctx, ancho, alto, variante);
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
