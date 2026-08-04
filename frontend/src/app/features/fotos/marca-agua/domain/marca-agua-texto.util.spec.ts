import { ANCHO_REFERENCIA_FOTO_PX, calcularTamanoFuente } from './marca-agua-texto.util';

// `rasterizarTextoComoPng` necesita canvas 2D real (medir/dibujar texto, `toBlob`), que jsdom no
// provee sin el paquete nativo `canvas` (no instalado acá) — mismo criterio ya documentado para
// `PerfilMarcaAguaCanvasComponent`/`ComparadorTamanosComponent` (tasks 7.8/8.3): sólo se prueba la
// parte pura (el cálculo de escala), no el dibujo en sí.
describe('calcularTamanoFuente', () => {
  it('escala el tamaño de fuente en proporción directa al ancho destino', () => {
    // Un texto que mide 800px a fuente 100px necesita fuente 200px para medir 1600px.
    expect(calcularTamanoFuente(800, 100, 1600)).toBe(200);
  });

  it('con el ancho de referencia del backend (1600px) da un tamaño mayor para texto corto', () => {
    // Texto corto (mide poco) -> hace falta una fuente más grande para llegar al mismo ancho destino.
    const corto = calcularTamanoFuente(150, 100, ANCHO_REFERENCIA_FOTO_PX);
    const largo = calcularTamanoFuente(900, 100, ANCHO_REFERENCIA_FOTO_PX);
    expect(corto).toBeGreaterThan(largo);
  });

  it('con ancho medido 0 (texto vacío) no divide por cero: devuelve el tamaño de medición tal cual', () => {
    expect(calcularTamanoFuente(0, 100, 1600)).toBe(100);
  });
});
