import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { ApiClient, injectCrudClient } from '../../../core/http';
import {
  AplicacionOption,
  RolEfectivo,
  RolOption,
  TipoLicenciaConRoles,
  TipoLicenciaOption,
  Usuario,
  UsuarioActividadMes,
  ACTIVIDAD_MENSUAL_MESES,
} from '../domain/usuario.model';

/**
 * Servicio de datos de Usuarios. Expone el `CrudClient` (entidad `usuarios` del módulo
 * `general`) para la lista/edit, los lookups que pueblan el ABM (tipos de licencia, roles por
 * licencia, aplicaciones del tenant) y los endpoints dedicados que la API mantiene separados del
 * update (`set-administrador`, `bloquear-desbloquear`, `re-confirmar-cuenta`).
 */
@Injectable({ providedIn: 'root' })
export class UsuariosService {
  readonly crud = injectCrudClient<Usuario>('usuarios');
  private readonly tiposLicenciaCrud = injectCrudClient<TipoLicenciaConRoles>('tipos-licencia');
  private readonly api = inject(ApiClient);

  /** Tipos de licencia disponibles para asignar (la licencia define qué roles se pueden dar). */
  getTiposLicencia(): Observable<TipoLicenciaOption[]> {
    return this.tiposLicenciaCrud
      .getAll()
      .pipe(map((result) => result.items.map((t) => ({ id: t.id, descripcion: t.descripcion }))));
  }

  /** Roles que habilita una licencia (su colección `tipoLicenciaRoles`). */
  getRolesDeLicencia(tipoLicenciaId: number): Observable<RolOption[]> {
    return this.tiposLicenciaCrud.getById(tipoLicenciaId).pipe(
      map((tl) =>
        (tl.tipoLicenciaRoles ?? []).map((r) => ({
          id: r.rolId,
          descripcion: r.rolDescripcion ?? `Rol ${r.rolId}`,
        })),
      ),
    );
  }

  /**
   * Aplicaciones que el usuario puede tener asignadas: las **contratadas por su tenant**
   * (`gen_TenantAplicaciones`), no el catálogo global. Es el comportamiento multi-tenant correcto
   * (igual que el original con `getAplicacionesPorTenant`). Si viene vacío, el tenant no tiene
   * aplicaciones vinculadas — es un tema de datos, no del ABM. (Parámetros usa el listado global
   * `general/aplicaciones` porque los parámetros son a nivel sistema/aplicación, no por tenant.)
   */
  getAplicaciones(): Observable<AplicacionOption[]> {
    return this.api.get<AplicacionOption[]>('general/aplicaciones/aplicaciones-tenant');
  }

  /**
   * Cambia el flag `administrador` vía el endpoint dedicado (`UsuarioInputDto` no lo acepta).
   * La API lo restringe a root/admin; el cliente sólo lo muestra a root como UX.
   */
  setAdministrador(id: number, administrador: boolean): Observable<void> {
    return this.api.post<void>(`general/usuarios/${id}/set-administrador`, { administrador });
  }

  /** Bloquea/desbloquea al usuario (endpoint dedicado: hace más que togglear LockoutEnabled). */
  bloquearDesbloquear(userName: string, state: boolean): Observable<boolean> {
    return this.api.post<boolean>('general/usuarios/bloquear-desbloquear', { userName, state });
  }

  /** Reenvía el mail de confirmación de cuenta. */
  reConfirmarCuenta(id: number): Observable<boolean> {
    return this.api.post<boolean>('general/usuarios/re-confirmar-cuenta', id);
  }

  /**
   * Roles **efectivos** del usuario (directos + heredados de sus grupos) con su origen, para
   * mostrarlos read-only en el ABM. La unión la resuelve la API (vista `vw_UsuarioRolesEfectivos`).
   */
  getRolesEfectivos(id: number): Observable<RolEfectivo[]> {
    return this.api.get<RolEfectivo[]>(`general/usuarios/${id}/roles-efectivos`);
  }

  /**
   * Serie mensual de actividad del tenant (activos + altas por mes) para el gráfico del homepage.
   * `meses` = ventana hacia atrás (default 12). La API rellena los meses sin datos con cero.
   */
  getActividadMensual(meses = ACTIVIDAD_MENSUAL_MESES): Observable<UsuarioActividadMes[]> {
    return this.api.get<UsuarioActividadMes[]>('general/usuarios/actividad-mensual', { meses });
  }
}
