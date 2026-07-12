import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { ApiClient, injectCrudClient } from '../../../core/http';
import { RolOption, TipoLicencia } from '../domain/tipo-licencia.model';

/**
 * Servicio de datos de Tipos de Licencia. Expone el `CrudClient` (entidad `tipos-licencia`
 * del módulo `general`) que consumen la lista (`getAllByCriteria`) y el edit (`getById`/`save`),
 * y un lookup de roles (`api/general/roles`) para los checkboxes del ABM.
 */
@Injectable({ providedIn: 'root' })
export class TiposLicenciaService {
  readonly crud = injectCrudClient<TipoLicencia>('tipos-licencia');
  private readonly rolesCrud = injectCrudClient<RolOption>('roles');
  private readonly api = inject(ApiClient);

  /** Todos los roles disponibles para asignar a un tipo de licencia. */
  getRoles(): Observable<RolOption[]> {
    return this.rolesCrud.getAll().pipe(map((result) => result.items));
  }

  /**
   * Cambia el flag `esDefaultParaNuevoTenant` de un tipo de licencia vía el endpoint dedicado
   * (`TipoLicenciaInputDto` no lo acepta). La API lo restringe a **root** (`AppContext.IsRoot`);
   * el cliente solo muestra la acción a root como UX, pero la autorización la valida la API.
   */
  setDefaultTenant(id: number, esDefaultParaNuevoTenant: boolean): Observable<void> {
    return this.api.post<void>(`general/tipos-licencia/${id}/set-default-tenant`, {
      esDefaultParaNuevoTenant,
    });
  }
}
