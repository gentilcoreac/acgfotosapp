/** Participante visible en la sesión (hoy siempre 1; el token ya viene listo para más de uno). */
export interface ParticipanteSesion {
  id: number;
  nombre: string;
}

/** Respuesta de `POST fotos/canje` (`CanjeResultDto`). */
export interface CanjeResult {
  /** Token de sesión de familia (JWT de corta duración, ADR-11). */
  token: string;
  /** Fecha/hora de expiración del token (ISO). */
  validTo: string;
  eventoId: number;
  nombreEvento: string;
  participantes: ParticipanteSesion[];
}
