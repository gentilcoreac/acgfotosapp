import { Injectable } from '@angular/core';
import { Observable, map } from 'rxjs';
import { CrudClient, injectCrudClient } from '../../../core/http';

/**
 * Métricas de conteo para los KPIs del homepage. Reutiliza los endpoints de listado existentes:
 * pide una página mínima y se queda con el `totalCount` (no trae las filas). Cada conteo respeta el
 * scope multi-tenant de la API (root ve el tenant de sistema; impersonando, el del cliente).
 */
@Injectable({ providedIn: 'root' })
export class HomeMetricsService {
  private readonly usuarios = injectCrudClient<unknown>('usuarios');
  private readonly tenants = injectCrudClient<unknown>('tenants');
  private readonly grupos = injectCrudClient<unknown>('grupos');

  countUsuarios(): Observable<number> {
    return this.count(this.usuarios);
  }

  countTenants(): Observable<number> {
    return this.count(this.tenants);
  }

  countGrupos(): Observable<number> {
    return this.count(this.grupos);
  }

  /** Total de la entidad: pide la primera página de tamaño 1 y devuelve sólo `totalCount`. */
  private count(crud: CrudClient<unknown>): Observable<number> {
    return crud.getAllByCriteria({ page: 0, pageSize: 1 }).pipe(map((result) => result.totalCount));
  }
}
