import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { ApiClient, injectCrudClient } from '../../../core/http';
import {
  ApiHierarchicalItem,
  AplicacionOption,
  Permiso,
  PermisoDeAplicacion,
} from '../domain/permiso.model';

/** Shape de `AplicacionDto` que consumimos: básicos + su colección de permisos (con padre). */
interface AplicacionConPermisos {
  id: number;
  nombre: string;
  permisos?: PermisoDeAplicacion[];
}

/**
 * Servicio de datos de Permisos. Expone el `CrudClient` (entidad `permisos`) para lista y edit,
 * más los lookups del ABM: aplicaciones (select), permisos de una aplicación (árbol de padre,
 * vía `aplaciones/{id}` que los trae con `permisoPadreId`), el árbol de endpoints
 * (`endpoints/hierarchical-items`) y el endpoint dedicado de `esRestringido`.
 */
@Injectable({ providedIn: 'root' })
export class PermisosService {
  readonly crud = injectCrudClient<Permiso>('permisos');
  private readonly aplicacionesCrud = injectCrudClient<AplicacionConPermisos>('aplicaciones');
  private readonly api = inject(ApiClient);

  /** Aplicaciones disponibles (select de aplicación). */
  getAplicaciones(): Observable<AplicacionOption[]> {
    return this.aplicacionesCrud
      .getAll()
      .pipe(map((result) => result.items.map((a) => ({ id: a.id, nombre: a.nombre }))));
  }

  /**
   * Permisos de una aplicación (vía `aplicaciones/{id}`, que devuelve el `AplicacionDto` con su
   * colección `permisos` poblada e incluyendo `permisoPadreId`). Sirve para armar el árbol de
   * "permiso padre" filtrado por la aplicación elegida, sin reintroducir `aplicacionId` en el
   * header de permisos (mismo enfoque que `parametros`).
   */
  getPermisosDeAplicacion(aplicacionId: number): Observable<PermisoDeAplicacion[]> {
    return this.aplicacionesCrud.getById(aplicacionId).pipe(map((a) => a.permisos ?? []));
  }

  /** Árbol Módulo→Controller→Endpoint (la API lo devuelve ya anidado). */
  getEndpointsTree(): Observable<ApiHierarchicalItem[]> {
    return this.api.get<ApiHierarchicalItem[]>('general/endpoints/hierarchical-items');
  }

  /**
   * Cambia el flag `esRestringido` de un permiso vía el endpoint dedicado (`PermisoInputDto` no lo
   * acepta). La API lo restringe a **root**; el cliente sólo muestra la acción a root (UX).
   */
  setEsRestringido(id: number, esRestringido: boolean): Observable<void> {
    return this.api.post<void>(`general/permisos/${id}/set-es-restringido`, { esRestringido });
  }
}
