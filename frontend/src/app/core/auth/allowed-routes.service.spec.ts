import { provideHttpClient, withXhr } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { AppConfig } from '../config/app-config.model';
import { AppConfigService } from '../config/app-config.service';
import { AllowedRoutesService } from './allowed-routes.service';
import { AllowedRoute } from './models/allowed-route.model';

const sampleConfig: AppConfig = {
  appTitle: 'AcgFotos',
  year: '2026',
  version: '0.0.1',
  production: false,
  apiUrl: 'https://api.test.com',
  inactivity: { maxTimeMs: 1, maxResponseTimeMs: 1 },
  pageSize: 100,
  pageSizeOptions: [10, 100],
  defaultTenant: '',
  defaultTheme: 'light',
  phoneMinLength: 10,
  phoneMaxLength: 13,
  oldSystemUrl: '',
};

const ROUTES_URL = 'https://api.test.com/api/general/menus/allowed-routes';

const routes: AllowedRoute[] = [
  { id: 1, nombre: 'Menus', codigo: 'Menus', routePath: '/menus' },
  { id: 2, nombre: 'Tenants', codigo: 'Tenants', routePath: '/tenants' },
  { id: 3, nombre: 'Contenedor', codigo: 'Cont', routePath: null },
];

describe('AllowedRoutesService', () => {
  let service: AllowedRoutesService;
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(withXhr()), provideHttpClientTesting()],
    });
    vi.spyOn(window, 'fetch').mockResolvedValue(
      new Response(JSON.stringify(sampleConfig), { status: 200 }),
    );
    await TestBed.inject(AppConfigService).load();
    service = TestBed.inject(AllowedRoutesService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('deja pasar el home sin pegarle a la API', () => {
    let result: boolean | undefined;
    service.isAllowed('/').subscribe((ok) => (result = ok));
    httpMock.expectNone(ROUTES_URL);
    expect(result).toBe(true);
  });

  it('deja pasar las páginas de error (/404, /sin-permisos) sin pegarle a la API', () => {
    let notFound: boolean | undefined;
    let denied: boolean | undefined;
    service.isAllowed('/404').subscribe((ok) => (notFound = ok));
    service.isAllowed('/sin-permisos').subscribe((ok) => (denied = ok));
    httpMock.expectNone(ROUTES_URL);
    expect(notFound).toBe(true);
    expect(denied).toBe(true);
  });

  it('permite una ruta que está en allowed-routes', () => {
    let result: boolean | undefined;
    service.isAllowed('/menus').subscribe((ok) => (result = ok));
    httpMock.expectOne(ROUTES_URL).flush(routes);
    expect(result).toBe(true);
  });

  it('deniega una ruta que no está en allowed-routes', () => {
    let result: boolean | undefined;
    service.isAllowed('/permisos').subscribe((ok) => (result = ok));
    httpMock.expectOne(ROUTES_URL).flush(routes);
    expect(result).toBe(false);
  });

  it('ignora id final y query string al comparar', () => {
    let result: boolean | undefined;
    service.isAllowed('/menus/12?foo=bar').subscribe((ok) => (result = ok));
    httpMock.expectOne(ROUTES_URL).flush(routes);
    expect(result).toBe(true);
  });

  it('permite una sub-ruta sin menú propio (p. ej. tenants/:id/databases) si el padre está permitido', () => {
    let result: boolean | undefined;
    service.isAllowed('/tenants/5/databases').subscribe((ok) => (result = ok));
    httpMock.expectOne(ROUTES_URL).flush(routes);
    expect(result).toBe(true);
  });

  it('deniega una sub-ruta cuyo padre no está permitido', () => {
    let result: boolean | undefined;
    service.isAllowed('/permisos/5/detalle').subscribe((ok) => (result = ok));
    httpMock.expectOne(ROUTES_URL).flush(routes);
    expect(result).toBe(false);
  });

  it('cachea: el segundo chequeo no vuelve a pegarle a la API', () => {
    service.isAllowed('/menus').subscribe();
    httpMock.expectOne(ROUTES_URL).flush(routes);

    let result: boolean | undefined;
    service.isAllowed('/tenants').subscribe((ok) => (result = ok));
    httpMock.expectNone(ROUTES_URL);
    expect(result).toBe(true);
  });

  it('clear() fuerza un nuevo fetch', () => {
    service.isAllowed('/menus').subscribe();
    httpMock.expectOne(ROUTES_URL).flush(routes);

    service.clear();
    service.isAllowed('/menus').subscribe();
    httpMock.expectOne(ROUTES_URL).flush(routes);
  });

  it('falla abierto si la API da error (deja pasar)', () => {
    let result: boolean | undefined;
    service.isAllowed('/menus').subscribe((ok) => (result = ok));
    httpMock.expectOne(ROUTES_URL).flush('boom', { status: 500, statusText: 'Server Error' });
    expect(result).toBe(true);
  });

  it('dedupe: isAllowed y getAllowedPaths concurrentes hacen UNA sola llamada', () => {
    let allowed: boolean | undefined;
    let paths: ReadonlySet<string> | null | undefined;
    service.isAllowed('/menus').subscribe((ok) => (allowed = ok));
    service.getAllowedPaths().subscribe((p) => (paths = p));

    // expectOne falla si hubo más de un request: valida que el in-flight se comparte.
    httpMock.expectOne(ROUTES_URL).flush(routes);

    expect(allowed).toBe(true);
    expect(paths).toEqual(new Set(['/menus', '/tenants']));
  });

  it('getAllowedPaths excluye routePath nulos y reusa el cache de isAllowed', () => {
    service.isAllowed('/menus').subscribe();
    httpMock.expectOne(ROUTES_URL).flush(routes);

    let paths: ReadonlySet<string> | null | undefined;
    service.getAllowedPaths().subscribe((p) => (paths = p));
    httpMock.expectNone(ROUTES_URL);
    expect(paths).toEqual(new Set(['/menus', '/tenants']));
  });

  it('getAllowedPaths devuelve null si la API da error (lo decide el consumidor)', () => {
    let paths: ReadonlySet<string> | null | undefined;
    service.getAllowedPaths().subscribe((p) => (paths = p));
    httpMock.expectOne(ROUTES_URL).flush('boom', { status: 500, statusText: 'Server Error' });
    expect(paths).toBeNull();
  });

  it('no cachea el error: un reintento vuelve a pegarle a la API', () => {
    service.getAllowedPaths().subscribe();
    httpMock.expectOne(ROUTES_URL).flush('boom', { status: 500, statusText: 'Server Error' });

    let paths: ReadonlySet<string> | null | undefined;
    service.getAllowedPaths().subscribe((p) => (paths = p));
    httpMock.expectOne(ROUTES_URL).flush(routes);
    expect(paths).toEqual(new Set(['/menus', '/tenants']));
  });
});
