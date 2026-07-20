import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
  signal,
} from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { FormField, form, maxLength, pattern, required } from '@angular/forms/signals';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule } from '@angular/material/dialog';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatTabsModule } from '@angular/material/tabs';
import { Observable, catchError, of } from 'rxjs';
import { AuthStore } from '../../../core/auth';
import { EditComponentBase } from '../../../shared/forms/edit-component-base';
import { TbiButtonComponent } from '../../../shared/ui/tbi-button/tbi-button.component';
import {
  TbiSelectComponent,
  TbiSelectOption,
} from '../../../shared/ui/tbi-select/tbi-select.component';
import { TbiTextFieldComponent } from '../../../shared/ui/tbi-text-field/tbi-text-field.component';
import { TbiTreeRadioComponent } from '../../../shared/ui/tbi-tree-radio/tbi-tree-radio.component';
import {
  TbiTreeNode,
  TbiTreeSelectComponent,
} from '../../../shared/ui/tbi-tree-select/tbi-tree-select.component';
import { lookupResource } from '../../../shared/util/lookup-resource';
import { PermisosService } from '../data/permisos.service';
import { ApiHierarchicalItem, Permiso, PermisoDeAplicacion } from '../domain/permiso.model';

interface PermisoFormModel {
  nombre: string;
  codigoPermiso: string;
  descripcion: string;
  activo: boolean;
  aplicacionId: number | null;
  permisoPadreId: number | null;
  /** tbi-tree-select emite ids de hojas (endpoints reales) + ids sintéticos de los agrupadores. */
  endpoints: number[];
  /** Flag reservado a root; se persiste por endpoint aparte (ver afterSave), no en el update. */
  esRestringido: boolean;
}

// TODO (Fase 4 - i18n): textos en español por ahora.
@Component({
  selector: 'tbi-permiso-edit',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FormField,
    MatDialogModule,
    MatButtonModule,
    MatProgressBarModule,
    MatSlideToggleModule,
    MatTabsModule,
    TbiTextFieldComponent,
    TbiSelectComponent,
    TbiTreeRadioComponent,
    TbiTreeSelectComponent,
    TbiButtonComponent,
  ],
  templateUrl: './permiso-edit.component.html',
  styleUrl: './permiso-edit.component.scss',
})
export class PermisoEditComponent extends EditComponentBase<Permiso, PermisoFormModel> {
  private readonly service = inject(PermisosService);
  protected readonly crud = this.service.crud;
  private loaded?: Permiso;

  /** Solo root puede editar `esRestringido` (la API lo valida; acá es UX). */
  protected readonly isRoot = inject(AuthStore).isRoot;
  private originalEsRestringido = false;

  private readonly aplicacionesResource = lookupResource(() => this.service.getAplicaciones(), []);
  protected readonly aplicacionOptions = computed<TbiSelectOption<number>[]>(() =>
    (this.aplicacionesResource.value() ?? []).map((a) => ({ value: a.id, label: a.nombre })),
  );
  /** Árbol de permiso padre (single-select), filtrado por la aplicación elegida y sin el propio. */
  protected readonly padreTree = signal<TbiTreeNode[]>([]);
  /** Árbol Módulo→Controller→Endpoint (multi-select); ids sintéticos en los nodos agrupadores. */
  private readonly endpointsResource = lookupResource(() => this.service.getEndpointsTree(), []);
  protected readonly endpointsTree = computed<TbiTreeNode[]>(() =>
    this.remapEndpoints(this.endpointsResource.value() ?? []),
  );

  protected readonly model = signal<PermisoFormModel>({
    nombre: '',
    codigoPermiso: '',
    descripcion: '',
    activo: true,
    aplicacionId: null,
    permisoPadreId: null,
    endpoints: [],
    esRestringido: false,
  });

