import { provideHttpClient, withXhr } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { AppConfig } from '../config/app-config.model';
import { AppConfigService } from '../config/app-config.service';
import { ApiClient } from './api-client';
import { CrudClient } from './crud-client';

interface Rol {
  id: number;
  descripcion: string;
}

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

describe('CrudClient', () => {
  let crud: CrudClient<Rol>;
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(withXhr()), provideHttpClientTesting()],
    });
    vi.spyOn(window, 'fetch').mockResolvedValue(
      new Response(JSON.stringify(sampleConfig), { status: 200 }),
    );
    await TestBed.inject(AppConfigService).load();
    crud = new CrudClient<Rol>(TestBed.inject(ApiClient), 'Role');
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('getAllByCriteria pega a /api/general/Role', () => {
    crud.getAllByCriteria({ page: 0 }).subscribe();
    const req = httpMock.expectOne((r) => r.url === 'https://api.test.com/api/general/Role');
    expect(req.request.method).toBe('GET');
    req.flush({ items: [], totalCount: 0 });
  });

  it('getById agrega el id al path', () => {
    crud.getById(7).subscribe();
    const req = httpMock.expectOne('https://api.test.com/api/general/Role/7');
    expect(req.request.method).toBe('GET');
    req.flush({ id: 7, descripcion: 'admin' });
  });

  it('save hace POST a /update', () => {
    crud.save({ id: 0, descripcion: 'nuevo' }).subscribe();
    const req = httpMock.expectOne('https://api.test.com/api/general/Role/update');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ id: 0, descripcion: 'nuevo' });
    req.flush({ id: 1, descripcion: 'nuevo' });
  });

  it('delete pega a /api/general/Role/{id}', () => {
    crud.delete(3).subscribe();
    const req = httpMock.expectOne('https://api.test.com/api/general/Role/3');
    expect(req.request.method).toBe('DELETE');
    req.flush(null);
  });
});
