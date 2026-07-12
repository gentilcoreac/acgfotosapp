import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, catchError, map, of, tap } from 'rxjs';
import { AuthStore } from '../auth/auth.store';
import { ApiClient } from '../http';
import { AplicacionPermitida } from './aplicacion-context.model';

const LS_KEY = 'tbi-aplicacion-id';

/** Shape de `aplicaciones-tenant` (AplicacionDto): apps que el tenant tiene habilitadas. */
interface AplicacionTenantDto {
  id: number;
  nombre: string;
}

/**
 * Aplicación activa del usuario. Persiste la selección en localStorage para sobrevivir el refresh;
 * siempre se valida contra la lista real del backend al cargar.
 *
 * Fuente de las apps (igual que el cliente original):
 * - **No-root**: sus `UsuarioAplicacion` (`aplicaciones-permitidas`), con flag `default`.
 * - **Root**: las apps del tenant activo (`aplicaciones-tenant`); como no hay flag de default a
 *   nivel tenant, se toma la primera. Esto le da a root un `aplicacionId` concreto (antes el front
 *   mandaba `1` hardcodeado); sin él, los listados/permisos filtrados por aplicación quedan vacíos.
 */
@Injectable({ providedIn: 'root' })
export class AplicacionContextStore {
  private readonly api = inject(ApiClient);
  private readonly auth = inject(AuthStore);

  private readonly _aplicaciones = signal<AplicacionPermitida[]>([]);
  private readonly _selectedId = signal<number | null>(readFromStorage());
  /**
   * Contador que se incrementa cada vez que `load()` termina de resolver el contexto. Sirve como
   * disparador único para recargar lo que depende de (usuario + aplicación) — p. ej. el menú — sin
   * caer en doble carga: el cambio de usuario (token) y el de aplicación no son simultáneos, así que
   * watchear ambos por separado dispara dos veces (la primera con la app vieja). Esta señal cambia
   * **una sola vez**, recién cuando el contexto quedó consistente.
   */
  private readonly _version = signal(0);

  readonly aplicaciones = this._aplicaciones.asReadonly();
  readonly selectedId = this._selectedId.asReadonly();
  /** Cambia una vez por resolución completa de contexto (ver `_version`). */
  readonly version = this._version.asReadonly();
  /** Hay más de una aplicación → mostrar el selector en el sidenav. */
  readonly isMultiApp = computed(() => this._aplicaciones().length > 1);

  /**
   * Carga las aplicaciones del usuario efectivo y resuelve la selección. Devuelve un `Observable`
   * (nunca falla, ver `catchError`) para que login/impersonalización **esperen** a que `selectedId`
   * esté resuelto antes de navegar — si navegaran antes, el primer request saldría sin
   * `aplicacionId` y la API filtraría sus listados/permisos a vacío.
   */
  load(): Observable<AplicacionPermitida[]> {
    return this.fetch().pipe(
      catchError(() => of([])),
      tap((apps) => {
        this._aplicaciones.set(apps);
        this.resolveSelected(apps);
        // Bump al final: marca "contexto resuelto" para los consumidores (menú) en una sola emisión.
        this._version.update((v) => v + 1);
      }),
    );
  }

  /** Cambia la aplicación activa y persiste en localStorage. */
  select(id: number): void {
    this._selectedId.set(id);
    localStorage.setItem(LS_KEY, String(id));
  }

  /** Limpia el estado (logout). */
  clear(): void {
    this._aplicaciones.set([]);
    this._selectedId.set(null);
    localStorage.removeItem(LS_KEY);
  }

  /** Root → apps del tenant; no-root → apps del usuario. Ambas se normalizan a `AplicacionPermitida`. */
  private fetch(): Observable<AplicacionPermitida[]> {
    if (this.auth.isRoot()) {
      return this.api.get<AplicacionTenantDto[]>('general/aplicaciones/aplicaciones-tenant').pipe(
        map((apps) =>
          apps.map((a, idx) => ({
            aplicacionId: a.id,
            aplicacionNombre: a.nombre,
            default: idx === 0,
          })),
        ),
      );
    }
    return this.api.get<AplicacionPermitida[]>('general/aplicaciones/aplicaciones-permitidas');
  }

  private resolveSelected(apps: AplicacionPermitida[]): void {
    if (!apps.length) {
      this._selectedId.set(null);
      localStorage.removeItem(LS_KEY);
      return;
    }
    const stored = readFromStorage();
    if (stored != null && apps.some((a) => a.aplicacionId === stored)) {
      this._selectedId.set(stored);
      return;
    }
    const fallback = apps.find((a) => a.default) ?? apps[0];
    this.select(fallback.aplicacionId);
  }
}

function readFromStorage(): number | null {
  const raw = localStorage.getItem(LS_KEY);
  const id = Number(raw);
  return raw != null && Number.isFinite(id) && id > 0 ? id : null;
}
