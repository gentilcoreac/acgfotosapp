import { NgTemplateOutlet } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  effect,
  input,
  model,
  output,
  signal,
} from '@angular/core';
import { FormValueControl } from '@angular/forms/signals';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatIconModule } from '@angular/material/icon';
import { TbiTreeNode } from '../tbi-tree-select/tbi-tree-select.component';

/**
 * Selector **único** en árbol jerárquico (reemplaza al `mvz-tree-view` single-select del original,
 * p. ej. "permiso padre"). `FormValueControl` nativo cuyo valor es **un id** (`number | null`).
 *
 * - Sólo un nodo puede estar marcado a la vez; marcar otro reemplaza la selección.
 * - Desmarcar el nodo activo deja la selección en `null` (sin padre).
 * - Los nodos con hijos arrancan expandidos.
 *
 * Usa `mat-checkbox` (como el original) detrás del wrapper `tbi-*`. Reutiliza `TbiTreeNode`.
 */
@Component({
  selector: 'tbi-tree-radio',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [NgTemplateOutlet, MatCheckboxModule, MatIconModule, MatButtonModule],
  templateUrl: './tbi-tree-radio.component.html',
  styleUrl: './tbi-tree-radio.component.scss',
})
export class TbiTreeRadioComponent implements FormValueControl<number | null> {
  readonly nodes = input.required<TbiTreeNode[]>();

  readonly value = model<number | null>(null);
  readonly disabled = input<boolean>(false);
  readonly touch = output<void>();

  protected readonly expanded = signal<ReadonlySet<number>>(new Set());

  constructor() {
    // Expandir por defecto todos los nodos con hijos cada vez que cambia el árbol.
    effect(() => {
      const ids = new Set<number>();
      const walk = (list: TbiTreeNode[]): void => {
        for (const node of list) {
          if (node.children?.length) {
            ids.add(node.id);
            walk(node.children);
          }
        }
      };
      walk(this.nodes());
      this.expanded.set(ids);
    });
  }

  protected hasChildren(node: TbiTreeNode): boolean {
    return !!node.children?.length;
  }

  protected isChecked(node: TbiTreeNode): boolean {
    return this.value() === node.id;
  }

  protected isExpanded(node: TbiTreeNode): boolean {
    return this.expanded().has(node.id);
  }

  protected toggleExpand(node: TbiTreeNode): void {
    this.expanded.update((current) => {
      const next = new Set(current);
      if (next.has(node.id)) {
        next.delete(node.id);
      } else {
        next.add(node.id);
      }
      return next;
    });
  }

  protected select(node: TbiTreeNode, checked: boolean): void {
    this.value.set(checked ? node.id : null);
    this.touch.emit();
  }
}
