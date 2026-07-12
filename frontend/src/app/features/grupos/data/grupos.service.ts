import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { ApiClient, injectCrudClient } from '../../../core/http';
import { TbiSearchSelectItem } from '../../../shared/ui/tbi-search-select/tbi-search-select.component';
import { Grupo, RolOption } from '../domain/grupo.model';

/** Usuario (campos mínimos) para el selector de miembros (listado `api/general/usuarios`). */
interface UsuarioLite {
  id: number;
  userName: string;
  nombre: string;
  apellido: string;
  /** Licencia activa (la API la trae en el header); se usa para avisar mezcla de licencias (§11.2). */
  tipoLicenciaActiva?: { tipoLicenciaId: number } | null;
}

/** Item del buscador de miembros + la licencia activa del usuario (para detectar mezcla, §11.2). */
export interface UsuarioBusqueda extends TbiSearchSelectItem {
  tipoLicenciaActivaId: number | null;
}

/**
 * Servicio de datos de Grupos. Expone el `CrudClient` (entidad `grupos`) para la lista/edit, más la
 * búsqueda server-side de usuarios (typeahead) que alimenta el selector de miembros del ABM.
 *
 * `searchUsuarios` usa un `CrudClient` propio contra `api/general/usuarios` (no acopla con la
 * feature `usuarios`) y trae sólo una página de coincidencias por término — así escala aunque el
 * tenant tenga muchos usuarios (no carga todo el padrón como hacía el selector estático).
 */
@Injectable({ providedIn: 'root' })
export class GruposService {
  readonly crud = injectCrudClient<Grupo>('grupos');
  private readonly usuariosCrud = injectCrudClient<UsuarioLite>('usuarios');
  private readonly api = inject(ApiClient);

  /**
   * Roles que el grupo puede otorgar: los **licenciados por el tenant** (`roles/del-tenant` =
   * TenantLicencias → TipoLicencia → roles). NO el catálogo completo: así se excluyen los roles
   * huérfanos (sin licencia) y los de licencias que el tenant no tiene. Se suman a los roles
   * propios de cada miembro (roles efectivos).
   */
  getRoles(): Observable<RolOption[]> {
    return this.api.get<RolOption[]>('general/roles/del-tenant');
  }

  /**
   * Busca usuarios por término (nombre/apellido/usuario/email) para el `tbi-search-select`. Devuelve
   * también la licencia activa de cada uno (`tipoLicenciaActivaId`) para que el ABM detecte mezcla de
   * licencias (§11.2). El tipo extiende `TbiSearchSelectItem`, así el selector lo consume igual.
   */
  searchUsuarios(term: string): Observable<UsuarioBusqueda[]> {
    return this.usuariosCrud.getAllByCriteria({ page: 0, pageSize: 20, searchText: term }).pipe(
      map((result) =>
        result.items.map((u) => ({
          id: u.id,
          label: `${u.nombre} ${u.apellido} (${u.userName})`,
          tipoLicenciaActivaId: u.tipoLicenciaActiva?.tipoLicenciaId ?? null,
        })),
      ),
    );
  }
}
