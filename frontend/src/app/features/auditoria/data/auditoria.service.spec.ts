import { provideHttpClient, withXhr } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { AppConfig } from '../../../core/config/app-config.model';
import { AppConfigService } from '../../../core/config/app-config.service';
import { AuditoriaService } from './auditoria.service';
import { Auditoria } from '../domain/auditoria.model';

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

describe('AuditoriaService', () => {
  let service: AuditoriaService;
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(withXhr()), provideHttpClientTesting()],
    });
    vi.spyOn(window, 'fetch').mockResolvedValue(
      new Response(JSON.stringify(sampleConfig), { status: 200 }),
    );
    await TestBed.inject(AppConfigService).load();
    service = TestBed.inject(AuditoriaService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('getById pega a general/auditoria/{id}', () => {
    let result: Auditoria | undefined;
    service.getById(7).subscribe((r) => (result = r));
    const req = httpMock.expectOne('https://api.test.com/api/general/auditoria/7');
    expect(req.request.method).toBe('GET');
    req.flush({ id: 7, servicio: 'Foo' } as Auditoria);
    expect(result?.id).toBe(7);
  });

  it('getAllByCriteria pega al listado con los filtros como query params', () => {
    service.crud.getAllByCriteria({ page: 0, pageSize: 10, servicio: 'Foo' }).subscribe();
    const req = httpMock.expectOne(
      (r) =>
        r.url === 'https://api.test.com/api/general/auditoria' &&
        r.params.get('servicio') === 'Foo',
    );
    expect(req.request.method).toBe('GET');
    req.flush({ items: [], totalCount: 0 });
  });
});
