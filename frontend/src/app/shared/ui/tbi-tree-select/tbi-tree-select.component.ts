import { NgTemplateOutlet } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  input,
  model,
  output,
  signal,
} from '@angular/core';
import { FormValueControl } from '@angular/forms/signals';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatIconModule } from '@angular/material/icon';

/**
 * Nodo del árbol de selección. Espejo de `HierarchicalItem<long>` de la API
 * (`GET api/general/permisos/hierarchical-items`): `id`, `name` y `children` anidados.
 */
export interface TbiTreeNode {
  id: number;
  name: string;
  children?: TbiTreeNode[];
}

/**
 * Selector múltiple en árbol jerárquico (reemplaza a `mvz-multiple-tree-view` del original).
 * `FormValueControl` nativo cuyo valor es el **array de ids seleccionados** (`number[]`): incluye
 * tanto hojas como padres cuando están marcados (en el original, cada nodo del árbol es una
 * entidad real con id —p. ej. un permiso— y el padre se asigna también al marcarse).
 *
 * Comportamiento (cascada, como el original):
 * - Marcar/desmarcar un nodo propaga a todos sus descendientes.
 * - Un padre queda **marcado** sólo si todos sus hijos lo están; **indeterminado** si algunos.
 *
 * Se construyó sobre Material (`mat-checkbox`) detrás del wrapper `tbi-*` y es reutilizable
 * para otras features con jerarquías (menú, edición de permisos).
 */
@Component({
  selector: 'tbi-tree-select',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [NgTemplateOutlet, MatCheckboxModule, MatIconModule, MatButtonModule],
  templateUrl: './tbi-tree-select.component.html',
  styleUrl: './tbi-tree-select.component.scss',
})
export class TbiTreeSelectComponent implements FormValueControl<number[]> {
  readonly nodes = input.required<TbiTreeNode[]>();

  readonly value = model<number[]>([]);
  readonly disabled = input<boolean>(false);
  readonly touch = output<void>();

  protected readonly expanded = signal<ReadonlySet<number>>(new Set());

  /** Ids seleccionados normalizados: un padre queda marcado sólo si lo están todos sus hijos. */
  protected readonly selected = computed(() => {
    const set = new Set(this.value());
    this.normalizeParents(set);
    return set;
  });

  /** Índice `id → nodo` y `id → idPadre` para reconciliar ancestros al togglear. */
  private readonly index = computed(() => {
    const byId = new Map<number, TbiTreeNode>();
    const parentOf = new Map<number, number | null>();
    const walk = (list: TbiTreeNode[], parent: number | null): void => {
      for (const node of list) {
        byId.set(node.id, node);
        parentOf.set(node.id, parent);
        if (node.children?.length) {
          walk(node.children, node.id);
        }
      }
    };
    walk(this.nodes(), null);
    return { byId, parentOf };
  });

  protected hasChildren(node: TbiTreeNode): boolean {
    return !!node.children?.length;
  }

  protected isChecked(node: TbiTreeNode): boolean {
    return this.selected().has(node.id);
  }

  protected isIndeterminate(node: TbiTreeNode): boolean {
    return !this.isChecked(node) && this.someDescendantSelected(node);
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

  protected toggle(node: TbiTreeNode, checked: boolean): void {
    const next = new Set(this.selected());
    this.setSubtree(node, checked, next);
    this.reconcileAncestors(node, next);
    this.value.set([...next]);
    this.touch.emit();
  }

  /** Marca/desmarca el nodo y todo su subárbol. */
  private setSubtree(node: TbiTreeNode, checked: boolean, set: Set<number>): void {
    if (checked) {
      set.add(node.id);
    } else {
      set.delete(node.id);
    }
    node.children?.forEach((child) => this.setSubtree(child, checked, set));
  }

  /** Recalcula la membresía de cada ancestro: padre marcado ⇔ todos sus hijos marcados. */
  private reconcileAncestors(node: TbiTreeNode, set: Set<number>): void {
    const { byId, parentOf } = this.index();
    let parentId = parentOf.get(node.id) ?? null;
    while (parentId != null) {
      const parent = byId.get(parentId);
      if (!parent?.children) {
        break;
      }
      if (parent.children.every((child) => set.has(child.id))) {
        set.add(parent.id);
      } else {
        set.delete(parent.id);
      }
      parentId = parentOf.get(parentId) ?? null;
    }
  }

  private someDescendantSelected(node: TbiTreeNode): boolean {
    const set = this.selected();
    return (
      node.children?.some((child) => set.has(child.id) || this.someDescendantSelected(child)) ??
      false
    );
  }

  /** Normaliza la membresía de los padres a partir de las hojas (post-order). */
  private normalizeParents(set: Set<number>): void {
    const visit = (node: TbiTreeNode): void => {
      if (!node.children?.length) {
        return;
      }
      node.children.forEach(visit);
      if (node.children.every((child) => set.has(child.id))) {
        set.add(node.id);
      } else {
        set.delete(node.id);
      }
    };
    this.nodes().forEach(visit);
  }
}
