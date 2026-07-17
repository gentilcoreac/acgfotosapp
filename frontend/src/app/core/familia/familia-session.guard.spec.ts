import { TestBed } from '@angular/core/testing';
import {
  CanMatchFn,
  PartialMatchRouteSnapshot,
  Route,
  Router,
  UrlTree,
  provideRouter,
} from '@angular/router';
import { familiaSessionGuard } from './familia-session.guard';
import { FamiliaSessionStore } from './familia-session.store';
import { CanjeResult } from './models/canje-result.model';

describe('familiaSessionGuard', () => {
  let store: FamiliaSessionStore;
  let router: Router;

  beforeEach(() => {
    sessionStorage.clear();
    TestBed.configureTestingModule({ providers: [provideRouter([])] });
    store = TestBed.inject(FamiliaSessionStore);
    router = TestBed.inject(Router);
  });

  afterEach(() => sessionStorage.clear());

  function run(guard: CanMatchFn): boolean | UrlTree {
    return TestBed.runInInjectionContext(() =>
      guard({} as Route, [], {} as PartialMatchRouteSnapshot),
    ) as boolean | UrlTree;
  }

  it('deja pasar con sesión activa', () => {
    store.setSession({
      token: 'tok',
      validTo: new Date(Date.now() + 60000).toISOString(),
      eventoId: 1,
      nombreEvento: 'Egresados 2026',
      participantes: [{ id: 100, nombre: 'Ana Pérez' }],
    } as CanjeResult);

    expect(run(familiaSessionGuard)).toBe(true);
  });

  it('redirige a /canje sin sesión', () => {
    expect(run(familiaSessionGuard)).toEqual(router.createUrlTree(['/canje']));
  });
});
