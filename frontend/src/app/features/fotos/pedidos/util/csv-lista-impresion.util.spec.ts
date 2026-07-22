import { LineaAgregadoImpresion } from '../domain/pedido.model';
import { armarCsvAgregado } from './csv-lista-impresion.util';

function linea(overrides: Partial<LineaAgregadoImpresion> = {}): LineaAgregadoImpresion {
  return {
    fotoId: 1,
    nombreArchivoOriginal: 'foto1.jpg',
    anchoFoto: 800,
    altoFoto: 1200,
    tamanoPrecioNombre: '10x15',
    cantidadTotal: 3,
    ...overrides,
  };
}

describe('armarCsvAgregado', () => {
  it('arma la cabecera y una fila por línea', () => {
    const csv = armarCsvAgregado([linea()]);

    const [cabecera, fila] = csv.split('\n');
    expect(cabecera).toBe('Foto,Tamaño,Cantidad,Advertencia proporción');
    expect(fila).toBe('foto1.jpg,10x15,3,');
  });

  it('escapa valores con coma envolviéndolos en comillas dobles', () => {
    const csv = armarCsvAgregado([linea({ nombreArchivoOriginal: 'foto, final.jpg' })]);

    expect(csv.split('\n')[1]).toBe('"foto, final.jpg",10x15,3,');
  });

  it('duplica comillas internas al escapar', () => {
    const csv = armarCsvAgregado([linea({ nombreArchivoOriginal: 'foto "buena".jpg' })]);

    expect(csv.split('\n')[1]).toBe('"foto ""buena"".jpg",10x15,3,');
  });

  it('marca la advertencia de proporción cuando el tamaño y la foto no coinciden', () => {
    const csv = armarCsvAgregado([linea({ tamanoPrecioNombre: '20x10', anchoFoto: 600, altoFoto: 2000 })]);

    expect(csv.split('\n')[1]).toBe('foto1.jpg,20x10,3,Sí');
  });
});
