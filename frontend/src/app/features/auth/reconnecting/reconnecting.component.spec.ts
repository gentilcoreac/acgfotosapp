import type { Mock } from 'vitest';
import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { ActivatedRoute, Router, convertToParamMap } from '@angular/router';
import { of, throwError } from 'rxjs';
import { AplicacionContextStore } from '../../../core/aplicacion-context';
import { AuthService, AuthStore } from '../../../core/auth';
import { AppConfigService } from '../../../core/config';
import { ReconnectingComponent } from './reconnecting.component';

// Debe coincidir con RETRY_MS del componente.
const RETRY_MS = 5000;
const MAX_ATTEMPTS = 14;

describe('ReconnectingComponent', () => {
  beforeEach(() => {
    vi.useFakeTimers({ advanceTimeDelta: 1, shouldAdvanceTime: true });
  });
  afterEach(() => {
    vi.useRealTimers();
  });
  let refreshSession: Mock;
  let load: Mock;
  let setReconnecting: Mock;
  let navigateByUrl: Mock;
  let isAuthenticated: Mock;
  let reconnectReason: Mock;

  function setup(returnUrl?: string): void {
    refreshSession = vi.fn().mockName('refreshSession');
    load = vi.fn().mockName('load').mockReturnValue(of([]));
    setReconnecting = vi.fn().mockName('setReconnecting');
    navigateByUrl = vi.fn().mockName('navigateByUrl');
    isAuthenticated = vi.fn().mockName('isAuthenticated').mockReturnValue(false);
    reconnectReason = vi.fn().mockName('reconnectReason').mockReturnValue('rate-limit');

    TestBed.configureTestingModule({
      imports: [ReconnectingComponent],
      providers: [
        provideNoopAnimations(),
        { provide: AuthService, useValue: { refreshSession } },
        { provide: AplicacionContextStore, useValue: { load } },
        { provide: AuthStore, useValue: { isAuthenticated, setReconnecting, reconnectReason } },
        { provide: Router, useValue: { navigateByUrl } },
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: { queryParamMap: convertToParamMap(returnUrl ? { returnUrl } : {}) },
          },
        },
        { provide: AppConfigService, useValue: { config: () => ({ appTitle: 'AcgFotos' }) } },
      ],
    });
  }

  function create(): void {
    const fixture = TestBed.createComponent(ReconnectingComponent);
    fixture.detectChanges(); // dispara ngOnInit
  }

  const tokenOk = () => of({ token: 't', validTo: new Date(Date.now() + 60000).toISOString() });
  const httpError = (status: number) => throwError(() => new HttpErrorResponse({ status }));

  it('reconecta al primer intento y vuelve a returnUrl', async () => {
    setup('/usuarios');
    refreshSession.mockReturnValue(tokenOk());
    create();
    await vi.advanceTimersByTimeAsync(0); // primer tick del timer(0)
    expect(load).toHaveBeenCalled();
    expect(setReconnecting).toHaveBeenCalledWith(false);
    expect(navigateByUrl).toHaveBeenCalledWith('/usuarios');
  });

  it('reintenta tras 429 y reconecta en el segundo intento', async () => {
    setup('/');
    refreshSession.mockReturnValueOnce(httpError(429)).mockReturnValueOnce(tokenOk());
    create();
    await vi.advanceTimersByTimeAsync(0); // 1er intento → 429 → retry
    expect(navigateByUrl).not.toHaveBeenCalled();
    await vi.advanceTimersByTimeAsync(RETRY_MS); // 2do intento → ok
    expect(navigateByUrl).toHaveBeenCalledWith('/');
  });

  it('va a /login si el refresh es rechazado de verdad (401)', async () => {
    setup('/usuarios');
    refreshSession.mockReturnValue(httpError(401));
    create();
    await vi.advanceTimersByTimeAsync(0);
    expect(setReconnecting).toHaveBeenCalledWith(false);
    expect(navigateByUrl).toHaveBeenCalledWith('/login');
  });

  it('va a /login al agotar los intentos (siempre 429)', async () => {
    setup('/usuarios');
    refreshSession.mockReturnValue(httpError(429));
    create();
    await vi.advanceTimersByTimeAsync(MAX_ATTEMPTS * RETRY_MS);
    expect(navigateByUrl).toHaveBeenCalledTimes(1);
    expect(navigateByUrl).toHaveBeenCalledWith('/login');
  });

  it('si ya hay sesión, vuelve directo sin reintentar', async () => {
    setup('/usuarios');
    isAuthenticated.mockReturnValue(true);
    create();
    await vi.advanceTimersByTimeAsync(0);
    expect(refreshSession).not.toHaveBeenCalled();
    expect(navigateByUrl).toHaveBeenCalledWith('/usuarios');
  });

  it('usa "/" como returnUrl si no viene en la query', async () => {
    setup();
    refreshSession.mockReturnValue(tokenOk());
    create();
    await vi.advanceTimersByTimeAsync(0);
    expect(navigateByUrl).toHaveBeenCalledWith('/');
  });

  it('muestra el mensaje de API caída cuando la causa es network (no "demasiadas solicitudes")', () => {
    setup('/usuarios');
    isAuthenticated.mockReturnValue(true); // evita disparar el bucle de reintentos
    reconnectReason.mockReturnValue('network');
    const fixture = TestBed.createComponent(ReconnectingComponent);
    fixture.detectChanges();
    const text = (fixture.nativeElement as HTMLElement).querySelector(
      '.reconnect__text',
    )?.textContent;
    expect(text).toContain('No pudimos conectar con el servidor');
    expect(text).not.toContain('Demasiadas solicitudes');
  });

  it('muestra el mensaje de rate limit cuando la causa es rate-limit', () => {
    setup('/usuarios');
    isAuthenticated.mockReturnValue(true);
    reconnectReason.mockReturnValue('rate-limit');
    const fixture = TestBed.createComponent(ReconnectingComponent);
    fixture.detectChanges();
    const text = (fixture.nativeElement as HTMLElement).querySelector(
      '.reconnect__text',
    )?.textContent;
    expect(text).toContain('Demasiadas solicitudes');
  });
});
