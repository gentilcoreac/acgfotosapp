import { LicenciaResumen } from './licencia-resumen.model';

/**
 * Cálculos derivados del resumen de licencias, centralizados en el dominio. Antes esta lógica
 * (sumar cupos, clasificar vigencia, contar por-vencer/vencidas) estaba copiada en el listado de
 * usuarios, el ABM y el homepage. Acá viven como funciones puras: cada UI las consume y mapea el
 * resultado a su propia presentación (chips/tonos), sin reimplementar la matemática.
 */

/** Estado de vigencia de una licencia. Único lugar que interpreta `isExpired`/`isExpiringSoon`. */
export type EstadoVigencia = 'vigente' | 'por-vencer' | 'vencida';

/** Clasifica la vigencia de una licencia (la API ya separó vencida de por-vencer). */
export function estadoVigencia(licencia: LicenciaResumen): EstadoVigencia {
  if (licencia.isExpired) {
    return 'vencida';
  }
  return licencia.isExpiringSoon ? 'por-vencer' : 'vigente';
}

/** Totales/conteos agregados de un conjunto de licencias del tenant. */
export interface LicenciasMetrics {
  /** Cupo total contratado (suma de cantidades). */
  total: number;
  /** Licencias asignadas a usuarios activos. */
  asignadas: number;
  /** Cupo libre. */
  disponibles: number;
  /** Tipos de licencia por vencer (dentro del umbral). */
  porVencer: number;
  /** Tipos de licencia ya vencidos. */
  vencidas: number;
}

/** Agrega el resumen del tenant en sus totales/conteos (una sola pasada). */
export function agregarLicencias(resumen: readonly LicenciaResumen[]): LicenciasMetrics {
  return resumen.reduce<LicenciasMetrics>(
    (acc, licencia) => ({
      total: acc.total + licencia.cantidadTotal,
      asignadas: acc.asignadas + licencia.cantidadAsignada,
      disponibles: acc.disponibles + licencia.cantidadDisponible,
      porVencer: acc.porVencer + (licencia.isExpiringSoon ? 1 : 0),
      vencidas: acc.vencidas + (licencia.isExpired ? 1 : 0),
    }),
    { total: 0, asignadas: 0, disponibles: 0, porVencer: 0, vencidas: 0 },
  );
}
