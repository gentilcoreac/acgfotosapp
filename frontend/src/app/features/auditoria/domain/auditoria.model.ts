/**
 * Registro de auditoría (`AuditoriaDto`). Una fila por request auditado: quién, qué servicio/método,
 * cuándo, desde dónde y con qué resultado. `parametros`/`resultContent` son pesados → la API los
 * incluye sólo a pedido (ver detalle); en el listado pueden venir vacíos.
 */
export interface Auditoria {
  id: number;
  fechaHora: string;
  duracion: number;
  servicio: string;
  metodo: string;
  parametros?: string | null;
  usuarioId?: number | null;
  /** userId real de root si la acción se hizo impersonando (ADR-0002); null si no. */
  impersonatedBy?: number | null;
  cuentaId?: number | null;
  httpMethod: string;
  requestAbsolutePath: string;
  clientIP: string;
  clientUserAgent: string;
  resultStatusCode: string;
  resultContent?: string | null;
  usuarioNombre?: string | null;
  cuentaNombre?: string | null;
}

/** Filtros del listado (se mapean al `AuditoriaCriteria` de la API como query params). */
export interface AuditoriaFiltros {
  fechaDesde: string;
  fechaHasta: string;
  servicio: string;
  resultStatusCode: string;
}
