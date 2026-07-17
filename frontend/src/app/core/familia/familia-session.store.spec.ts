import { TestBed } from '@angular/core/testing';
import { FamiliaSessionStore } from './familia-session.store';
import { CanjeResult } from './models/canje-result.model';

function fakeResult(overrides: Partial<CanjeResult> = {}): CanjeResult {
  return {
    token: 'tok',
    validTo: new Date(Date.now() + 30 * 60000).toISOString(),
    eventoId: 1,
    nombreEvento: 'Egresados 2026',
    participantes: [{ id: 100, nombre: 'Ana Pérez' }],
    ...overrides,
  };
}

describe('FamiliaSessionStore', () => {
  let store: FamiliaSessionStore;

  beforeEach(() => {
    sessionStorage.clear();
    TestBed.configureTestingModule({});
    store = TestBed.inject(FamiliaSessionStore);
  });

  afterEach(() => sessionStorage.clear());

  it('arranca sin sesión', () => {
    expect(store.token()).toBeNull();
    expect(store.isActive()).toBe(false);
  });

  it('setSession guarda el token y los datos de display', () => {
    store.setSession(fakeResult());
    expect(store.token()).toBe('tok');
    expect(store.isActive()).toBe(true);
    expect(store.eventoId()).toBe(1);
    expect(store.nombreEvento()).toBe('Egresados 2026');
    expect(store.participantes()).toEqual([{ id: 100, nombre: 'Ana Pérez' }]);
  });

  it('no está activa si el token ya venció', () => {
    store.setSession(fakeResult({ validTo: new Date(Date.now() - 1000).toISOString() }));
    expect(store.isActive()).toBe(false);
  });

  it('clearSession limpia la sesión', () => {
    store.setSession(fakeResult());
    store.clearSession();
    expect(store.token()).toBeNull();
    expect(store.isActive()).toBe(false);
    expect(store.participantes()).toEqual([]);
  });

  it('persiste en sessionStorage y una instancia nueva la restaura', () => {
    store.setSession(fakeResult());

    const restored = TestBed.runInInjectionContext(() => new FamiliaSessionStore());
    expect(restored.token()).toBe('tok');
    expect(restored.isActive()).toBe(true);
    expect(restored.nombreEvento()).toBe('Egresados 2026');
  });

  it('una sesión vencida en sessionStorage NO se restaura', () => {
    store.setSession(fakeResult({ validTo: new Date(Date.now() - 1000).toISOString() }));

    const restored = TestBed.runInInjectionContext(() => new FamiliaSessionStore());
    expect(restored.token()).toBeNull();
    expect(sessionStorage.getItem('acgfotos.familia.session')).toBeNull();
  });
});
