import { LineaAgregadoImpresion } from '../domain/pedido.model';
import { detectarDesajusteProporcion } from './proporcion-foto.util';

/** Comillas dobles si el valor trae coma, comilla o salto de línea (CSV estándar). */
function escapeCsv(valor: string): string {
  if (/[",\n]/.test(valor)) {
    return `"${valor.replaceAll('"', '""')}"`;
  }
  return valor;
}

/** CSV del agregado (para laboratorios que piden planilla en vez de PDF/imagen). */
export function armarCsvAgregado(lineas: LineaAgregadoImpresion[]): string {
  const filas = lineas.map((linea) => {
    const advertencia = detectarDesajusteProporcion(linea.tamanoPrecioNombre, linea.anchoFoto, linea.altoFoto)
      ? 'Sí'
      : '';
    return [
      escapeCsv(linea.nombreArchivoOriginal),
      escapeCsv(linea.tamanoPrecioNombre),
      String(linea.cantidadTotal),
      advertencia,
    ].join(',');
  });

  return ['Foto,Tamaño,Cantidad,Advertencia proporción', ...filas].join('\n');
}
