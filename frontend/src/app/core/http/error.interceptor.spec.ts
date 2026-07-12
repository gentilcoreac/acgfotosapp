import type { MockedObject } from 'vitest';
import { HttpClient, provideHttpClient, withInterceptors, withXhr } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { NotificationService } from '../../shared/feedback/notification.service';
import { ApiError } from '../models/api-error.model';
import { errorInterceptor } from './error.interceptor';

const URL = 'https://api.test.com/api/general/Role';

describe('errorInterceptor', () => {
  let http: HttpClient;
  let httpMock: HttpTestingController;
  let router: Router;
  let notify: MockedObject<NotificationService>;

  beforeEach(() => {
    notify = {
      error: vi.fn().mockName('NotificationService.error'),
      success: vi.fn().mockName('NotificationService.success'),
    } as unknown as MockedObject<NotificationService>;
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        provideHttpClient(withXhr(), withInterceptors([errorInterceptor])),
        provideHttpClientTesting(),
        { provide: NotificationService, useValue: notify },
      ],
    });
    http = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);
    router = TestBed.inject(Router);
  });

  afterEach(() => httpMock.verify());

  it('normaliza la forma estándar { message, errors, traceId }', () => {
    let caught: ApiError | undefined;
    http.get(URL).subscribe({ error: (e: ApiError) => (caught = e) });
    httpMock.expectOne(URL).flush(
      {
        message: 'No se pudo guardar',
        errors: ['Email duplicado', 'Falta licencia'],
        traceId: 'abc:001',
      },
      { status: 400, statusText: 'Bad Request' },
    );
    expect(caught).toEqual({
      status: 400,
      message: 'No se pudo guardar',
      errors: ['Email duplicado', 'Falta licencia'],
      traceId: 'abc:001',
    });
  });

  it('normaliza un string[] legacy: el primero es el titular y el resto, detalle', () => {
    let caught: ApiError | undefined;
    http.get(URL).subscribe({ error: (e: ApiError) => (caught = e) });
    httpMock.expectOne(URL).flush(['ErrorA', 'ErrorB'], { status: 400, statusText: 'Bad Request' });
    expect(caught).toEqual({ status: 400, message: 'ErrorA', errors: ['ErrorB'] });
  });

  it('usa fallback cuando el 500 no trae cuerpo', () => {
    let caught: ApiError | undefined;
    http.get(URL).subscribe({ error: (e: ApiError) => (caught = e) });
    httpMock.expectOne(URL).flush(null, { status: 500, statusText: 'Server Error' });
    expect(caught?.message).toBe('ErrorInternalServer');
    expect(caught?.errors).toEqual([]);
  });

  it('mapea 429 (rate limit) a un mensaje claro de demasiadas solicitudes', () => {
    let caught: ApiError | undefined;
    http.get(URL).subscribe({ error: (e: ApiError) => (caught = e) });
    httpMock.expectOne(URL).flush(null, { status: 429, statusText: 'Too Many Requests' });
    expect(caught?.status).toBe(429);
    expect(caught?.message).toContain('Demasiadas solicitudes');
  });

  it('notifica el error con un toast global (excepto 404)', () => {
    http.get(URL).subscribe({ error: () => undefined });
    httpMock.expectOne(URL).flush(['Falló'], { status: 400, statusText: 'Bad Request' });
    expect(notify.error).toHaveBeenCalledWith('Falló', [], undefined);
  });

  it('no toastea el 404 (la página /404 ya comunica)', () => {
    http.get(URL).subscribe({ error: () => undefined });
    httpMock.expectOne(URL).flush(null, { status: 404, statusText: 'Not Found' });
    expect(notify.error).not.toHaveBeenCalled();
  });

  it('no toastea el 401 (lo maneja authInterceptor)', () => {
    http.get(URL).subscribe({ error: () => undefined });
    httpMock.expectOne(URL).flush(null, { status: 401, statusText: 'Unauthorized' });
    expect(notify.error).not.toHaveBeenCalled();
  });

  it('en 404 redirige a /404', () => {
    const navigateSpy = vi
      .spyOn(router, 'navigate')
      .mockReturnValue(undefined as unknown as Promise<boolean>);
    http.get(URL).subscribe({ error: () => undefined });
    httpMock.expectOne(URL).flush(null, { status: 404, statusText: 'Not Found' });
    expect(navigateSpy).toHaveBeenCalledWith(['/404']);
  });

  it('mapea status 0 (sin conexión) a ErrorConnectionTimeOut', () => {
    let caught: ApiError | undefined;
    http.get(URL).subscribe({ error: (e: ApiError) => (caught = e) });
    httpMock.expectOne(URL).error(new ProgressEvent('error'));
    expect(caught?.status).toBe(0);
    expect(caught?.message).toBe('ErrorConnectionTimeOut');
  });

  it('deja pasar el 401 sin transformarlo (lo maneja authInterceptor)', () => {
    const navigateSpy = vi
      .spyOn(router, 'navigate')
      .mockReturnValue(undefined as unknown as Promise<boolean>);
    let caught: unknown;
    http.get(URL).subscribe({ error: (e) => (caught = e) });
    httpMock.expectOne(URL).flush('no autorizado', { status: 401, statusText: 'Unauthorized' });
    expect(
      (
        caught as {
          status: number;
        }
      ).status,
    ).toBe(401);
    // No es ApiError: conserva la forma original (no se normalizó, no tiene 'errors').
    expect((caught as ApiError).errors).toBeUndefined();
    expect(navigateSpy).not.toHaveBeenCalled();
  });

  it('parsea un error serializado en Blob JSON', async () => {
    const blob = new Blob([JSON.stringify(['BlobMsg'])], { type: 'application/json' });
    // responseType 'blob' replica el caso real (descargas) donde el error viene en Blob.
    const caught = new Promise<ApiError>((resolve) => {
      http.get(URL, { responseType: 'blob' }).subscribe({ error: (e: ApiError) => resolve(e) });
    });
    httpMock.expectOne(URL).flush(blob, { status: 400, statusText: 'Bad Request' });
    expect(await caught).toEqual({ status: 400, message: 'BlobMsg', errors: [] });
  });
});
