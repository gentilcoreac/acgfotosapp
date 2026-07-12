import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { ApiClient, injectCrudClient } from '../../../core/http';
import {
  AplicacionOption,
  Tenant,
  TenantAdminInfo,
  TipoLicenciaOption,
} from '../domain/tenant.model';

/** Shape mínimo de `AplicacionDto` que consume el ABM (checkboxes de aplicaciones). */
interface AplicacionListItem {
  id: number;
  nombre: string;
}

/** Shape mínimo de `TipoLicenciaDto` que consume la subgrilla de licencias. */
interface TipoLicenciaListItem {
  id: number;
  descripcion: string;
  esDefaultParaNuevoTenant?: boolean;
}

/**
 * Servicio de datos de Tenants. Expone el `CrudClient` (entidad `tenants`) para lista
 * (`getAllByCriteria`) y edit (`getById`/`save`), más los lookups del ABM (aplicaciones y
 * tipos de licencia) y los endpoints dedicados de regeneración de temas (root-only en la API).
 */
@Injectable({ providedIn: 'root' })
export class TenantService {
  readonly crud = injectCrudClient<Tenant>('tenants');
  private readonly api = inject(ApiClient);
  private readonly aplicacionesCrud = injectCrudClient<AplicacionListItem>('aplicaciones');
  private readonly tiposLicenciaCrud = injectCrudClient<TipoLicenciaListItem>('tipos-licencia');

  /** Todas las aplicaciones disponibles (checkboxes de la pestaña Aplicaciones). */
  getAplicaciones(): Observable<AplicacionOption[]> {
    return this.aplicacionesCrud
      .getAll()
      .pipe(map((result) => result.items.map((a) => ({ id: a.id, nombre: a.nombre }))));
  }

  /** Tipos de licencia (select de la subgrilla + detección de las default para nuevo tenant). */
  getTiposLicencia(): Observable<TipoLicenciaOption[]> {
    return this.tiposLicenciaCrud.getAll().pipe(
      map((result) =>
        result.items.map((t) => ({
          id: t.id,
          descripcion: t.descripcion,
          esDefaultParaNuevoTenant: t.esDefaultParaNuevoTenant ?? false,
        })),
      ),
    );
  }

  /**
   * Administrador(es) del tenant (solo lectura, para mostrarlos en el edit). Endpoint dedicado:
   * no viene en el detalle (`getById`) ni se reenvía en el save.
   */
  getAdministradores(id: number): Observable<TenantAdminInfo[]> {
    return this.api.get<TenantAdminInfo[]>(`general/tenants/${id}/administradores`);
  }
}
