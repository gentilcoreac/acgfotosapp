import { NgTemplateOutlet } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  TemplateRef,
  computed,
  contentChild,
  input,
  model,
} from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

/** Contexto que recibe el `<ng-template>` proyectado: el item actual y su posición. */
export interface TbiCarouselItemContext<T> {
  $implicit: T;
  index: number;
}

/**
 * Visualizador/carrusel del design system: navegación anterior/siguiente + contador + flechas de
 * teclado, sin saber nada de qué es cada item — el caller decide cómo se renderiza con un
 * `<ng-template let-item>` proyectado. Extraído del shell de `FotoFamiliaPreviewDialogComponent`
 * (mi-album), que tiene el mismo patrón de navegación pero cableado directo al dominio de familia
 * (carrito, sesión, imagen autenticada) — ese diálogo sigue como está (grupo 11, design.md D15);
 * este componente es la versión reusable para el resto de la app (arranca en `/fotos/publicacion`).
 *
 * A diferencia del diálogo de familia (que escucha `document:keydown`, apropiado para un modal
 * pantalla completa), acá las flechas sólo navegan con el carrusel enfocado — este componente está
 * pensado también para uso INLINE en una página con otros controles (selects, sliders), y escuchar
 * en `document` interferiría con esos controles.
 */
@Component({
  selector: 'tbi-carousel',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatButtonModule, MatIconModule, NgTemplateOutlet],
  templateUrl: './tbi-carousel.component.html',
  styleUrl: './tbi-carousel.component.scss',
})
export class TbiCarouselComponent<T> {
  readonly items = input.required<readonly T[]>();
  /** Índice actual, bindable con `[(index)]`. Se clampea solo si `items` cambia de tamaño. */
  readonly index = model(0);
  readonly prevLabel = input('Anterior');
  readonly nextLabel = input('Siguiente');
  readonly ariaLabel = input('Visualizador');

  /** Único `<ng-template>` proyectado: el render de cada item, `let-item` (+ `let-index="index"`). */
  readonly itemTemplate = contentChild.required(TemplateRef<TbiCarouselItemContext<T>>);

  protected readonly indiceActual = computed(() => {
    const total = this.items().length;
    if (total === 0) {
      return 0;
    }
    return Math.min(Math.max(this.index(), 0), total - 1);
  });

  protected readonly actual = computed(() => this.items()[this.indiceActual()]);

  protected readonly contexto = computed<TbiCarouselItemContext<T>>(() => ({
    $implicit: this.actual(),
    index: this.indiceActual(),
  }));

  protected anterior(): void {
    const total = this.items().length;
    if (total <= 1) {
      return;
    }
    this.index.set((this.indiceActual() - 1 + total) % total);
  }

  protected siguiente(): void {
    const total = this.items().length;
    if (total <= 1) {
      return;
    }
    this.index.set((this.indiceActual() + 1) % total);
  }
}
