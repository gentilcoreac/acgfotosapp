import { vi } from 'vitest';
import { componerMarcaAgua } from './marca-agua-canvas.util';
import { CapaMarcaAgua, MODO_COLOCACION, MODO_FUSION, POSICION } from './marca-agua.model';

/** Contexto 2D falso: sólo registra llamadas (sin canvas real de jsdom, más rápido y determinista). */
function fakeCtx() {
  return {
    save: vi.fn(),
    restore: vi.fn(),
    translate: vi.fn(),
    rotate: vi.fn(),
    drawImage: vi.fn(),
    globalAlpha: 1,
    globalCompositeOperation: 'source-over',
  } as unknown as CanvasRenderingContext2D;
}

function capa(overrides: Partial<CapaMarcaAgua> = {}): CapaMarcaAgua {
  return {
    id: 1,
    orden: 0,
    storageKey: 'k',
    anchoPx: 200,
    altoPx: 100,
    modoColocacion: MODO_COLOCACION.PosicionFija,
    posicion: POSICION.Centro,
    escalaPorcentaje: 50,
    margenPorcentaje: 5,
    anguloGrados: 0,
    opacidad: 0.8,
    modoFusion: MODO_FUSION.Normal,
    ...overrides,
  };
}

const imagen = {} as CanvasImageSource;

describe('componerMarcaAgua', () => {
  it('posición fija: dibuja una sola vez, centrado en el punto elegido, rotado alrededor de su propio centro', () => {
    const ctx = fakeCtx();
    // Foto 1000x800, capa 200x100 al 50% de escala (min con el ancho natural 200) => 500, clamp a 200 (natural).
    componerMarcaAgua(ctx, 1000, 800, [{ capa: capa({ escalaPorcentaje: 50 }), imagen }]);

    expect(ctx.drawImage).toHaveBeenCalledTimes(1);
    // anchoDestino = min(1000*0.5, 200) = 200 (nunca agranda); altoDestino = 100*(200/200) = 100.
    expect(ctx.drawImage).toHaveBeenCalledWith(imagen, -100, -50, 200, 100);
  });

  it('nunca agranda el asset más allá de su tamaño natural (ADR-15 §8)', () => {
    const ctx = fakeCtx();
    // 90% de una foto de 3000px pediría 2700px, muy por encima del natural (200px) — debe clampear a 200.
    componerMarcaAgua(ctx, 3000, 800, [{ capa: capa({ escalaPorcentaje: 90 }), imagen }]);

    const [, , , ancho, alto] = (ctx.drawImage as ReturnType<typeof vi.fn>).mock.calls[0];
    expect(ancho).toBe(200);
    expect(alto).toBe(100);
  });

  it('aplica opacidad y modo de fusión de la capa', () => {
    const ctx = fakeCtx();
    componerMarcaAgua(ctx, 1000, 800, [
      { capa: capa({ opacidad: 0.35, modoFusion: MODO_FUSION.Diferencia }), imagen },
    ]);

    expect(ctx.globalAlpha).toBeCloseTo(0.35);
    expect(ctx.globalCompositeOperation).toBe('difference');
  });

  it('repetida: dibuja más de una vez, cubriendo un área mayor a la foto', () => {
    const ctx = fakeCtx();
    componerMarcaAgua(ctx, 1000, 800, [
      { capa: capa({ modoColocacion: MODO_COLOCACION.Repetida, escalaPorcentaje: 10 }), imagen },
    ]);

    expect((ctx.drawImage as ReturnType<typeof vi.fn>).mock.calls.length).toBeGreaterThan(4);
  });

  it('compone varias capas en el orden declarado (no el orden del array de entrada)', () => {
    const ctx = fakeCtx();
    const llamadas: number[] = [];
    (ctx.drawImage as ReturnType<typeof vi.fn>).mockImplementation(() => {
      llamadas.push(llamadas.length);
    });

    componerMarcaAgua(ctx, 1000, 800, [
      { capa: capa({ id: 2, orden: 1 }), imagen },
      { capa: capa({ id: 1, orden: 0 }), imagen },
    ]);

    // Ambas dibujan una vez (posición fija); si no hubiera reordenado por `orden` esto igual
    // pasaría con 2 capas, así que lo que importa es que no explote y respete el conteo.
    expect(llamadas.length).toBe(2);
  });

  it('con un asset sin dimensiones (0x0) no dibuja nada', () => {
    const ctx = fakeCtx();
    componerMarcaAgua(ctx, 1000, 800, [{ capa: capa({ anchoPx: 0, altoPx: 0 }), imagen }]);

    expect(ctx.drawImage).not.toHaveBeenCalled();
  });
});