  // codigoPermiso es una clave referenciada por los checks de autorización: sin espacios ni
  // caracteres raros que puedan colar un typo silencioso.
  protected readonly form = form(this.model, (path) => {
    required(path.nombre, { message: 'Requerido' });
    maxLength(path.nombre, 100, { message: 'Máximo 100 caracteres' });
    required(path.codigoPermiso, { message: 'Requerido' });
    pattern(path.codigoPermiso, /^[A-Za-z0-9_.]+$/, {
      message: 'Solo letras, números, punto y guión bajo, sin espacios',
    });
    required(path.descripcion, { message: 'Requerido' });
    maxLength(path.descripcion, 500, { message: 'Máximo 500 caracteres' });
    required(path.aplicacionId, { message: 'Requerido' });
  });

  protected override invalidSubmitMessage =
    'Completá los campos obligatorios resaltados (revisá todas las pestañas).';

  /** Filtro del árbol de endpoints: mostrar sólo módulos/controllers con items seleccionados. */
  protected readonly showOnlySelected = signal(false);
  /** Árbol de endpoints que se muestra: completo, o sólo las ramas con hojas seleccionadas. */
  protected readonly endpointsTreeShown = computed(() => {
    if (!this.showOnlySelected()) {
      return this.endpointsTree();
    }
    // Dependencia fina (form.endpoints, no model() entero): evita recalcular el filtro del árbol
    // en cada tecla de Nombre/Descripción. Hojas reales = id > 0.
    const selectedLeaves = new Set(
      this.form
        .endpoints()
        .value()
        .filter((id) => id > 0),
    );
    return this.filterSelectedBranches(this.endpointsTree(), selectedLeaves);
  });

  /** Refetchea sola cuando cambia la aplicación elegida (cancela el pedido en vuelo anterior). */
  private readonly padreTreeResource = rxResource({
    params: () => this.form.aplicacionId().value(),
    stream: ({ params: aplicacionId }) =>
      aplicacionId == null
        ? of<PermisoDeAplicacion[] | null>(null)
        : this.service.getPermisosDeAplicacion(aplicacionId).pipe(
            // el toast de error lo emite el errorInterceptor (global)
            catchError(() => of<PermisoDeAplicacion[] | undefined>(undefined)),
          ),
  });

  /** El patch inicial (edición) arma esta aplicación ANTES de volcar el modelo: cuando el effect
   * corra y la aplicación ACTUAL siga siendo esta, no poda `permisoPadreId` — se compara contra el
   * valor vigente en vez de un booleano "consumido" (un booleano es frágil si dos cambios de
   * aplicación ocurren antes de que el effect llegue a correr para el primero: se coalescen en una
   * sola ejecución y el booleano se consumiría para el cambio equivocado). `undefined` = sin
   * supresión armada (distinto de `null`, que es "sin aplicación elegida", un id válido). */
  private suppressClearForAplicacionId: number | null | undefined = undefined;

  constructor() {
    super();
    // El permiso padre depende de la aplicación: al cambiarla se recarga su árbol y se limpia el
    // padre elegido (salvo el patch inicial, ver `suppressClearForAplicacionId`).
    effect(() => {
      const currentAplicacionId = this.form.aplicacionId().value();
      const permisos = this.padreTreeResource.value();
      if (permisos === undefined) {
        return;
      }
      const suppress = this.suppressClearForAplicacionId === currentAplicacionId;
      this.suppressClearForAplicacionId = undefined;
      if (!suppress) {
        this.model.update((m) => ({ ...m, permisoPadreId: null }));
      }
      this.padreTree.set(
        permisos === null ? [] : this.buildPadreTree(permisos, this.loaded?.id ?? null),
      );
    });
  }

  /** Arma la jerarquía de permisos por `permisoPadreId`, excluyendo al propio permiso editado. */
  private buildPadreTree(permisos: PermisoDeAplicacion[], selfId: number | null): TbiTreeNode[] {
    const usable = permisos.filter((p) => p.id !== selfId);
    const idSet = new Set(usable.map((p) => p.id));
    const childrenOf = (parentId: number): TbiTreeNode[] =>
      usable
        .filter((p) => (p.permisoPadreId ?? null) === parentId)
        .map((p) => this.toPadreNode(p, childrenOf));
    // Raíz: sin padre, o cuyo padre no está en el set (otra app o el propio excluido).
    const roots = usable.filter((p) => p.permisoPadreId == null || !idSet.has(p.permisoPadreId));
    return roots.map((p) => this.toPadreNode(p, childrenOf));
  }

