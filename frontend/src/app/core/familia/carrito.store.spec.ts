import { ApplicationRef } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { CarritoStore } from './carrito.store';
import { FamiliaSessionStore } from './familia-session.store';
import { CanjeResult } from './models/canje-result.model';

function fakeSession(overrides: Partial<CanjeResult> = {}): CanjeResult {
  return {
    token: 'tok-1',
    validTo: new Date(Date.now() + 30 * 60000).toISOString(),
    eventoId: 1,
    nombreEvento: 'Egresados 2026',
    participantes: [{ id: 100, nombre: 'Ana Pérez' }],
    ...overrides,
  };
}

describe('CarritoStore', () => {
  let store: CarritoStore;
  let sessionStore: FamiliaSessionStore;

  beforeEach(() => {
    sessionStorage.clear();
    TestBed.configureTestingModule({});
    sessionStore = TestBed.inject(FamiliaSessionStore);
    store = TestBed.inject(CarritoStore);
  });

  afterEach(() => sessionStorage.clear());

  it('arranca vacío', () => {
    expect(store.lineas()).toEqual([]);
    expect(store.totalItems()).toBe(0);
  });

  it('agregar crea una línea nueva', () => {
    store.agregar(1, 10, 2);
    expect(store.lineas()).toEqual([{ fotoId: 1, tamanoPrecioId: 10, cantidad: 2 }]);
    expect(store.totalItems()).toBe(2);
  });

  it('agregar sobre una línea existente suma cantidad, no duplica la línea', () => {
    store.agregar(1, 10, 2);
    store.agregar(1, 10, 3);
    expect(store.lineas()).toEqual([{ fotoId: 1, tamanoPrecioId: 10, cantidad: 5 }]);
  });

  it('agregar el mismo fotoId con OTRO tamaño crea una línea aparte', () => {
    store.agregar(1, 10, 1);
    store.agregar(1, 20, 1);
    expect(store.lineas().length).toBe(2);
  });

  it('cantidadTotalDeFoto suma las copias de esa foto en todos los tamaños (badge de la grilla)', () => {
    store.agregar(1, 10, 2);
    store.agregar(1, 20, 3);
    store.agregar(2, 10, 5); // otra foto, no debe sumar
    expect(store.cantidadTotalDeFoto(1)).toBe(5);
    expect(store.cantidadTotalDeFoto(2)).toBe(5);
    expect(store.cantidadTotalDeFoto(3)).toBe(0);
  });

  it('cantidadDe devuelve 0 si la línea no está', () => {
    expect(store.cantidadDe(1, 10)).toBe(0);
    store.agregar(1, 10, 4);
    expect(store.cantidadDe(1, 10)).toBe(4);
  });

  it('actualizarCantidad fija la cantidad de una línea existente', () => {
    store.agregar(1, 10, 2);
    store.actualizarCantidad(1, 10, 9);
    expect(store.cantidadDe(1, 10)).toBe(9);
  });

  it('actualizarCantidad a 0 o menos quita la línea', () => {
    store.agregar(1, 10, 2);
    store.actualizarCantidad(1, 10, 0);
    expect(store.lineas()).toEqual([]);
  });

  it('quitar elimina solo esa línea', () => {
    store.agregar(1, 10, 1);
    store.agregar(2, 10, 1);
    store.quitar(1, 10);
    expect(store.lineas()).toEqual([{ fotoId: 2, tamanoPrecioId: 10, cantidad: 1 }]);
  });

  it('vaciar deja el carrito vacío y limpia sessionStorage', () => {
    store.agregar(1, 10, 1);
    store.vaciar();
    expect(store.lineas()).toEqual([]);
    expect(sessionStorage.getItem('acgfotos.familia.carrito')).toBeNull();
  });

  it('persiste en sessionStorage y una instancia nueva la restaura', () => {
    store.agregar(1, 10, 2);

    const restored = TestBed.runInInjectionContext(() => new CarritoStore());
    expect(restored.lineas()).toEqual([{ fotoId: 1, tamanoPrecioId: 10, cantidad: 2 }]);
  });

  it('un cambio de sesión de familia vacía el carrito (dispositivo compartido, ADR-11)', () => {
    sessionStore.setSession(fakeSession());
    store.agregar(1, 10, 2);
    expect(store.lineas().length).toBe(1);

    sessionStore.setSession(fakeSession({ token: 'tok-2' }));
    TestBed.inject(ApplicationRef).tick();

    expect(store.lineas()).toEqual([]);
  });
});
