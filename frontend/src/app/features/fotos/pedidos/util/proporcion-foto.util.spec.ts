import { detectarDesajusteProporcion } from './proporcion-foto.util';

describe('detectarDesajusteProporcion', () => {
  it('no avisa si el nombre del tamaño no matchea el patrón NxM', () => {
    expect(detectarDesajusteProporcion('Panorámica', 4000, 1000)).toBe(false);
  });

  it('no avisa si la proporción de la foto coincide con la del tamaño (misma orientación)', () => {
    // 10x15 → ratio 1.5; foto 800x1200 → ratio 1.5.
    expect(detectarDesajusteProporcion('10x15', 800, 1200)).toBe(false);
  });

  it('no avisa si coincide aunque la foto esté en la orientación opuesta (normaliza ratio)', () => {
    // 10x15 pedido vertical, foto horizontal 1200x800 — misma proporción, solo rotada.
    expect(detectarDesajusteProporcion('10x15', 1200, 800)).toBe(false);
  });

  it('avisa si la proporción se desvía más del umbral', () => {
    // 20x10 ≈ 2.0; foto muy vertical 600x2000 ≈ 3.33 → desviación fuerte.
    expect(detectarDesajusteProporcion('20x10', 600, 2000)).toBe(true);
  });

  it('no avisa con ancho/alto en cero (dato incompleto)', () => {
    expect(detectarDesajusteProporcion('10x15', 0, 0)).toBe(false);
  });
});
