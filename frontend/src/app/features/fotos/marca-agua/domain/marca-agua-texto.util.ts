/**
 * "Diseñar texto" en el editor: una forma más de fabricar el PNG con transparencia que una capa
 * necesita (D14/ADR-15 §2 — el editor de texto no es parte del contrato, sólo otro camino hacia el
 * mismo archivo que también puede llegar subiendo un logo). El resultado sale por el mismo
 * `MarcaAguaService.subirCapa` que un archivo elegido a mano.
 */

/** Parámetros del texto que el fotógrafo diseña en el editor. */
export interface DisenoTexto {
  readonly texto: string;
  readonly color: string;
  readonly negrita: boolean;
  readonly contorno: boolean;
}

/**
 * Ancho de referencia que el backend usa para juzgar si un asset alcanza a una escala dada
 * (`IValidadorAssetMarcaAgua.EvaluarEscala`, default 1600px — ADR-15 §8/design.md D6). Rasterizar el
 * texto a este ancho garantiza que nunca haga falta agrandarlo, sea cual sea la escala (1-100%) que
 * se elija después en el editor de colocación.
 */
export const ANCHO_REFERENCIA_FOTO_PX = 1600;

const FONT_FAMILY = 'system-ui, "Segoe UI", Roboto, sans-serif';
const FONT_SIZE_MEDICION_PX = 100;
/** Margen alrededor del texto, proporcional al tamaño de fuente final. */
const PADDING_FACTOR = 0.28;

function fuenteCss(negrita: boolean, tamanoPx: number): string {
  return `${negrita ? '700' : '400'} ${tamanoPx}px ${FONT_FAMILY}`;
}

/**
 * Dado el ancho que un texto mide a `tamanoMedicionPx`, calcula el tamaño de fuente que hace falta
 * para que, medido de nuevo a ese nuevo tamaño, ocupe `anchoDestinoPx`. Pura y testeable sin canvas
 * real — la parte que sí necesita canvas (medir/dibujar de verdad) vive en `rasterizarTextoComoPng`.
 */
export function calcularTamanoFuente(
  anchoMedidoPx: number,
  tamanoMedicionPx: number,
  anchoDestinoPx: number,
): number {
  if (anchoMedidoPx <= 0) {
    return tamanoMedicionPx;
  }
  return tamanoMedicionPx * (anchoDestinoPx / anchoMedidoPx);
}

/**
 * Rasteriza el texto a un PNG con transparencia, al tamaño máximo de uso (D6): mismo mecanismo de
 * alta que subir un logo, el backend no distingue de dónde salió el archivo.
 */
export async function rasterizarTextoComoPng(
  diseno: DisenoTexto,
  anchoDestinoPx = ANCHO_REFERENCIA_FOTO_PX,
): Promise<File> {
  const texto = diseno.texto.trim();
  if (!texto) {
    throw new Error('El texto no puede estar vacío.');
  }

  const medidor = document.createElement('canvas').getContext('2d');
  if (!medidor) {
    throw new Error('El navegador no soporta canvas 2D.');
  }
  medidor.font = fuenteCss(diseno.negrita, FONT_SIZE_MEDICION_PX);
  const anchoMedido = medidor.measureText(texto).width;
  const tamanoFuente = calcularTamanoFuente(anchoMedido, FONT_SIZE_MEDICION_PX, anchoDestinoPx);
  const padding = Math.ceil(tamanoFuente * PADDING_FACTOR);

  medidor.font = fuenteCss(diseno.negrita, tamanoFuente);
  const anchoTexto = Math.ceil(medidor.measureText(texto).width);
  const altoTexto = Math.ceil(tamanoFuente * 1.35);

  const lienzo = document.createElement('canvas');
  lienzo.width = Math.max(1, anchoTexto + padding * 2);
  lienzo.height = Math.max(1, altoTexto + padding * 2);

  const ctx = lienzo.getContext('2d');
  if (!ctx) {
    throw new Error('El navegador no soporta canvas 2D.');
  }
  ctx.font = fuenteCss(diseno.negrita, tamanoFuente);
  ctx.textBaseline = 'top';

  if (diseno.contorno) {
    ctx.strokeStyle = 'rgba(0, 0, 0, 0.85)';
    ctx.lineWidth = Math.max(1, tamanoFuente / 12);
    ctx.lineJoin = 'round';
    ctx.strokeText(texto, padding, padding);
  }
  ctx.fillStyle = diseno.color;
  ctx.fillText(texto, padding, padding);

  const blob = await new Promise<Blob | null>((resolve) => lienzo.toBlob(resolve, 'image/png'));
  if (!blob) {
    throw new Error('No se pudo generar la imagen del texto.');
  }
  return new File([blob], 'texto-marca-agua.png', { type: 'image/png' });
}
