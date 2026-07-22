const UMBRAL_DESVIACION = 0.12;

/**
 * Best-effort: `TamanoPrecio.Nombre` es texto libre por evento (ej. "10x15"), no ancho/alto
 * estructurados. Si el nombre no matchea el patrón "NxM" (ej. "Panorámica"), no se avisa — no
 * bloquea nada. Normaliza orientación con `Math.max(r, 1/r)` porque una foto puede salir vertical
 * u horizontal sin relación con la orientación del nombre del tamaño.
 */
export function detectarDesajusteProporcion(nombreTamano: string, anchoFoto: number, altoFoto: number): boolean {
  const match = /^(\d+)\s*[xX]\s*(\d+)$/.exec(nombreTamano.trim());
  if (!match || !anchoFoto || !altoFoto) {
    return false;
  }

  const [ancho, alto] = [Number(match[1]), Number(match[2])];
  const ratioTamano = Math.max(ancho / alto, alto / ancho);
  const ratioFoto = Math.max(anchoFoto / altoFoto, altoFoto / anchoFoto);
  return Math.abs(ratioTamano - ratioFoto) / ratioTamano > UMBRAL_DESVIACION;
}
