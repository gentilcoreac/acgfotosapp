import { HttpClient, provideHttpClient, withInterceptors, withXhr } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { AppConfig } from '../config/app-config.model';
import { AppConfigService } from '../config/app-config.service';
import { authInterceptor } from './auth.interceptor';
import { AuthService } from './auth.service';
import { AuthStore } from './auth.store';

const PROTECTED_URL = 'https://api.test.com/api/general/Role';
const ANONYMOUS_URL = 'https://api.test.com/api/auth/token';

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

function futureIso(): string {
  return new Date(Date.now() + 60000).toISOString();
}

describe('authInterceptor', () => {
  let http: HttpClient;
  let httpMock: HttpTestingController;
  let store: AuthStore;
  let authService: AuthService;

  beforeEach(async () => {
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        provideHttpClient(withXhr(), withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
      ],
    });
    vi.spyOn(window, 'fetch').mockResolvedValue(
      new Response(JSON.stringify(sampleConfig), { status: 200 }),
    );
    await TestBed.inject(AppConfigService).load();
    http = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);
    store = TestBed.inject(AuthStore);
    authService = TestBed.inject(AuthService);
    store.setSession('abc', new Date(Date.now() + 60000));
  });

  afterEach(() => httpMock.verify());

  it('agrega el header Authorization en requests protegidos', () => {
    http.get(PROTECTED_URL).subscribe();
    const req = httpMock.expectOne(PROTECTED_URL);
    expect(req.request.headers.get('Authorization')).toBe('Bearer abc');
    req.flush({});
  });

  it('no agrega Authorization en endpoints anónimos', () => {
    http.post(ANONYMOUS_URL, {}).subscribe();
    const req = httpMock.expectOne(ANONYMOUS_URL);
    expect(req.request.headers.has('Authorization')).toBe(false);
    req.flush({});
  });

  it('ante 401 refresca y reintenta el request original con el token nuevo', () => {
    http.get(PROTECTED_URL).subscribe();

    const first = httpMock.expectOne(PROTECTED_URL);
    expect(first.request.headers.get('Authorization')).toBe('Bearer abc');
    first.flush('no autorizado', { status: 401, statusText: 'Unauthorized' });

    const refreshReq = httpMock.expectOne((r) => r.url.endsWith('/api/auth/refresh'));
    expect(refreshReq.request.withCredentials).toBe(true);
    refreshReq.flush({
      token: 'newtoken',
      validTo: futureIso(),
      tenantId: 1,
      hasVerifiedEmail: true,
    });

    const retry = httpMock.expectOne(PROTECTED_URL);
    expect(retry.request.headers.get('Authorization')).toBe('Bearer newtoken');
    retry.flush({ ok: true });

    expect(store.accessToken()).toBe('newtoken');
  });

  it('si el refresh es rechazado (401), cierra sesión', () => {
    const logoutSpy = vi.spyOn(authService, 'logout').mockReturnValue(undefined);
    http.get(PROTECTED_URL).subscribe({ error: () => undefined });

    httpMock.expectOne(PROTECTED_URL).flush('no', { status: 401, statusText: 'Unauthorized' });
    httpMock
      .expectOne((r) => r.url.endsWith('/api/auth/refresh'))
      .flush('no', { status: 401, statusText: 'Unauthorized' });

    expect(logoutSpy).toHaveBeenCalled();
  });

  it('si el refresh falla por blip de red (status 0), NO cierra sesión', () => {
    const logoutSpy = vi.spyOn(authService, 'logout').mockReturnValue(undefined);
    http.get(PROTECTED_URL).subscribe({ error: () => undefined });

    httpMock.expectOne(PROTECTED_URL).flush('no', { status: 401, statusText: 'Unauthorized' });
    // Refresh con error de red/CORS (la API tuvo un hipo): status 0.
    httpMock
      .expectOne((r) => r.url.endsWith('/api/auth/refresh'))
      .error(new ProgressEvent('error'), { status: 0, statusText: 'Unknown Error' });

    expect(logoutSpy).not.toHaveBeenCalled();
  });
});
