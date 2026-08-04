import {
  TAMANOS_COMPARADOR,
  calcularDimensionesDestino,
  calcularDpi,
  formatearPeso,
} from './comparador-tamanos.util';

describe('calcularDpi', () => {
  it('un lado mayor de 1600px imprime a ~271 dpi en un 10×15', () => {
    expect(calcularDpi(1600)).toBeCloseTo(1600 / (15 / 2.54), 2);
    expect(calcularDpi(1600)).toBeCloseTo(270.93, 1);
  });

  it('300px (el piso del comparador) queda muy por debajo de una impresión nítida', () => {
    expect(calcularDpi(300)).toBeCloseTo(50.8, 1);
  });
});

describe('calcularDimensionesDestino', () => {
  it('escala proporcionalmente al lado mayor pedido', () => {
    expect(calcularDimensionesDestino(4000, 3000, 1600)).toEqual({ ancho: 1600, alto: 1200 });
  });

  it('nunca agranda más allá del tamaño original (ADR-15 §8)', () => {
    expect(calcularDimensionesDestino(800, 600, 1600)).toEqual({ ancho: 800, alto: 600 });
  });

  it('respeta una foto vertical (el lado mayor es el alto)', () => {
    expect(calcularDimensionesDestino(1200, 1600, 900)).toEqual({ ancho: 675, alto: 900 });
  });

  it('con dimensiones originales 0x0 no produce NaN/Infinity', () => {
    expect(calcularDimensionesDestino(0, 0, 900)).toEqual({ ancho: 0, alto: 0 });
  });
});

describe('formatearPeso', () => {
  it('formatea bytes como KB con un decimal', () => {
    expect(formatearPeso(153600)).toBe('150.0 KB');
    expect(formatearPeso(1536)).toBe('1.5 KB');
  });
});

describe('TAMANOS_COMPARADOR', () => {
  it('son los 5 lados mayores de la spec 8.3', () => {
    expect(TAMANOS_COMPARADOR).toEqual([300, 600, 900, 1200, 1600]);
  });
});
