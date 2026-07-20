import { Injectable, computed, inject } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { AllowedRoutesService, AuthStore } from '../../../core/auth';

/**
 * Contexto del dashboard: expone las **capacidades** del usuario para que cada widget decida solo si
 * se muestra, sin if-ladders por rol en el home. Escala a N tipos de usuario: un tipo nuevo no toca
 * el home ni los widgets — hereda los indicadores de sus permisos.
 *
 * Hoy la capacidad es a nivel **sección/ruta** (`canAccess`), reusando las allowed-routes (lo mismo
 * que arma el menú). Para diferencias más finas entre roles que comparten una ruta (p. ej. admin vs
 * usuario común con distinto nivel sobre `/usuarios`), el seam es agregar `can(permisoCodigo)` sobre
 * una señal de permisos del usuario y que el widget (o cada tarjeta) lo consulte — sin tocar el home.
 *
 * Se provee a nivel del HomeComponent (no root): vive mientras el dashboard está montado y recarga
 * las capacidades cuando cambia el contexto efectivo del token (login / impersonar / parar).
 */
@Injectable()
export class DashboardContextService {
  private readonly store = inject(AuthStore);
  private readonly allowedRoutes = inject(AllowedRoutesService);

  readonly isRoot = this.store.isRoot;
  readonly isImpersonating = this.store.isImpersonating;

  /** Refetchea sola al cambiar el contexto efectivo (tenant del token) — coincide con el momento en
   * que `ImpersonationService` limpia el cache de allowed-routes. `getAllowedPaths()` ya resuelve
   * `null` internamente ante un error de red (nunca propaga error), así que el resource nunca entra
   * en estado error acá. */
  private readonly allowedResource = rxResource({
    params: () => this.store.tenantId(),
    stream: () => this.allowedRoutes.getAllowedPaths(),
  });
  /** Capacidades ya resueltas (cargaron o fallaron-abierto): hasta entonces, los widgets esperan. */
  readonly ready = computed(() => this.allowedResource.value() !== undefined);

  /**
   * ¿El usuario puede acceder a la sección `path`? Root ve todo; ante error de red falla abierto
   * (coherente con `allowedRoutesGuard`). Reactivo: úsese dentro de `computed`/template.
   */
  canAccess(path: string): boolean {
    if (this.store.isRoot()) {
      return true;
    }
    const paths = this.allowedResource.value();
    if (paths === undefined) {
      return false; // todavía cargando
    }
    return paths === null ? true : paths.has(path); // null = falló, fail-open
  }
}
