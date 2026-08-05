import { vi } from 'vitest';
import { ImagenDecodificada, dibujarFotoPropia } from './marca-agua-muestra.util';

/** Contexto 2D falso: sólo registra llamadas (mismo criterio que marca-agua-canvas.util.spec.ts). */
function fakeCtx() {
  return { drawImage: vi.fn() } as unknown as CanvasRenderingContext2D;
}

function imagen(width: number, height: number): ImagenDecodificada {
  return { width, height } as unknown as ImagenDecodificada;
}

describe('dibujarFotoPropia', () => {
  it('foto más ancha que el lienzo (relativamente): escala por alto y recorta a los costados', () => {
    const ctx = fakeCtx();
    // Lienzo 200x100 (2:1), foto 600x200 (3:1, relativamente más ancha) -> escala por ALTO: 100/200.
    dibujarFotoPropia(ctx, 200, 100, imagen(600, 200));

    const [, x, y, ancho, alto] = (ctx.drawImage as ReturnType<typeof vi.fn>).mock.calls[0];
    expect(alto).toBeCloseTo(100); // llena el alto exacto
    expect(ancho).toBeCloseTo(600 * (100 / 200));
    expect(ancho).toBeGreaterThan(200); // se pasa del ancho del lienzo -> recorta a los costados
    expect(x).toBeLessThan(0); // centrado: arranca antes del borde izquierdo
    expect(y).toBeCloseTo(0);
  });

  it('foto más angosta que el lienzo (relativamente): escala por ancho y recorta arriba/abajo', () => {
    const ctx = fakeCtx();
    // Lienzo 200x100, foto vertical 100x300 -> escala por ANCHO: 200/100.
    dibujarFotoPropia(ctx, 200, 100, imagen(100, 300));

    const [, x, y, ancho, alto] = (ctx.drawImage as ReturnType<typeof vi.fn>).mock.calls[0];
    expect(ancho).toBeCloseTo(200);
    expect(alto).toBeCloseTo(300 * (200 / 100));
    expect(alto).toBeGreaterThan(100);
    expect(y).toBeLessThan(0);
    expect(x).toBeCloseTo(0);
  });

  it('con dimensiones 0x0 no dibuja nada (evita NaN/Infinity)', () => {
    const ctx = fakeCtx();
    dibujarFotoPropia(ctx, 200, 100, imagen(0, 0));
    expect(ctx.drawImage).not.toHaveBeenCalled();
  });
});
