/**
 * Respuesta del endpoint de login/refresh de la API (`TokenModelDto`).
 * El access token es de vida corta y se guarda solo en memoria (AuthStore).
 */
export interface AccessData {
  /** Access token (JWT). */
  token: string;
  /** Fecha/hora de expiración del access token (ISO). */
  validTo: string;
  /** Tenant del usuario autenticado. */
  tenantId: number;
  /** Indica si el email del usuario está verificado. */
  hasVerifiedEmail: boolean;
}
