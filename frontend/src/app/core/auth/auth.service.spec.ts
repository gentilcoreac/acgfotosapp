import { provideHttpClient, withXhr } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { AppConfig } from '../config/app-config.model';
import { AppConfigService } from '../config/app-config.service';
import { AuthService } from './auth.service';
import { AuthStore } from './auth.store';

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
    tenantId: 1,
    hasVerifiedEmail: true,
  };
}

describe('AuthService', () => {
  let service: AuthService;
  let store: AuthStore;
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    TestBed.configureTestingModule({
      providers: [provideRouter([]), provideHttpClient(withXhr()), provideHttpClientTesting()],
    });
    vi.spyOn(window, 'fetch').mockResolvedValue(
      new Response(JSON.stringify(sampleConfig), { status: 200 }),
    );
    await TestBed.inject(AppConfigService).load();
    service = TestBed.inject(AuthService);
    store = TestBed.inject(AuthStore);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('login pega a /auth/token con withCredentials y setea la sesión', () => {
    service.login({ userName: 'root', password: 'pass' }).subscribe();
    const req = httpMock.expectOne((r) => r.url.endsWith('/api/auth/token'));
    expect(req.request.withCredentials).toBe(true);
    req.flush(tokenResponse('access1'));
    expect(store.accessToken()).toBe('access1');
    // login dispara la carga de aplicaciones permitidas del usuario
    httpMock.expectOne((r) => r.url.includes('aplicaciones-permitidas')).flush([]);
  });

  it('refreshSession comparte un único request en vuelo (serializado)', () => {
    service.refreshSession().subscribe();
    service.refreshSession().subscribe();

    const reqs = httpMock.match((r) => r.url.endsWith('/api/auth/refresh'));
    expect(reqs.length).toBe(1);
    expect(reqs[0].request.withCredentials).toBe(true);
    reqs[0].flush(tokenResponse('access2'));
    expect(store.accessToken()).toBe('access2');
  });

  it('logout pega a /auth/logout con withCredentials y limpia la sesión', () => {
    store.setSession('access1', new Date(Date.now() + 60000));
    service.logout();
    const req = httpMock.expectOne((r) => r.url.endsWith('/api/auth/logout'));
    expect(req.request.withCredentials).toBe(true);
    req.flush(null);
    expect(store.accessToken()).toBeNull();
    expect(store.isAuthenticated()).toBe(false);
  });
});
