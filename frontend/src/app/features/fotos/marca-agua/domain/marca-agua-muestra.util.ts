/**
 * Fotos sintéticas para verificar la marca antes de tener fotos propias cargadas (spec 7.5: la
 * verificación es sobre tres muestras a la vez — clara, oscura y mixta — porque los modos de
 * fusión calculan el color a partir del pixel de abajo, y calibrar contra una sola foto rompe el
 * resto del evento). Puerto TS del generador del prototipo (`docs/ClaudeDesign/PropuestaMarcaAgua`).
 */
export type MuestraVariante = 'clara' | 'mixta' | 'oscura';

export const MUESTRAS: readonly { readonly clave: MuestraVariante; readonly etiqueta: string }[] = [
  { clave: 'clara', etiqueta: 'Clara' },
  { clave: 'mixta', etiqueta: 'Mixta' },
  { clave: 'oscura', etiqueta: 'Oscura' },
];

export function dibujarFotoMuestra(
  ctx: CanvasRenderingContext2D,
  ancho: number,
  alto: number,
  variante: MuestraVariante,
): void {
  const fondo = ctx.createLinearGradient(0, 0, ancho, alto);
  if (variante === 'clara') {
    fondo.addColorStop(0, '#f6f4ee');
    fondo.addColorStop(1, '#dcdeD7');
  } else if (variante === 'oscura') {
    fondo.addColorStop(0, '#1c2430');
    fondo.addColorStop(1, '#080b0f');
  } else {
    fondo.addColorStop(0, '#3d4c5a');
    fondo.addColorStop(0.55, '#6b7280');
    fondo.addColorStop(1, '#20262e');
  }
  ctx.fillStyle = fondo;
  ctx.fillRect(0, 0, ancho, alto);

  if (variante === 'mixta') {
    const claro = ctx.createRadialGradient(
      ancho * 0.3,
      alto * 0.55,
      0,
      ancho * 0.3,
      alto * 0.55,
      ancho * 0.34,
    );
    claro.addColorStop(0, 'rgba(250,248,243,1)');
    claro.addColorStop(1, 'rgba(250,248,243,0)');
    ctx.fillStyle = claro;
    ctx.fillRect(0, 0, ancho, alto);

    const oscuro = ctx.createRadialGradient(
      ancho * 0.78,
      alto * 0.65,
      0,
      ancho * 0.78,
      alto * 0.65,
      ancho * 0.3,
    );
    oscuro.addColorStop(0, 'rgba(6,8,11,.95)');
    oscuro.addColorStop(1, 'rgba(6,8,11,0)');
    ctx.fillStyle = oscuro;
    ctx.fillRect(0, 0, ancho, alto);
  }

  if (variante === 'clara') {
    const sombra = ctx.createRadialGradient(
      ancho * 0.75,
      alto * 0.7,
      0,
      ancho * 0.75,
      alto * 0.7,
      ancho * 0.3,
    );
    sombra.addColorStop(0, 'rgba(150,145,135,.45)');
    sombra.addColorStop(1, 'rgba(150,145,135,0)');
    ctx.fillStyle = sombra;
    ctx.fillRect(0, 0, ancho, alto);
  }

  // Franja inferior más oscura: simula el borde de una vestimenta/sombra baja, donde una capa
  // repetida con margen chico suele recortarse.
  ctx.fillStyle = variante === 'clara' ? 'rgba(0,0,0,.08)' : 'rgba(0,0,0,.22)';
  ctx.fillRect(0, alto * 0.86, ancho, alto * 0.14);
}
