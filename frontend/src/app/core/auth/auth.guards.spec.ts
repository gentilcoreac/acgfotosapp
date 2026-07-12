import type { Mock } from 'vitest';
import { TestBed } from '@angular/core/testing';
import {
  ActivatedRouteSnapshot,
  CanMatchFn,
  PartialMatchRouteSnapshot,
  Route,
  Router,
  RouterStateSnapshot,
  UrlTree,
  provideRouter,
} from '@angular/router';
import { Observable, of } from 'rxjs';
import { AllowedRoutesService } from './allowed-routes.service';
import { allowedRoutesGuard, anonGuard, authGuard } from './auth.guards';
import { AuthStore } from './auth.store';

describe('auth guards', () => {
  let store: AuthStore;
  let router: Router;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideRouter([])] });
    store = TestBed.inject(AuthStore);
    router = TestBed.inject(Router);
  });

  function run(guard: CanMatchFn): boolean | UrlTree {
    return TestBed.runInInjectionContext(() =>
      guard({} as Route, [], {} as PartialMatchRouteSnapshot),
    ) as boolean | UrlTree;
  }

  function authenticate(): void {
    store.setSession('tok', new Date(Date.now() + 60000));
  }

  it('authGuard deja pasar si hay sesión', () => {
    authenticate();
    expect(run(authGuard)).toBe(true);
  });

  it('authGuard redirige a /login sin sesión', () => {
    const result = run(authGuard);
    expect(result).toEqual(router.createUrlTree(['/login']));
  });

  it('authGuard redirige a /reconnecting si la sesión está en estado reconnecting', () => {
    store.setReconnecting(true);
    const result = run(authGuard);
    expect(result).toEqual(
      router.createUrlTree(['/reconnecting'], { queryParams: { returnUrl: '/' } }),
    );
  });

  it('anonGuard deja pasar sin sesión', () => {
    expect(run(anonGuard)).toBe(true);
  });

  it('anonGuard redirige a / si hay sesión', () => {
    authenticate();
    expect(run(anonGuard)).toEqual(router.createUrlTree(['/']));
  });
});

describe('allowedRoutesGuard', () => {
  let router: Router;
  let isAllowed: Mock;

  beforeEach(() => {
    isAllowed = vi.fn().mockName('isAllowed');
    TestBed.configureTestingModule({
      providers: [provideRouter([]), { provide: AllowedRoutesService, useValue: { isAllowed } }],
    });
    router = TestBed.inject(Router);
  });

  function run(url: string): boolean | UrlTree {
    const state = { url } as RouterStateSnapshot;
    let result!: boolean | UrlTree;
    TestBed.runInInjectionContext(() => {
      const out = allowedRoutesGuard({} as ActivatedRouteSnapshot, state);
      (out as Observable<boolean | UrlTree>).subscribe((value) => (result = value));
    });
    return result;
  }

  it('deja pasar si la ruta está permitida', () => {
    isAllowed.mockReturnValue(of(true));
    expect(run('/menus')).toBe(true);
  });

  it('redirige a /sin-permisos si la ruta no está permitida', () => {
    isAllowed.mockReturnValue(of(false));
    expect(run('/permisos')).toEqual(router.createUrlTree(['/sin-permisos']));
  });
});
