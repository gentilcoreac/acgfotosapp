import { TestBed } from '@angular/core/testing';
import { AuthStore } from './auth.store';

/** Arma un JWT de prueba (sin firma real) con el payload de claims dado. */
function fakeJwt(payload: Record<string, unknown>): string {
  const encode = (obj: object): string => btoa(JSON.stringify(obj)).replace(/=/g, '');
  return `${encode({ alg: 'none' })}.${encode(payload)}.sig`;
}

describe('AuthStore', () => {
  let store: AuthStore;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    store = TestBed.inject(AuthStore);
  });

  it('arranca sin sesión', () => {
    expect(store.accessToken()).toBeNull();
    expect(store.isAuthenticated()).toBe(false);
  });

  it('setSession guarda el token y autentica con expiración futura', () => {
    store.setSession('tok', new Date(Date.now() + 60000));
    expect(store.accessToken()).toBe('tok');
    expect(store.isAuthenticated()).toBe(true);
  });

  it('no autentica si la expiración ya pasó', () => {
    store.setSession('tok', new Date(Date.now() - 1000));
    expect(store.accessToken()).toBe('tok');
    expect(store.isAuthenticated()).toBe(false);
  });

  it('clearSession limpia la sesión', () => {
    store.setSession('tok', new Date(Date.now() + 60000));
    store.clearSession();
    expect(store.accessToken()).toBeNull();
    expect(store.isAuthenticated()).toBe(false);
  });

  it('isRoot es false sin sesión', () => {
    expect(store.isRoot()).toBe(false);
  });

  it('isRoot lee el claim isRoot del JWT (True → root)', () => {
    store.setSession(fakeJwt({ isRoot: 'True' }), new Date(Date.now() + 60000));
    expect(store.isRoot()).toBe(true);
  });

  it('isRoot es false si el claim isRoot es False', () => {
    store.setSession(fakeJwt({ isRoot: 'False' }), new Date(Date.now() + 60000));
    expect(store.isRoot()).toBe(false);
  });

  it('isRoot vuelve a false tras clearSession', () => {
    store.setSession(fakeJwt({ isRoot: 'True' }), new Date(Date.now() + 60000));
    store.clearSession();
    expect(store.isRoot()).toBe(false);
  });

  it('currentUserName es null sin sesión', () => {
    expect(store.currentUserName()).toBeNull();
  });

  it('currentUserName lee el claim sub (usuario efectivo)', () => {
    store.setSession(fakeJwt({ sub: 'qa.admin' }), new Date(Date.now() + 60000));
    expect(store.currentUserName()).toBe('qa.admin');
  });

  it('isImpersonating es false sin claim impersonatedBy', () => {
    store.setSession(fakeJwt({ isRoot: 'False', sub: 'user' }), new Date(Date.now() + 60000));
    expect(store.isImpersonating()).toBe(false);
  });

  it('isImpersonating es true con claim impersonatedBy (sesión de impersonalización)', () => {
    store.setSession(fakeJwt({ sub: 'target', impersonatedBy: '1' }), new Date(Date.now() + 60000));
    expect(store.isImpersonating()).toBe(true);
  });

  it('isImpersonating vuelve a false tras clearSession', () => {
    store.setSession(fakeJwt({ impersonatedBy: '1' }), new Date(Date.now() + 60000));
    store.clearSession();
    expect(store.isImpersonating()).toBe(false);
  });

  it('tenantId es null sin sesión', () => {
    expect(store.tenantId()).toBeNull();
  });

  it('tenantId lee el claim tenant como número', () => {
    store.setSession(fakeJwt({ tenant: '7' }), new Date(Date.now() + 60000));
    expect(store.tenantId()).toBe(7);
  });

  it('tenantId vuelve a null tras clearSession', () => {
    store.setSession(fakeJwt({ tenant: '7' }), new Date(Date.now() + 60000));
    store.clearSession();
    expect(store.tenantId()).toBeNull();
  });
});
