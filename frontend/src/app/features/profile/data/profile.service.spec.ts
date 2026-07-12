import { provideHttpClient, withXhr } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { AppConfig } from '../../../core/config/app-config.model';
import { AppConfigService } from '../../../core/config/app-config.service';
import { ProfileService } from './profile.service';
import { ChangePassword, Perfil } from '../domain/profile.model';

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

const BASE = 'https://api.test.com/api';

describe('ProfileService', () => {
  let service: ProfileService;
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(withXhr()), provideHttpClientTesting()],
    });
    vi.spyOn(window, 'fetch').mockResolvedValue(
      new Response(JSON.stringify(sampleConfig), { status: 200 }),
    );
    await TestBed.inject(AppConfigService).load();
    service = TestBed.inject(ProfileService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('getPerfil pega a general/usuarios/mi-perfil', () => {
    let result: Perfil | undefined;
    service.getPerfil().subscribe((p) => (result = p));
    const req = httpMock.expectOne(`${BASE}/general/usuarios/mi-perfil`);
    expect(req.request.method).toBe('GET');
    req.flush({ userName: 'jdoe', nombre: 'Juan' } as Perfil);
    expect(result?.userName).toBe('jdoe');
  });

  it('updateProfile postea a general/usuarios/update-profile con el perfil', () => {
    const perfil = { userName: 'jdoe', nombre: 'Juan', apellido: 'Doe' } as Perfil;
    service.updateProfile(perfil).subscribe();
    const req = httpMock.expectOne(`${BASE}/general/usuarios/update-profile`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(perfil);
    req.flush({});
  });

  it('changePassword postea a auth/cambiar-password', () => {
    const model: ChangePassword = {
      currentPassword: 'a',
      newPassword: 'b',
      newConfirmPassword: 'b',
    };
    service.changePassword(model).subscribe();
    const req = httpMock.expectOne(`${BASE}/auth/cambiar-password`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(model);
    req.flush({});
  });
});
