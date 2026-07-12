import { DestroyRef, Injectable, computed, effect, inject, signal } from '@angular/core';
import { Subscription } from 'rxjs';
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
  private readonly destroyRef = inject(DestroyRef);

  readonly isRoot = this.store.isRoot;
  readonly isImpersonating = this.store.isImpersonating;

  /** Paths permitidos del usuario; `null` mientras carga. */
  private readonly allowedPaths = signal<ReadonlySet<string> | null>(null);
  /** `true` si no se pudieron determinar (error de red): se falla **abierto** como el guard de rutas. */
  private readonly allowedFailedOpen = signal(false);
  /** Capacidades ya resueltas (cargaron o fallaron-abierto): hasta entonces, los widgets esperan. */
  readonly ready = computed(() => this.allowedPaths() !== null || this.allowedFailedOpen());

  private sub?: Subscription;

  constructor() {
    // Recarga las capacidades al cambiar el contexto efectivo (tenant del token). Coincide con el
    // momento en que ImpersonationService limpia el cache de allowed-routes.
    effect(() => {
      this.store.tenantId();
      this.reloadAllowed();
    });
    this.destroyRef.onDestroy(() => this.sub?.unsubscribe());
  }

  private reloadAllowed(): void {
    this.sub?.unsubscribe();
    this.allowedPaths.set(null);
    this.allowedFailedOpen.set(false);
    this.sub = this.allowedRoutes.getAllowedPaths().subscribe((paths) => {
      if (paths === null) {
        this.allowedFailedOpen.set(true);
      } else {
        this.allowedPaths.set(paths);
      }
    });
  }

  /**
   * ¿El usuario puede acceder a la sección `path`? Root ve todo; ante error de red falla abierto
   * (coherente con `allowedRoutesGuard`). Reactivo: úsese dentro de `computed`/template.
   */
  canAccess(path: string): boolean {
    if (this.store.isRoot() || this.allowedFailedOpen()) {
      return true;
    }
    return this.allowedPaths()?.has(path) ?? false;
  }
}
