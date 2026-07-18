/**
 * Foto de la galería de familia (`FotoDto` de `GET fotos/familia/fotos`, ADR-11). Siempre está
 * Lista — el endpoint filtra Pendiente/Error del lado del servidor — así que no viaja el estado.
 */
export interface FotoFamilia {
  id: number;
  grupoId: number;
  /** Null ⇒ foto grupal, visible para todos los participantes del grupo. */
  participanteId: number | null;
  ancho: number;
  alto: number;
}
