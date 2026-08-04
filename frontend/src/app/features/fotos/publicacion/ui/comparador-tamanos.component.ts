import { DecimalPipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  computed,
  effect,
  inject,
  signal,
} from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { TbiSliderComponent } from '../../../../shared/ui/tbi-slider/tbi-slider.component';
import {
  ResultadoComparador,
  TAMANOS_COMPARADOR,
  calcularDimensionesDestino,
  calcularDpi,
  formatearPeso,
} from '../domain/comparador-tamanos.util';

/** Umbral bajo el cual la impresión en 10×15 se ve visiblemente borrosa (guarda informativa, no bloqueante). */
const DPI_ACEPTABLE = 150;

interface ResultadoVista extends ResultadoComparador {
  previewUrl: string;
}

/**
 * Comparador de tamaños (spec 8.3): sobre una foto real elegida por el fotógrafo, muestra lo que
 * realmente se generaría en cada lado mayor — dimensiones, dpi equivalente al imprimir en 10×15 y
 * peso ya pasado por el encoder WebP (mismo criterio D8 que la vista previa de marca de agua: mostrar
 * el resultado real, no una estimación).
 */
// TODO (Fase 4 - i18n): textos en español por ahora.
@Component({
  selector: 'tbi-comparador-tamanos',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DecimalPipe, MatButtonModule, MatIconModule, MatProgressSpinnerModule, TbiSliderComponent],
  templateUrl: './comparador-tamanos.component.html',
  styleUrl: './comparador-tamanos.component.scss',
})
export class ComparadorTamanosComponent {
  protected readonly archivoNombre = signal<string | null>(null);
  protected readonly calidad = signal(80);
  protected readonly procesando = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly resultados = signal<ResultadoVista[]>([]);
  protected readonly formatearPeso = formatearPeso;
  protected readonly DPI_ACEPTABLE = DPI_ACEPTABLE;

  private readonly imagenBitmap = signal<ImageBitmap | null>(null);
  protected readonly tieneFoto = computed(() => this.imagenBitmap() !== null);

  private urlsPrevias: string[] = [];

  constructor() {
    inject(DestroyRef).onDestroy(() => this.liberarPreviews());

    // Recalcula al elegir foto y cada vez que se mueve el slider de calidad — mismo criterio que
    // `PerfilMarcaAguaCanvasComponent`: un effect que lee los signals de entrada y dispara el
    // trabajo async de canvas/encoder.
    effect(() => {
      const imagen = this.imagenBitmap();
      const calidad = this.calidad();
      if (imagen) {
        void this.recalcular(imagen, calidad);
      }
    });
  }

  protected async elegirArchivo(event: Event): Promise<void> {
    const input = event.target as HTMLInputElement;
    const archivo = input.files?.[0];
    input.value = ''; // permite re-elegir el mismo archivo si hace falta
    if (!archivo) {
      return;
    }
    this.error.set(null);
    try {
      const bitmap = await createImageBitmap(archivo);
      this.imagenBitmap()?.close();
      this.imagenBitmap.set(bitmap);
      this.archivoNombre.set(archivo.name);
    } catch {
      this.error.set('No se pudo leer la imagen. Probá con otro archivo.');
    }
  }

  private async recalcular(imagen: ImageBitmap, calidad: number): Promise<void> {
    this.procesando.set(true);
    try {
      const resultados = await Promise.all(
        TAMANOS_COMPARADOR.map((ladoMayor) => this.procesarTamano(imagen, ladoMayor, calidad)),
      );
      this.liberarPreviews();
      this.urlsPrevias = resultados.map((r) => r.previewUrl);
      this.resultados.set(resultados);
    } finally {
      this.procesando.set(false);
    }
  }

  private async procesarTamano(
    imagen: ImageBitmap,
    ladoMayorPedido: number,
    calidad: number,
  ): Promise<ResultadoVista> {
    const { ancho, alto } = calcularDimensionesDestino(imagen.width, imagen.height, ladoMayorPedido);
    const canvas = document.createElement('canvas');
    canvas.width = ancho;
    canvas.height = alto;
    canvas.getContext('2d')?.drawImage(imagen, 0, 0, ancho, alto);
    const blob = await new Promise<Blob | null>((resolve) =>
      canvas.toBlob(resolve, 'image/webp', calidad / 100),
    );
    return {
      ladoMayorPedido,
      ancho,
      alto,
      dpi: calcularDpi(Math.max(ancho, alto)),
      pesoBytes: blob?.size ?? 0,
      previewUrl: URL.createObjectURL(blob ?? new Blob()),
    };
  }

  private liberarPreviews(): void {
    for (const url of this.urlsPrevias) {
      URL.revokeObjectURL(url);
    }
    this.urlsPrevias = [];
  }
}
