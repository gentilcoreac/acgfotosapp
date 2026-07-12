import { TestBed } from '@angular/core/testing';
import { AppConfig } from './app-config.model';
import { AppConfigService } from './app-config.service';

const sampleConfig: AppConfig = {
  appTitle: 'AcgFotos',
  year: '2026',
  version: '0.0.1',
  production: false,
  apiUrl: 'https://api.test.com/', // con barra final a propósito, para validar el trim
  inactivity: { maxTimeMs: 7200000, maxResponseTimeMs: 60000 },
  pageSize: 100,
  pageSizeOptions: [5, 10, 25, 50, 100],
  defaultTenant: '',
  defaultTheme: 'light',
  phoneMinLength: 10,
  phoneMaxLength: 13,
  oldSystemUrl: '',
};

function mockFetch(body: unknown, status = 200): void {
  vi.spyOn(window, 'fetch').mockResolvedValue(
    new Response(JSON.stringify(body), { status, headers: { 'Content-Type': 'application/json' } }),
  );
}

describe('AppConfigService', () => {
  let service: AppConfigService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(AppConfigService);
  });

  it('lanza si se lee la config antes de cargarla', () => {
    expect(() => service.config()).toThrowError(/no inicializada/i);
  });

  it('carga la config desde el archivo', async () => {
    mockFetch(sampleConfig);
    await service.load();
    expect(service.config().apiUrl).toBe('https://api.test.com/');
    expect(service.config().pageSizeOptions).toEqual([5, 10, 25, 50, 100]);
  });

  it('rechaza si el fetch responde con error', async () => {
    mockFetch('', 500);
    await expect(service.load()).rejects.toThrowError(/no se pudo cargar/i);
  });

  describe('URL builders', () => {
    beforeEach(async () => {
      mockFetch(sampleConfig);
      await service.load();
    });

    it('buildApiUrl arma {base}/api/{path} sin doble barra', () => {
      expect(service.buildApiUrl('/auth/token')).toBe('https://api.test.com/api/auth/token');
      expect(service.buildApiUrl('auth/token')).toBe('https://api.test.com/api/auth/token');
      expect(service.buildApiUrl()).toBe('https://api.test.com/api');
    });

    it('buildModuleApiUrl usa el módulo general por defecto y permite override', () => {
      expect(service.buildModuleApiUrl('roles')).toBe('https://api.test.com/api/general/roles');
      expect(service.buildModuleApiUrl('roles', 'budget')).toBe(
        'https://api.test.com/api/budget/roles',
      );
      expect(service.buildModuleApiUrl()).toBe('https://api.test.com/api/general');
    });
  });
});
