import { provideHttpClient, withXhr } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { AppConfig } from '../config/app-config.model';
import { AppConfigService } from '../config/app-config.service';
import { FamiliaGaleriaService } from './familia-galeria.service';
import { FotoFamilia } from './models/foto-familia.model';

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

describe('FamiliaGaleriaService', () => {
  let service: FamiliaGaleriaService;
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(withXhr()), provideHttpClientTesting()],
    });
    vi.spyOn(window, 'fetch').mockResolvedValue(
      new Response(JSON.stringify(sampleConfig), { status: 200 }),
    );
    await TestBed.inject(AppConfigService).load();
    service = TestBed.inject(FamiliaGaleriaService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('listar pega a fotos/familia/fotos sin filtros (el alcance sale del JWT)', () => {
    const fotos: FotoFamilia[] = [{ id: 1, grupoId: 3, participanteId: 100, ancho: 800, alto: 600 }];
    service.listar().subscribe();

    const req = httpMock.expectOne((r) => r.url.endsWith('/api/fotos/familia/fotos'));
    expect(req.request.method).toBe('GET');
    req.flush(fotos);
  });

  it('derivado baja el blob del thumb/preview de una foto', () => {
    let recibido: Blob | undefined;
    service.derivado(1, 'preview').subscribe((blob) => (recibido = blob));

    const req = httpMock.expectOne((r) => r.url.endsWith('/api/fotos/familia/fotos/1/preview'));
    req.flush(new Blob(['x']));

    expect(recibido).toBeTruthy();
  });
});
