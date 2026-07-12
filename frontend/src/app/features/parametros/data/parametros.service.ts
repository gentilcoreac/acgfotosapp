import { Injectable } from '@angular/core';
import { Observable, map } from 'rxjs';
import { injectCrudClient } from '../../../core/http';
import { AplicacionOption, Parametro, PermisoOption } from '../domain/parametro.model';

/**
 * Servicio de datos de Parámetros. Expone el `CrudClient` (entidad `parametros` del módulo
 * `general`) que consumen la lista y el edit, más el lookup de `aplicaciones` para el select
 * de aplicación y el filtro del listado. La feature `aplicaciones` todavía no está migrada;
 * por ahora se resuelve como lookup acá (mismo enfoque que `tipos-licencias` con `roles`).
 *
 * TODO: Los permisos se obtienen **por aplicación** desde `aplicaciones/{id}` (que devuelve el
 * `AplicacionDto` con su colección `permisos`): el listado de `permisos` devuelve un header
 * recortado (`PermisoHeaderDto`) que ya **no expone `aplicacionId`**, por lo que filtrarlo
 * por aplicación —como hacía el front original— no es posible contra la API actual.
 */
@Injectable({ providedIn: 'root' })
export class ParametrosService {
  readonly crud = injectCrudClient<Parametro>('parametros');
  private readonly aplicacionesCrud = injectCrudClient<AplicacionOption>('aplicaciones');

  /** Todas las aplicaciones disponibles (select de aplicación y filtro del listado). */
  getAplicaciones(): Observable<AplicacionOption[]> {
    return this.aplicacionesCrud.getAll().pipe(map((result) => result.items));
  }

  /** Permisos de una aplicación (vía `aplicaciones/{id}`, que los trae poblados). */
  getPermisosDeAplicacion(aplicacionId: number): Observable<PermisoOption[]> {
    return this.aplicacionesCrud
      .getById(aplicacionId)
      .pipe(map((aplicacion) => aplicacion.permisos ?? []));
  }
}
