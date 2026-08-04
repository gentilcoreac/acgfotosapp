import {
  CapaMarcaAgua,
  MODO_COLOCACION,
  MODO_FUSION_COMPOSITE_OPERATION,
  POSICION,
  Posicion,
} from './marca-agua.model';

/**
 * Composición de capas en canvas — réplica en TypeScript de
 * `ImageSharpImageProcessor.ComponerCapa` (backend, SkiaSharp). El reparto front/API (ADR-15, D1)
 * depende de que las dos implementaciones coincidan: acá se usa para la vista previa del editor y
 * para renderizar la marca real en el listado; la paridad la sostiene `BlendModeParityTests`
 * (backend) comparando contra fixtures reales de este mismo motor (Chromium/canvas).
 */

const PITCH_X_FACTOR = 1.25;
const PITCH_Y_FACTOR = 2.2;

/** Una capa lista para componer: sus parámetros + la imagen ya decodificada. */
export interface CapaComposicion {
  readonly capa: CapaMarcaAgua;
  readonly imagen: CanvasImageSource;
}

/** Compone las capas, en orden, sobre el canvas de destino (que ya tiene la foto dibujada). */
export function componerMarcaAgua(
  ctx: CanvasRenderingContext2D,
  anchoFoto: number,
  altoFoto: number,
  capas: readonly CapaComposicion[],
): void {
  for (const { capa, imagen } of [...capas].sort((a, b) => a.capa.orden - b.capa.orden)) {
    componerCapa(ctx, anchoFoto, altoFoto, capa, imagen);
  }
}

function componerCapa(
  ctx: CanvasRenderingContext2D,
  anchoFoto: number,
  altoFoto: number,
  capa: CapaMarcaAgua,
  imagen: CanvasImageSource,
): void {
  if (capa.anchoPx <= 0 || capa.altoPx <= 0) {
    return;
  }

  // Escala en % del ancho de la foto, pero NUNCA hacia arriba (ADR-15 §8): el piso es el tamaño
  // natural del asset.
  const anchoDestino = Math.min(anchoFoto * (capa.escalaPorcentaje / 100), capa.anchoPx);
  const altoDestino = capa.altoPx * (anchoDestino / capa.anchoPx);

  ctx.save();
  ctx.globalAlpha = Math.min(Math.max(capa.opacidad, 0), 1);
  ctx.globalCompositeOperation = MODO_FUSION_COMPOSITE_OPERATION[capa.modoFusion];

  if (capa.modoColocacion === MODO_COLOCACION.PosicionFija) {
    const margen = anchoFoto * (capa.margenPorcentaje / 100);
    const { x, y } = calcularPosicionFija(
      capa.posicion ?? POSICION.Centro,
      anchoFoto,
      altoFoto,
      anchoDestino,
      altoDestino,
      margen,
    );

    ctx.translate(x + anchoDestino / 2, y + altoDestino / 2);
    ctx.rotate(gradosARadianes(capa.anguloGrados));
    ctx.drawImage(imagen, -anchoDestino / 2, -altoDestino / 2, anchoDestino, altoDestino);
    ctx.restore();
    return;
  }

  // Repetida: grilla en mosaico con offset alternado por fila (patrón ladrillo), rotada como
  // conjunto — se recorre un área mayor a la foto para que la rotación no deje esquinas limpias.
  const pitchX = anchoDestino * PITCH_X_FACTOR;
  const pitchY = altoDestino * PITCH_Y_FACTOR;
  if (pitchX <= 0 || pitchY <= 0) {
    ctx.restore();
    return;
  }

  ctx.translate(anchoFoto / 2, altoFoto / 2);
  ctx.rotate(gradosARadianes(capa.anguloGrados));
  ctx.translate(-anchoFoto / 2, -altoFoto / 2);

  let fila = 0;
  for (let y = -altoFoto; y < altoFoto * 2; y += pitchY) {
    const offsetX = fila % 2 === 0 ? 0 : pitchX / 2;
    for (let x = -anchoFoto + offsetX; x < anchoFoto * 2; x += pitchX) {
      ctx.drawImage(imagen, x, y, anchoDestino, altoDestino);
    }
    fila++;
  }

  ctx.restore();
}

function calcularPosicionFija(
  posicion: Posicion,
  anchoFoto: number,
  altoFoto: number,
  anchoCapa: number,
  altoCapa: number,
  margen: number,
): { x: number; y: number } {
  const x = esIzquierda(posicion)
    ? margen
    : esCentroHorizontal(posicion)
      ? (anchoFoto - anchoCapa) / 2
      : anchoFoto - anchoCapa - margen;

  const y = esArriba(posicion)
    ? margen
    : esCentroVertical(posicion)
      ? (altoFoto - altoCapa) / 2
      : altoFoto - altoCapa - margen;

  return { x, y };
}

function esIzquierda(p: Posicion): boolean {
  return p === POSICION.ArribaIzquierda || p === POSICION.CentroIzquierda || p === POSICION.AbajoIzquierda;
}

function esCentroHorizontal(p: Posicion): boolean {
  return p === POSICION.ArribaCentro || p === POSICION.Centro || p === POSICION.AbajoCentro;
}

function esArriba(p: Posicion): boolean {
  return p === POSICION.ArribaIzquierda || p === POSICION.ArribaCentro || p === POSICION.ArribaDerecha;
}

function esCentroVertical(p: Posicion): boolean {
  return p === POSICION.CentroIzquierda || p === POSICION.Centro || p === POSICION.CentroDerecha;
}

function gradosARadianes(grados: number): number {
  return (grados * Math.PI) / 180;
}
