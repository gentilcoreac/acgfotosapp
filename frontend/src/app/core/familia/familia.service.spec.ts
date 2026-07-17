import { provideHttpClient, withXhr } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { AppConfig } from '../config/app-config.model';
import { AppConfigService } from '../config/app-config.service';
import { FamiliaService } from './familia.service';
import { FamiliaSessionStore } from './familia-session.store';
import { CanjeResult } from './models/canje-result.model';

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

function fakeResult(): CanjeResult {
  return {
    token: 'tok',
    validTo: new Date(Date.now() + 30 * 60000).toISOString(),
    eventoId: 1,
    nombreEvento: 'Egresados 2026',
    participantes: [{ id: 100, nombre: 'Ana Pérez' }],
  };
}

describe('FamiliaService', () => {
  let service: FamiliaService;
  let store: FamiliaSessionStore;
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    sessionStorage.clear();
    TestBed.configureTestingModule({
      providers: [provideHttpClient(withXhr()), provideHttpClientTesting()],
    });
    vi.spyOn(window, 'fetch').mockResolvedValue(
      new Response(JSON.stringify(sampleConfig), { status: 200 }),
    );
    await TestBed.inject(AppConfigService).load();
    service = TestBed.inject(FamiliaService);
    store = TestBed.inject(FamiliaSessionStore);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
    sessionStorage.clear();
  });

  it('canjear pega a fotos/canje con el código y guarda la sesión', () => {
    service.canjear('K7F3-9QMD').subscribe();

    const req = httpMock.expectOne((r) => r.url.endsWith('/api/fotos/canje'));
    expect(req.request.body).toEqual({ codigo: 'K7F3-9QMD' });
    req.flush(fakeResult());

    expect(store.token()).toBe('tok');
    expect(store.isActive()).toBe(true);
  });

  it('un código inválido propaga el error sin tocar la sesión', () => {
    let error: unknown;
    service.canjear('ZZZZ-9999').subscribe({ error: (e) => (error = e) });

    const req = httpMock.expectOne((r) => r.url.endsWith('/api/fotos/canje'));
    req.flush({ message: 'Código inválido o vencido.', errors: [] }, { status: 400, statusText: 'Bad Request' });

    expect(error).toBeTruthy();
    expect(store.isActive()).toBe(false);
  });
});
