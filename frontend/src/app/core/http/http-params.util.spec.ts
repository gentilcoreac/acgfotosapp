import { toHttpParams } from './http-params.util';

describe('toHttpParams', () => {
  it('devuelve params vacíos si no hay query', () => {
    expect(toHttpParams().keys().length).toBe(0);
  });

  it('ignora null, undefined y string vacío, pero conserva 0 y false', () => {
    const params = toHttpParams({ a: '', b: null, c: undefined, page: 0, descendingOrder: false });
    expect(params.has('a')).toBe(false);
    expect(params.has('b')).toBe(false);
    expect(params.has('c')).toBe(false);
    expect(params.get('page')).toBe('0');
    expect(params.get('descendingOrder')).toBe('false');
  });

  it('serializa valores a string', () => {
    const params = toHttpParams({ searchText: 'hola', page: 2, descendingOrder: true });
    expect(params.get('searchText')).toBe('hola');
    expect(params.get('page')).toBe('2');
    expect(params.get('descendingOrder')).toBe('true');
  });
});
