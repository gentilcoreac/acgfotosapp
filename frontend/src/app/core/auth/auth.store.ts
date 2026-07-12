import { Injectable, Signal, computed, signal } from '@angular/core';

/**
 * Causa por la que falló la restauración de sesión y se entró en reconexión. Determina el mensaje
 * de la pantalla de reconexión: el 429 NO es lo mismo que la API caída, decirlo mal confunde.
 * - `rate-limit`: 429 (demasiadas solicitudes en poco tiempo).
 * - `network`: status 0 (la API no responde / sin conexión).
 * - `server`: 5xx (la API respondió con un error interno).
 */
export type ReconnectReason = 'rate-limit' | 'network' | 'server';

/** Decodifica el payload (claims) de un JWT. Devuelve `null` si el token es inválido. */
function decodeJwtPayload(token: string): Record<string, unknown> | null {
  const part = token.split('.')[1];
  if (!part) {
    return null;
  }
  try {
    const base64 = part.replace(/-/g, '+').replace(/_/g, '/');
    const padded = base64.padEnd(base64.length + ((4 - (base64.length % 4)) % 4), '=');
    return JSON.parse(atob(padded)) as Record<string, unknown>;
  } catch {
    return null;
  }
}

/**
 * Estado de sesión basado en signals.
 *
 * El access token vive **solo en memoria** (no en localStorage ni cookie
 * JS-readable, a diferencia del original que lo guardaba en cookie ofuscada con
 * `btoa`). Al refrescar la página (F5 / pestaña nueva) el token en memoria se
 * pierde, pero la sesión se restaura de forma silenciosa con el refresh token
 * (cookie HttpOnly) en `provideAuthSessionRestore` → sin re-login mientras la
 * cookie de refresh siga vigente.
 *
 * No guarda perfil ni permisos (eso va en un store aparte): solo sesión/token.
 */
@Injectable({ providedIn: 'root' })
export class AuthStore {
  private readonly _accessToken = signal<string | null>(null);
  /** Expiración del access token en epoch ms (fuente: `validTo` de la API). */
  private readonly _expiresAt = signal<number | null>(null);

  /**
   * Hay una sesión que NO se pudo restaurar por un fallo transitorio (rate limit / blip de API), no
   * por estar deslogueado. Lo setea la restauración de sesión (F5) ante 429/red/5xx; el `authGuard`
   * lo usa para mandar a la pantalla de reconexión en vez de a login. La `ReconnectingComponent` lo
   * limpia al reconectar o al rendirse.
   */
  private readonly _reconnecting = signal(false);
  readonly reconnecting = this._reconnecting.asReadonly();

  /**
   * Causa del estado de reconexión (rate-limit / network / server). La `ReconnectingComponent` la
   * usa para mostrar el mensaje correcto. Null cuando no hay reconexión en curso.
   */
  private readonly _reconnectReason = signal<ReconnectReason | null>(null);
  readonly reconnectReason = this._reconnectReason.asReadonly();

  /** Access token actual (o null si no hay sesión). */
  readonly accessToken: Signal<string | null> = this._accessToken.asReadonly();

  /** Claims del access token actual, decodificados una sola vez (o null si no hay token válido). */
  private readonly claims = computed<Record<string, unknown> | null>(() => {
    const token = this._accessToken();
    return token ? decodeJwtPayload(token) : null;
  });

  /**
   * Sesión válida del lado cliente (token presente y no expirado). Es para UX
   * de ruteo — la autorización real siempre la valida la API en cada endpoint.
   */
  readonly isAuthenticated = computed<boolean>(() => {
    const token = this._accessToken();
    const expiresAt = this._expiresAt();
    return token !== null && expiresAt !== null && Date.now() < expiresAt;
  });

  /**
   * Indica si el usuario es **root** (administrador del sistema), leído del claim `isRoot`
   * del JWT (la API lo emite comparando `tenantId == rootTenantId`).
   *
   * Es **solo para UX** (mostrar/ocultar acciones reservadas a root). La autorización real la
   * valida la API en cada endpoint (ej. `roles/{id}/set-default-tenant` chequea `IsRoot`).
   */
  readonly isRoot = computed<boolean>(
    () => String(this.claims()?.['isRoot'] ?? '').toLowerCase() === 'true',
  );

  /**
   * Nombre de usuario **efectivo** del token (claim `sub`): mientras se impersona es el usuario
   * destino. Cambia al loguearse / impersonar / parar, pero NO en el refresh silencioso (mismo
   * usuario) — útil como disparador para recargar menú/rutas solo cuando cambia el contexto.
   */
  readonly currentUserName = computed<string | null>(
    () => (this.claims()?.['sub'] as string | undefined) ?? null,
  );

  /**
   * Indica si la sesión actual es de **impersonalización** (claim `impersonatedBy` presente, ADR-0002).
   * Es solo para UX (banner + botón salir); el server valida todo por el token firmado.
   */
  readonly isImpersonating = computed<boolean>(() => this.claims()?.['impersonatedBy'] != null);

  /**
   * Tenant **efectivo** del token (claim `tenant` = tenantId): cambia al loguearse / impersonar /
   * parar. Lo usa el theming para aplicar el estilo del tenant del usuario activo. Null si no hay token.
   */
  readonly tenantId = computed<number | null>(() => {
    const raw = this.claims()?.['tenant'];
    const id = Number(raw);
    return raw != null && Number.isFinite(id) ? id : null;
  });

  /** Establece la sesión a partir de la respuesta de login/refresh. */
  setSession(token: string, validTo: string | Date): void {
    this._accessToken.set(token);
    this._expiresAt.set(new Date(validTo).getTime());
    this._reconnecting.set(false);
    this._reconnectReason.set(null);
  }

  /**
   * Marca/desmarca el estado de reconexión (restauración de sesión fallida por causa transitoria).
   * `reason` indica la causa (para el mensaje); al desmarcar se limpia.
   */
  setReconnecting(value: boolean, reason: ReconnectReason | null = null): void {
    this._reconnecting.set(value);
    this._reconnectReason.set(value ? reason : null);
  }

  /** Limpia la sesión (logout / expiración). */
  clearSession(): void {
    this._accessToken.set(null);
    this._expiresAt.set(null);
    this._reconnecting.set(false);
    this._reconnectReason.set(null);
  }
}
