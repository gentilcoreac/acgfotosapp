import { Injectable, inject } from '@angular/core';
import { Observable, catchError, map, of, shareReplay, tap, throwError } from 'rxjs';
import { ApiClient } from '../http';
import { AllowedRoute } from './models/allowed-route.model';

/**
 * Rutas que el usuario puede navegar según sus permisos (menú). Cachea el resultado de
 * `GET api/general/menus/allowed-routes` y resuelve `isAllowed(url)` para `allowedRoutesGuard`.
 *
 * Es **solo UX de ruteo**: la autorización real la valida la API en cada endpoint. Por eso, ante un
 * error de red falla **abierto** (deja pasar) en vez de bloquear toda la navegación, igual que el
 * sidenav que cae al menú estático si el backend falla. Un resultado vacío (usuario sin menús) sí
 * deniega: solo el home queda accesible.
 */
@Injectable({ providedIn: 'root' })
export class AllowedRoutesService {
  private readonly api = inject(ApiClient);

  /** Cache del resultado exitoso; se puebla tras el primer fetch y se limpia en logout/impersonación. */
  private cached: AllowedRoute[] | null = null;
  /** Request en vuelo compartido: dedupe a `isAllowed` y `getAllowedPaths` cuando disparan juntos. */
  private inFlight$: Observable<AllowedRoute[]> | null = null;
  /** Generación del cache; se incrementa en cada `clear()` para invalidar taps de requests en vuelo. */
  private generation = 0;

  /** `true` si `url` corresponde a una ruta permitida (o a una ruta básica como el home). */
  isAllowed(url: string): Observable<boolean> {
    const path = normalizePath(url);
    if (isBasicRoute(path)) {
      return of(true);
    }
    return this.routes().pipe(
      map((routes) => routes.some((r) => r.routePath != null && isSubPath(path, r.routePath))),
      catchError(() => of(true)),
    );
  }

  /**
   * Conjunto de paths permitidos del usuario (para gateo reactivo de UI, p. ej. los widgets del
   * homepage). Comparte el mismo request/cache que `isAllowed`. Ante error de red devuelve `null`
   * (no se pudo determinar) para que el consumidor decida si falla abierto o cerrado.
   */
  getAllowedPaths(): Observable<ReadonlySet<string> | null> {
    return this.routes().pipe(
      map(
        (routes) =>
          new Set(
            routes.map((r) => r.routePath).filter((p): p is string => p != null),
          ) as ReadonlySet<string>,
      ),
      catchError(() => of(null)),
    );
  }

  /**
   * Rutas permitidas con una sola fuente de verdad: si ya están cacheadas las devuelve; si hay un
   * request en vuelo lo reusa (dedupe concurrente vía `shareReplay`); el error NO se cachea (se
   * limpia el in-flight para permitir reintento).
   */
  private routes(): Observable<AllowedRoute[]> {
    if (this.cached) {
      return of(this.cached);
    }
    const gen = this.generation;
    this.inFlight$ ??= this.api.get<AllowedRoute[]>('general/menus/allowed-routes').pipe(
      tap((routes) => {
        // Solo cachea si no hubo un clear() posterior (generación no cambió).
        if (this.generation === gen) {
          this.cached = routes;
        }
        this.inFlight$ = null;
      }),
      catchError((err) => {
        this.inFlight$ = null;
        return throwError(() => err);
      }),
      shareReplay(1),
    );
    return this.inFlight$;
  }

  /** Limpia el cache (logout/impersonación) para que el próximo usuario reciba sus propias rutas. */
  clear(): void {
    this.cached = null;
    this.inFlight$ = null;
    this.generation++;
  }
}

/** Rutas siempre accesibles para una sesión válida (no dependen de permisos de menú). */
function isBasicRoute(path: string): boolean {
  return (
    path === '' || path === '/' || path === '/perfil' || path === '/404' || path === '/sin-permisos'
  );
}

/**
 * Normaliza la URL antes de compararla contra `routePath`: saca el query string y cualquier
 * segmento numérico (id), no solo el final (p. ej. `/menus/12?x=1` → `/menus`,
 * `/tenants/5/databases` → `/tenants/databases`). Hoy los edits son diálogos (no rutas con `:id`
 * en el medio) salvo `tenants/:tenantId/databases`, que no tiene menú propio — ver `isSubPath`.
 */
function normalizePath(url: string): string {
  return url
    .split('?')[0]
    .split('/')
    .filter((segment) => segment.length === 0 || Number.isNaN(Number(segment)))
    .join('/');
}

/**
 * `path` está permitido por `routePath` si coincide exacto o cuelga de él (p. ej.
 * `/tenants/databases` bajo `/tenants`). Estas pantallas sin menú propio (alcanzables solo desde
 * un botón de su pantalla padre, como el ABM de bases del tenant) heredan el permiso del padre en
 * vez de necesitar una fila propia en `gen_Menus` — coherente con que la autorización real la
 * valida la API en cada endpoint, esto es solo UX de ruteo.
 */
function isSubPath(path: string, routePath: string): boolean {
  return path === routePath || path.startsWith(`${routePath}/`);
}