  private toPadreNode(
    p: PermisoDeAplicacion,
    childrenOf: (parentId: number) => TbiTreeNode[],
  ): TbiTreeNode {
    const kids = childrenOf(p.id);
    return { id: p.id, name: p.nombre, children: kids.length ? kids : undefined };
  }

  /**
   * Mapea el árbol de la API a `TbiTreeNode[]`. Los nodos agrupadores (Módulo/Controller vienen
   * con `id = 0`) reciben ids sintéticos **negativos** únicos; las hojas conservan el `endpointId`
   * real (positivo). Así `tbi-tree-select` no colisiona y al guardar filtramos por id > 0.
   */
  private remapEndpoints(items: ApiHierarchicalItem[]): TbiTreeNode[] {
    let synthetic = -1;
    const map = (list: ApiHierarchicalItem[]): TbiTreeNode[] =>
      list.map((item) => {
        const hasChildren = !!item.children?.length;
        return {
          id: hasChildren ? synthetic-- : item.id,
          name: item.name,
          children: hasChildren ? map(item.children ?? []) : undefined,
        };
      });
    return map(items);
  }

  /** Deja sólo las ramas (Módulo/Controller) que contienen al menos una hoja seleccionada. */
  private filterSelectedBranches(nodes: TbiTreeNode[], selectedLeaves: Set<number>): TbiTreeNode[] {
    const result: TbiTreeNode[] = [];
    for (const node of nodes) {
      if (!node.children?.length) {
        if (selectedLeaves.has(node.id)) {
          result.push(node);
        }
      } else {
        const kids = this.filterSelectedBranches(node.children, selectedLeaves);
        if (kids.length) {
          result.push({ ...node, children: kids });
        }
      }
    }
    return result;
  }

  protected toEntity(): Permiso {
    // Endpoints: solo ids de hojas (endpoints reales, positivos); descarta los sintéticos.
    // codigoPermiso viaja (editable). esRestringido NO (endpoint dedicado).
    const value = this.model();
    return {
      ...this.loaded,
      nombre: value.nombre,
      codigoPermiso: value.codigoPermiso,
      descripcion: value.descripcion,
      activo: value.activo,
      aplicacionId: value.aplicacionId,
      permisoPadreId: value.permisoPadreId,
      endpoints: value.endpoints.filter((id) => id > 0).map((endpointId) => ({ endpointId })),
      esRestringido: undefined,
    };
  }

  protected patchForm(entity: Permiso): void {
    this.loaded = entity;
    this.originalEsRestringido = entity.esRestringido ?? false;
    // Arma la supresión para esta aplicación ANTES de volcar el modelo (el effect no poda el padre)
    // — `padreTreeResource` refetchea sola.
    this.suppressClearForAplicacionId = entity.aplicacionId ?? null;
    this.model.set({
      nombre: entity.nombre,
      codigoPermiso: entity.codigoPermiso ?? '',
      descripcion: entity.descripcion,
      activo: entity.activo ?? true,
      aplicacionId: entity.aplicacionId ?? null,
      permisoPadreId: entity.permisoPadreId ?? null,
      endpoints: (entity.endpoints ?? []).map((e) => e.endpointId),
      esRestringido: this.originalEsRestringido,
    });
  }

  /**
   * Tras el update normal, si el usuario es root y cambió el flag, lo persiste por el endpoint
   * dedicado `permisos/{id}/set-es-restringido`. Si no es root o no cambió, no hace nada.
   */
  protected override afterSave(saved: Permiso): Observable<unknown> {
    const value = this.model().esRestringido;
    if (!this.isRoot() || saved.id == null || value === this.originalEsRestringido) {
      return of(saved);
    }
    return this.service.setEsRestringido(saved.id, value);
  }
}
