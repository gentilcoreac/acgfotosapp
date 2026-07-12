/**
 * Resumen de un tipo de licencia del tenant: cuántas hay, cuántas asignadas/disponibles y su
 * estado de vencimiento. Es la proyección de `GetCantidadLicenciasDisponiblesOutput` (endpoint
 * `general/usuarios/get-cantidad-licencias-asignadas`).
 *
 * **Una sola consulta, varios consumidores:** alimenta a la vez la columna de licencia del
 * listado de usuarios (resolviendo `tipoLicenciaId → descripcion`), el banner de "por vencer",
 * el indicador de licencias disponibles y (a futuro) los KPIs del homepage.
 */
export interface LicenciaResumen {
  tipoLicenciaId: number;
  descripcion: string;
  /** Licencias contratadas de este tipo (cupo del tenant). */
  cantidadTotal: number;
  /** Cupo libre (`cantidadTotal - cantidadAsignada`). */
  cantidadDisponible: number;
  /** Licencias de este tipo ya asignadas a usuarios activos. */
  cantidadAsignada: number;
  /** La licencia ya venció (la API la considera no válida para login). */
  isExpired: boolean;
  /** Vence dentro del umbral configurado en la API (`ExpiringSoonThresholdDays`, default 30 días). */
  isExpiringSoon: boolean;
  /** Fecha de expiración (ISO). `null` si el tipo no tiene vencimiento informado. */
  expirationDate: string | null;
}
