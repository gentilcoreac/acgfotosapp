import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiClient } from '../../core/http';
import { MenuNode } from './menu-item.model';

/**
 * Datos del menú lateral. Consume `GET api/general/menus/principal`, que devuelve el árbol de menús
 * del usuario logueado **filtrado por sus permisos** (root recibe los menús de `PermisoRoot`).
 */
@Injectable({ providedIn: 'root' })
export class MenuService {
  private readonly api = inject(ApiClient);

  getPrincipal(): Observable<MenuNode[]> {
    return this.api.get<MenuNode[]>('general/menus/principal');
  }
}
