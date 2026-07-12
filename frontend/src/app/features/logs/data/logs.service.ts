import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { ApiClient } from '../../../core/http';
import { QueryParams } from '../../../core/models/query-params.model';
import { QueryResult } from '../../../core/models/query-result.model';
import { TenantService } from '../../tenant/data/tenant.service';
import { LogInfo, TenantLookup } from '../domain/log.model';

/**
 * Servicio de datos del log de aplicación. Solo lectura, **root**: usa el endpoint cross-tenant
 * `logInfo/AllTenants` (la API lo gatea a root del tenant raíz). El detalle se muestra desde la
 * fila ya cargada (el endpoint trae el registro completo, incl. `exception`/`properties`).
 */
@Injectable({ providedIn: 'root' })
export class LogsService {
  private readonly api = inject(ApiClient);
  // Reusa el servicio de Tenants (mismo origen que el selector de impersonalización), no un crud propio.
  private readonly tenantService = inject(TenantService);

  /** Listado liviano (sin Exception/Properties); acepta filtros (searchText/level/fechaDesde/fechaHasta). */
  getAllTenants(query: QueryParams): Observable<QueryResult<LogInfo>> {
    return this.api.getQueryResult<LogInfo>('general/logInfo/AllTenants', query);
  }

  /** Detalle completo de un log por id (cross-tenant, root): trae mensaje/excepción/propiedades. */
  getByIdAllTenants(id: number): Observable<LogInfo> {
    return this.api.get<LogInfo>(`general/logInfo/${id}/all-tenants`);
  }

  /** Tenants (id + nombre) para el filtro y para mostrar el nombre en la columna Tenant. */
  getTenants(): Observable<TenantLookup[]> {
    return this.tenantService.crud
      .getAll()
      .pipe(map((r) => r.items.map((t) => ({ id: t.id ?? 0, nombre: t.nombre }))));
  }
}
