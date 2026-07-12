import { provideHttpClient, withXhr } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { AppConfig } from '../config/app-config.model';
import { AppConfigService } from '../config/app-config.service';
import { AllowedRoutesService } from './allowed-routes.service';
import { AuthStore } from './auth.store';
import { ImpersonationService } from './impersonation.service';

const sampleConfig = {
  apiUrl: 'https://api.test.com',
  inactivity: { maxTimeMs: 1, maxResponseTimeMs: 1 },
  pageSize: 100,
  pageSizeOptions: [10],
  defaultTenant: '',
  defaultTheme: 'light',
  phoneMinLength: 10,
  phoneMaxLength: 13,
  oldSystemUrl: '',
  appTitle: 'AcgFotos',
  year: '2026',
  version: '0.0.1',
  production: false,
} as AppConfig;

function tokenResponse(token: string) {
  return {
    token,
    validTo: new Date(Date.now() + 60000).toISOString(),
    tenantId: 4,
    hasVerifiedEmail: true,
  };
}

describe('ImpersonationService', () => {
  let service: ImpersonationService;
  let store: AuthStore;
  let allowedRoutes: AllowedRoutesService;
  let httpMock: HttpTestingController;
  let router: Router;

  beforeEach(async () => {
    TestBed.configureTestingModule({
      providers: [provideRouter([]), provideHttpClient(withXhr()), provideHttpClientTesting()],
    });
    vi.spyOn(window, 'fetch').mockResolvedValue(
      new Response(JSON.stringify(sampleConfig), { status: 200 }),
    );
    await TestBed.inject(AppConfigService).load();
    service = TestBed.inject(ImpersonationService);
    store = TestBed.inject(AuthStore);
    allowedRoutes = TestBed.inject(AllowedRoutesService);
    httpMock = TestBed.inject(HttpTestingController);
    router = TestBed.inject(Router);
  });

  afterEach(() => httpMock.verify());

  it('impersonate pega a /auth/impersonate con withCredentials + body y hace el swap de contexto', () => {
    const navSpy = vi
      .spyOn(router, 'navigateByUrl')
      .mockReturnValue(undefined as unknown as Promise<boolean>);
    const clearSpy = vi.spyOn(allowedRoutes, 'clear');

    service.impersonate(4, 10009).subscribe();

    const req = httpMock.expectOne((r) => r.url.endsWith('/api/auth/impersonation/start'));
    expect(req.request.withCredentials).toBe(true);
    expect(req.request.body).toEqual({ tenantId: 4, userId: 10009 });
    req.flush(tokenResponse('imp-token'));
    // applyContextSwitch recarga las apps del usuario efectivo
    httpMock.expectOne((r) => r.url.includes('aplicaciones-permitidas')).flush([]);

    expect(store.accessToken()).toBe('imp-token');
    expect(clearSpy).toHaveBeenCalled();
    expect(navSpy).toHaveBeenCalledWith('/', { replaceUrl: true });
  });

  it('getImpersonatableUsers pega a /auth/impersonatable-users/{tenantId} y devuelve la lista', () => {
    const sample = [
      { id: 10009, userName: 'qa.admin', nombre: 'QA', apellido: 'Admin', email: 'q@t' },
    ];
    let result: typeof sample | undefined;
    service.getImpersonatableUsers(4).subscribe((users) => (result = users));

    const req = httpMock.expectOne((r) => r.url.endsWith('/api/auth/impersonation/users/4'));
    expect(req.request.method).toBe('GET');
    req.flush(sample);
    expect(result).toEqual(sample);
  });

  it('stop pega a /auth/stop-impersonation con withCredentials y hace el swap de contexto', () => {
    const navSpy = vi
      .spyOn(router, 'navigateByUrl')
      .mockReturnValue(undefined as unknown as Promise<boolean>);

    service.stop().subscribe();

    const req = httpMock.expectOne((r) => r.url.endsWith('/api/auth/impersonation/stop'));
    expect(req.request.withCredentials).toBe(true);
    req.flush(tokenResponse('root-token'));
    // applyContextSwitch recarga las apps del usuario efectivo (vacías para root)
    httpMock.expectOne((r) => r.url.includes('aplicaciones-permitidas')).flush([]);

    expect(store.accessToken()).toBe('root-token');
    expect(navSpy).toHaveBeenCalledWith('/', { replaceUrl: true });
  });
});
