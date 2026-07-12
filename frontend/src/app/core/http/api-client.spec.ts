import { provideHttpClient, withXhr } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { AppConfig } from '../config/app-config.model';
import { AppConfigService } from '../config/app-config.service';
import { ApiClient } from './api-client';

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

describe('ApiClient', () => {
  let api: ApiClient;
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(withXhr()), provideHttpClientTesting()],
    });
    vi.spyOn(window, 'fetch').mockResolvedValue(
      new Response(JSON.stringify(sampleConfig), { status: 200 }),
    );
    await TestBed.inject(AppConfigService).load();
    api = TestBed.inject(ApiClient);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('GET arma la URL contra la base de la API', () => {
    api.get('general/Role').subscribe();
    const req = httpMock.expectOne('https://api.test.com/api/general/Role');
    expect(req.request.method).toBe('GET');
    req.flush({});
  });

  it('GET incluye los query params no vacíos', () => {
    api.get('general/Role', { page: 1, searchText: 'abc', orderBy: '' }).subscribe();
    const req = httpMock.expectOne((r) => r.url === 'https://api.test.com/api/general/Role');
    expect(req.request.params.get('page')).toBe('1');
    expect(req.request.params.get('searchText')).toBe('abc');
    expect(req.request.params.has('orderBy')).toBe(false);
    req.flush({});
  });

  it('POST envía el body', () => {
    const body = { id: 1, descripcion: 'x' };
    api.post('general/Role/update', body).subscribe();
    const req = httpMock.expectOne('https://api.test.com/api/general/Role/update');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(body);
    req.flush({});
  });

  it('DELETE arma la URL con id', () => {
    api.delete('general/Role/5').subscribe();
    const req = httpMock.expectOne('https://api.test.com/api/general/Role/5');
    expect(req.request.method).toBe('DELETE');
    req.flush(null);
  });

  it('postMultipart envía la FormData SIN fijar Content-Type (el browser pone el boundary)', () => {
    const form = new FormData();
    form.append('tableId', '5');
    api.postMultipart('budget/table-data/import/excel', form).subscribe();
    const req = httpMock.expectOne('https://api.test.com/api/budget/table-data/import/excel');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toBe(form);
    expect(req.request.headers.has('Content-Type')).toBe(false);
    req.flush({});
  });

  it('getBlob pide binario y devuelve la respuesta completa (headers incluidos)', () => {
    let status = 0;
    api.getBlob('budget/table-data/template', { tableId: 5 }).subscribe((r) => (status = r.status));
    const req = httpMock.expectOne(
      (r) => r.url === 'https://api.test.com/api/budget/table-data/template',
    );
    expect(req.request.responseType).toBe('blob');
    expect(req.request.params.get('tableId')).toBe('5');
    req.flush(new Blob(['x']));
    expect(status).toBe(200);
  });

  it('postBlob manda el body JSON y pide binario', () => {
    api.postBlob('budget/table-data/export/real', { modelId: 3 }).subscribe();
    const req = httpMock.expectOne('https://api.test.com/api/budget/table-data/export/real');
    expect(req.request.responseType).toBe('blob');
    expect(req.request.body).toEqual({ modelId: 3 });
    req.flush(new Blob(['x']));
  });
});
