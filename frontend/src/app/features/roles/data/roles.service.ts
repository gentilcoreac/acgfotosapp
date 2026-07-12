import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiClient, injectCrudClient } from '../../../core/http';
import { TbiTreeNode } from '../../../shared/ui/tbi-tree-select/tbi-tree-select.component';
import { Rol } from '../domain/rol.model';

/**
 * Servicio de datos de Roles. Expone el `CrudClient` (entidad `roles` del módulo `general`)
 * que consumen la lista (`getAllByCriteria`) y el edit (`getById`/`save`), más el árbol de
 * permisos para el selector del ABM.
 *
 * El árbol de permisos lo arma **la API**: `GET api/general/permisos/hierarchical-items`
 * devuelve `HierarchicalItem<long>` ya anidado (`id`, `name`, `children`), por lo que no hay
 * que construir la jerarquía en el cliente (el listado de `permisos` ya no expone
 * `permisoPadreId`, así que armarlo client-side no sería viable de todos modos).
 */
@Injectable({ providedIn: 'root' })
export class RolesService {
  readonly crud = injectCrudClient<Rol>('roles');
  private readonly api = inject(ApiClient);

  /** Árbol jerárquico de permisos (ya armado por la API) para el `tbi-tree-select`. */
  getPermisosTree(): Observable<TbiTreeNode[]> {
    return this.api.get<TbiTreeNode[]>('general/permisos/hierarchical-items');
  }

  /**
   * Cambia el flag `esDefaultParaNuevoTenant` de un rol vía el endpoint dedicado
   * (`RolInputDto` no lo acepta). La API lo restringe a **root** (`AppContext.IsRoot`);
   * el cliente solo muestra la acción a root como UX, pero la autorización la valida la API.
   */
  setDefaultTenant(id: number, esDefaultParaNuevoTenant: boolean): Observable<void> {
    return this.api.post<void>(`general/roles/${id}/set-default-tenant`, {
      esDefaultParaNuevoTenant,
    });
  }
}
