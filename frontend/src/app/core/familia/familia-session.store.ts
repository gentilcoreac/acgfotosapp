import { Injectable, Signal, computed, signal } from '@angular/core';
import { CanjeResult, ParticipanteSesion } from './models/canje-result.model';

/** Clave de `sessionStorage` (NO localStorage: la sesión de 30 min no debe sobrevivir a cerrar la
 * pestaña en un dispositivo compartido — ver ADR-11). */
const STORAGE_KEY = 'acgfotos.familia.session';

interface StoredSession {
  token: string;
  validTo: string;
  eventoId: number;
  nombreEvento: string;
  participantes: ParticipanteSesion[];
}

function readStorage(): StoredSession | null {
  try {
    const raw = sessionStorage.getItem(STORAGE_KEY);
    return raw ? (JSON.parse(raw) as StoredSession) : null;
  } catch {
    return null;
  }
}

/**
 * Sesión de familia (canje de código, ADR-02/ADR-11): a diferencia de `AuthStore` (memoria pura,
 * sesión de plataforma con refresh), acá no hay refresh — al vencer el token de 30 minutos la
 * familia vuelve a canjear su código. Persiste en `sessionStorage` para sobrevivir a un F5 sin
 * pedir el código de nuevo, pero se pierde al cerrar la pestaña (a propósito).
 *
 * Los datos de display (nombre del evento, participantes) vienen directo de la respuesta del canje
 * y se guardan junto con el token — evita un round-trip a la API solo para mostrar "Hola, familia
 * de Ana" después de un refresh.
 */
@Injectable({ providedIn: 'root' })
export class FamiliaSessionStore {
  private readonly _token = signal<string | null>(null);
  private readonly _expiresAt = signal<number | null>(null);
  private readonly _eventoId = signal<number | null>(null);
  private readonly _nombreEvento = signal<string | null>(null);
  private readonly _participantes = signal<ParticipanteSesion[]>([]);

  readonly token: Signal<string | null> = this._token.asReadonly();
  readonly eventoId: Signal<number | null> = this._eventoId.asReadonly();
  readonly nombreEvento: Signal<string | null> = this._nombreEvento.asReadonly();
  readonly participantes: Signal<ParticipanteSesion[]> = this._participantes.asReadonly();

  /** Sesión con token presente y no vencido. */
  readonly isActive = computed<boolean>(() => {
    const token = this._token();
    const expiresAt = this._expiresAt();
    return token !== null && expiresAt !== null && Date.now() < expiresAt;
  });

  /** "Familia de {nombres}" — texto de la segunda marca de agua (docs/05-notas-abiertas.md);
   * centralizado acá para que `tbi-foto-familia-img` y el preview ampliado usen exactamente el mismo
   * texto sin duplicar el `join`. `null` si por algún motivo no hay sesión activa. */
  readonly nombreFamilia = computed<string | null>(() => {
    const nombres = this._participantes()
      .map((p) => p.nombre)
      .join(' y ');
    return nombres ? `Familia de ${nombres}` : null;
  });

  constructor() {
    const stored = readStorage();
    if (stored && Date.now() < new Date(stored.validTo).getTime()) {
      this.hydrate(stored);
    } else if (stored) {
      sessionStorage.removeItem(STORAGE_KEY); // sesión vieja vencida: no dejarla pisada
    }
  }

  /** Guarda la sesión resultante de un canje exitoso (persiste en sessionStorage). */
  setSession(result: CanjeResult): void {
    this.hydrate(result);
    sessionStorage.setItem(
      STORAGE_KEY,
      JSON.stringify({
        token: result.token,
        validTo: result.validTo,
        eventoId: result.eventoId,
        nombreEvento: result.nombreEvento,
        participantes: result.participantes,
      } satisfies StoredSession),
    );
  }

  /** Limpia la sesión (vencimiento detectado / la familia sale). */
  clearSession(): void {
    this._token.set(null);
    this._expiresAt.set(null);
    this._eventoId.set(null);
    this._nombreEvento.set(null);
    this._participantes.set([]);
    sessionStorage.removeItem(STORAGE_KEY);
  }

  private hydrate(data: StoredSession): void {
    this._token.set(data.token);
    this._expiresAt.set(new Date(data.validTo).getTime());
    this._eventoId.set(data.eventoId);
    this._nombreEvento.set(data.nombreEvento);
    this._participantes.set(data.participantes);
  }
}
