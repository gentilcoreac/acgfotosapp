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
import { MatDialogModule } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';

/** Contexto del `<ng-template>` proyectado: el item actual y su posición. */
export interface TbiVisorItemContext<T> {
  $implicit: T;
  index: number;
}

/**
 * Visor de fotos del sistema: abrir una miniatura en grande y recorrer la colección desde ahí.
 * Aporta SÓLO el chrome común —cerrar, contador, flechas, teclado, la foto entera sin scroll— y
 * delega en el caller qué se dibuja (`<ng-template let-item>`) y qué acciones ofrece (slots
 * `visorFooter`, `visorEsquinaInferiorIzquierda`, `visorEsquinaInferiorDerecha`, `visorBajoContador`).
 *
 * Esa división es a propósito: lo que distingue al admin de las familias no es cómo se navega sino
 * qué se muestra y qué se puede hacer (el admin ve el original y lo descarga; la familia ve el
 * derivado con marca y agrega al carrito). Con banderas `esAdmin`/`esFamilia` adentro, esa distinción
 * viviría en una pieza compartida y un día se filtraría al flujo de las familias — el mismo criterio
 * por el que "modo fotógrafo" quedó como pantalla aparte (docs/05-notas-abiertas.md).
 *
 * Va dentro de un diálogo: por eso escucha las flechas en `document` (a diferencia de `tbi-carousel`,
 * pensado para uso inline junto a otros controles).
 */
@Component({
  selector: 'tbi-visor-fotos',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatButtonModule, MatDialogModule, MatIconModule, NgTemplateOutlet],
  host: {
    '(document:keydown.arrowleft)': 'anterior()',
    '(document:keydown.arrowright)': 'siguiente()',
  },
  templateUrl: './tbi-visor-fotos.component.html',
  styleUrl: './tbi-visor-fotos.component.scss',
})
export class TbiVisorFotosComponent<T> {
  readonly items = input.required<readonly T[]>();
  /** Índice actual, bindable con `[(index)]`. */
  readonly index = model(0);
  readonly ariaLabel = input('Visor de fotos');

  readonly itemTemplate = contentChild.required(TemplateRef<TbiVisorItemContext<T>>);

  protected readonly indiceActual = computed(() => {
    const total = this.items().length;
    return total === 0 ? 0 : Math.min(Math.max(this.index(), 0), total - 1);
  });

  readonly actual = computed<T | undefined>(() => this.items()[this.indiceActual()]);

  protected readonly hayVarias = computed(() => this.items().length > 1);

  protected readonly contexto = computed<TbiVisorItemContext<T> | null>(() => {
    const actual = this.actual();
    return actual === undefined ? null : { $implicit: actual, index: this.indiceActual() };
  });

  protected anterior(): void {
    const total = this.items().length;
    if (total > 1) {
      this.index.set((this.indiceActual() - 1 + total) % total);
    }
  }

  protected siguiente(): void {
    const total = this.items().length;
    if (total > 1) {
      this.index.set((this.indiceActual() + 1) % total);
    }
  }
}
