import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { ApiClient, injectCrudClient } from '../../../core/http';
import {
  AplicacionTenant,
  ParametroValorRow,
  ParametroValorTenantInput,
  TenantListItem,
} from '../domain/parametro-valor-tenant.model';

/**
 * Servicio de datos del override de parámetros por tenant.
 *
 * - `crud` (entidad `parametros-valores`): **upsert** del override (`save` → `POST .../update`,
 *   la API crea o actualiza según el id del body) y **reset** (`delete` → borra el override y el
 *   parámetro vuelve a su valor por defecto).
 * - `getParametros`: la grilla de parámetros con su valor efectivo para el tenant
 *   (`GET general/parametros/parametros-por-tenant-aplicacion`).
 * - Lookups del selector: tenants (`tenants`) y aplicaciones habilitadas del tenant
 *   (`aplicaciones/aplicaciones-tenant-id/{id}`, root-only en la API).
 */
@Injectable({ providedIn: 'root' })
export class ParametrosValorTenantService {
  private readonly api = inject(ApiClient);
  /** Override por tenant: `save` = upsert, `delete` = reset al valor por defecto. */
  readonly crud = injectCrudClient<ParametroValorTenantInput>('parametros-valores');
  private readonly tenantsCrud = injectCrudClient<TenantListItem>('tenants');

  /** Tenants disponibles para el selector (root lista todos). */
  getTenants(): Observable<TenantListItem[]> {
    return this.tenantsCrud
      .getAll()
      .pipe(map((result) => result.items.filter((t) => t.id != null)));
  }

  /** Aplicaciones habilitadas para el tenant elegido. */
  getAplicacionesPorTenant(tenantId: number): Observable<AplicacionTenant[]> {
    return this.api.get<AplicacionTenant[]>(
      `general/aplicaciones/aplicaciones-tenant-id/${tenantId}`,
    );
  }

  /** Parámetros de la aplicación con su valor efectivo (override del tenant o default). */
  getParametros(tenantId: number, aplicacionId: number): Observable<ParametroValorRow[]> {
    return this.api.get<ParametroValorRow[]>(
      'general/parametros/parametros-por-tenant-aplicacion',
      {
        tenantId,
        aplicacionId,
      },
    );
  }
}
