import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { ApiClient, injectCrudClient } from '../../../core/http';
import {
  AplicacionOption,
  Menu,
  MenuDash,
  MenuOption,
  PermisoDeAplicacion,
} from '../domain/menu.model';

/** Shape de `AplicacionDto` que consumimos: básicos + su colección de permisos. */
interface AplicacionConPermisos {
  id: number;
  nombre: string;
  permisos?: PermisoDeAplicacion[];
}

/** pageSize grande para traer un listado completo (sin paginar) de menús de una app. */
const ALL_PAGE_SIZE = 19999;

/**
 * Servicio de datos de Menús. Expone el `CrudClient` (entidad `menus`) para lista y edit, más los
 * lookups del ABM: aplicaciones (select/filtro), permisos de una aplicación (select de permiso) y
 * menús de una aplicación (select de "menú padre", filtrado por la app elegida).
 */
@Injectable({ providedIn: 'root' })
export class MenusService {
  readonly crud = injectCrudClient<Menu>('menus');
  private readonly aplicacionesCrud = injectCrudClient<AplicacionConPermisos>('aplicaciones');
  private readonly api = inject(ApiClient);

  /**
   * Accesos directos del home: menús marcados `VisibleDash` que el usuario puede ver (la API ya los
   * filtra por sus permisos). Ordenados por `orden` en el backend.
   */
  getAccesosDirectos(): Observable<MenuDash[]> {
    return this.api.get<MenuDash[]>('general/menus/dashboard');
  }

  /** Aplicaciones disponibles (select de aplicación y filtro de la lista). */
  getAplicaciones(): Observable<AplicacionOption[]> {
    return this.aplicacionesCrud
      .getAll()
      .pipe(map((result) => result.items.map((a) => ({ id: a.id, nombre: a.nombre }))));
  }

  /**
   * Permisos de una aplicación (vía `aplicaciones/{id}`, que devuelve el `AplicacionDto` con su
   * colección `permisos`). Mismo enfoque que `parametro`/`permiso`: el permiso depende de la
   * aplicación elegida, sin reintroducir filtros extra en el endpoint de permisos.
   */
  getPermisosDeAplicacion(aplicacionId: number): Observable<PermisoDeAplicacion[]> {
    return this.aplicacionesCrud
      .getById(aplicacionId)
      .pipe(map((a) => (a.permisos ?? []).map((p) => ({ id: p.id, nombre: p.nombre }))));
  }

  /**
   * Menús de una aplicación, para el select de "menú padre". Reusa el listado del ABM filtrando por
   * `aplicacionId` (la API lo soporta en el criteria). El propio menú en edición se excluye en el
   * componente (no puede ser su propio padre).
   */
  getMenusDeAplicacion(aplicacionId: number): Observable<MenuOption[]> {
    return this.crud
      .getAllByCriteria({ aplicacionId, page: 0, pageSize: ALL_PAGE_SIZE })
      .pipe(
        map((result) =>
          result.items.map((m) => ({ id: m.id as number, nombre: m.nombre, codigo: m.codigo })),
        ),
      );
  }
}
