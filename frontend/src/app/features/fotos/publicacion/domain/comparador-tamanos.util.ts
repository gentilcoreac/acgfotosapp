/**
 * Comparador de tamaños (spec 8.3): mismos lados mayores que se pueden configurar en
 * `OpcionesPublicacion.ladoMayorPreview/ladoMayorThumb`, para que el fotógrafo vea de antemano el
 * costo (peso) y el resultado (nitidez de impresión) de cada uno sobre una foto real.
 */
export const TAMANOS_COMPARADOR: readonly number[] = [300, 600, 900, 1200, 1600];

/** El papel de referencia (10×15cm) mide 15cm de lado mayor — de ahí sale el dpi equivalente. */
const LADO_MAYOR_IMPRESION_CM = 15;
const CM_POR_PULGADA = 2.54;

/** Dpi al imprimir una imagen de `ladoMayorPx` en el lado mayor de un 10×15. */
export function calcularDpi(ladoMayorPx: number): number {
  return ladoMayorPx / (LADO_MAYOR_IMPRESION_CM / CM_POR_PULGADA);
}

/**
 * Dimensiones resultantes de llevar una imagen a `ladoMayorDestino`, preservando el aspect ratio y
 * **sin agrandar** más allá del tamaño original (mismo criterio que la composición de marca de agua,
 * ADR-15 §8: agrandar un bitmap no tiene arreglo, así que el piso real es el tamaño que ya existe).
 */
export function calcularDimensionesDestino(
  anchoOriginal: number,
  altoOriginal: number,
  ladoMayorDestino: number,
): { ancho: number; alto: number } {
  const ladoMayorOriginal = Math.max(anchoOriginal, altoOriginal);
  if (ladoMayorOriginal <= 0) {
    return { ancho: 0, alto: 0 };
  }
  const escala = Math.min(ladoMayorDestino, ladoMayorOriginal) / ladoMayorOriginal;
  return {
    ancho: Math.round(anchoOriginal * escala),
    alto: Math.round(altoOriginal * escala),
  };
}

/** Peso legible (KB, un decimal) — mismo formato en toda la pantalla. */
export function formatearPeso(bytes: number): string {
  return `${(bytes / 1024).toFixed(1)} KB`;
}

/** Una fila del comparador: el tamaño pedido, lo que realmente se generó y su costo. */
export interface ResultadoComparador {
  ladoMayorPedido: number;
  ancho: number;
  alto: number;
  dpi: number;
  pesoBytes: number;
}
