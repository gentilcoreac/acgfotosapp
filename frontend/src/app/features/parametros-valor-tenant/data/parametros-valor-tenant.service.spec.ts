import { provideHttpClient, withXhr } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { AppConfig } from '../../../core/config/app-config.model';
import { AppConfigService } from '../../../core/config/app-config.service';
import { ParametrosValorTenantService } from './parametros-valor-tenant.service';

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

describe('ParametrosValorTenantService', () => {
  let service: ParametrosValorTenantService;
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(withXhr()), provideHttpClientTesting()],
    });
    vi.spyOn(window, 'fetch').mockResolvedValue(
      new Response(JSON.stringify(sampleConfig), { status: 200 }),
    );
    await TestBed.inject(AppConfigService).load();
    service = TestBed.inject(ParametrosValorTenantService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('getTenants pide el listado de tenants y descarta los que no tienen id', () => {
    let result: {
      id?: number;
    }[] = [];
    service.getTenants().subscribe((t) => (result = t));
    const req = httpMock.expectOne((r) => r.url === 'https://api.test.com/api/general/tenants');
    expect(req.request.method).toBe('GET');
    req.flush({
      items: [
        { id: 4, codigo: 'QA', nombre: 'QA Tenant' },
        { id: null, codigo: 'X', nombre: 'sin id' },
      ],
      totalCount: 2,
    });
    expect(result.length).toBe(1);
    expect(result[0].id).toBe(4);
  });

  it('getAplicacionesPorTenant pega al endpoint root por tenant', () => {
    service.getAplicacionesPorTenant(4).subscribe();
    const req = httpMock.expectOne(
      'https://api.test.com/api/general/aplicaciones/aplicaciones-tenant-id/4',
    );
    expect(req.request.method).toBe('GET');
    req.flush([{ id: 1, nombre: 'General' }]);
  });

  it('getParametros manda tenantId y aplicacionId como query params', () => {
    service.getParametros(4, 1).subscribe();
    const req = httpMock.expectOne(
      (r) =>
        r.url === 'https://api.test.com/api/general/parametros/parametros-por-tenant-aplicacion',
    );
    expect(req.request.method).toBe('GET');
    expect(req.request.params.get('tenantId')).toBe('4');
    expect(req.request.params.get('aplicacionId')).toBe('1');
    req.flush([]);
  });

  it('crud.save hace upsert (POST .../parametros-valores/update)', () => {
    service.crud.save({ tenantId: 4, parametroId: 19, valor: '25' }).subscribe();
    const req = httpMock.expectOne('https://api.test.com/api/general/parametros-valores/update');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ tenantId: 4, parametroId: 19, valor: '25' });
    req.flush({ id: 7, tenantId: 4, parametroId: 19, valor: '25' });
  });

  it('crud.delete resetea el override (DELETE .../parametros-valores/{id})', () => {
    service.crud.delete(7).subscribe();
    const req = httpMock.expectOne('https://api.test.com/api/general/parametros-valores/7');
    expect(req.request.method).toBe('DELETE');
    req.flush(null);
  });
});
