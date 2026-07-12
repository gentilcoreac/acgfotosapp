import type { MockedObject } from 'vitest';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { AuthService } from '../../../core/auth';
import { AppConfigService } from '../../../core/config';
import { ApiError } from '../../../core/models';
import { TenantPublicStyle, TenantStyleStore } from '../../../core/tenant-style';
import { ThemeStore } from '../../../core/theme';
import { LoginComponent } from './login.component';

function makeStyle(over: Partial<TenantPublicStyle> = {}): TenantPublicStyle {
  return {
    codigo: 'acme',
    tituloWeb: null,
    hostName: null,
    colorPrimarioDark: null,
    colorPrimarioLight: null,
    darkModeByDefault: false,
    logoLoginLightUrl: null,
    logoLoginDarkUrl: null,
    logoHeaderLightUrl: null,
    logoHeaderDarkUrl: null,
    faviconUrl: null,
    imagenFondoLoginLightUrl: null,
    imagenFondoLoginDarkUrl: null,
    styleSheetCssUrl: null,
    tipoLayoutLogin: 0,
    ...over,
  };
}

describe('LoginComponent', () => {
  let authSpy: MockedObject<AuthService>;
  let router: Router;

  beforeEach(async () => {
    authSpy = {
      login: vi.fn().mockName('AuthService.login'),
    } as unknown as MockedObject<AuthService>;
    await TestBed.configureTestingModule({
      imports: [LoginComponent],
      providers: [
        provideRouter([]),
        provideNoopAnimations(),
        { provide: AuthService, useValue: authSpy },
        {
          provide: AppConfigService,
          useValue: { config: () => ({ appTitle: 'AcgFotos', version: '0.0.1', year: '2026' }) },
        },
      ],
    }).compileComponents();
    router = TestBed.inject(Router);
  });

  afterEach(() => {
    localStorage.removeItem('tbi.theme');
    document.documentElement.style.colorScheme = '';
    document.documentElement.classList.remove('tbi-theme-transition');
  });

  function create(): LoginComponent {
    const fixture = TestBed.createComponent(LoginComponent);
    fixture.detectChanges();
    return fixture.componentInstance;
  }

  it('no llama a login con el form inválido', () => {
    const cmp = create();
    cmp.submit();
    expect(authSpy.login).not.toHaveBeenCalled();
  });

  it('login OK navega a /', () => {
    const navSpy = vi
      .spyOn(router, 'navigateByUrl')
      .mockReturnValue(undefined as unknown as Promise<boolean>);
    authSpy.login.mockReturnValue(
      of({ token: 't', validTo: 'x', tenantId: 1, hasVerifiedEmail: true }),
    );
    const cmp = create();
    cmp.form.setValue({ userName: 'root', password: 'pass' });
    cmp.submit();
    expect(authSpy.login).toHaveBeenCalledWith({ userName: 'root', password: 'pass' });
    expect(navSpy).toHaveBeenCalledWith('/');
  });

  it('ante error muestra los mensajes del ApiError', () => {
    const apiError: ApiError = { status: 400, message: 'ErrorCredentialsInvalid', errors: [] };
    authSpy.login.mockReturnValue(throwError(() => apiError));
    const cmp = create();
    cmp.form.setValue({ userName: 'root', password: 'bad' });
    cmp.submit();
    expect(cmp.errors()).toEqual(['ErrorCredentialsInvalid']);
    expect(cmp.loading()).toBe(false);
  });

  it('sin tenant usa el layout centrado y el brand de texto', () => {
    const cmp = create();
    expect(cmp.layout()).toBe(0);
    expect(cmp.isSplit()).toBe(false);
    expect(cmp.loginLogoUrl()).toBeNull();
  });

  it('tipoLayoutLogin=1 activa el split y renderiza el pane de imagen', () => {
    TestBed.inject(TenantStyleStore).set(
      makeStyle({ tipoLayoutLogin: 1, imagenFondoLoginLightUrl: 'bg.png' }),
    );
    const fixture = TestBed.createComponent(LoginComponent);
    fixture.detectChanges();
    const el = fixture.nativeElement as HTMLElement;
    expect(el.querySelector('.login--split')).toBeTruthy();
    expect(el.querySelector('.login__image img')).toBeTruthy();
  });

  it('loginLogoUrl sigue el modo light/dark', () => {
    TestBed.inject(TenantStyleStore).set(
      makeStyle({ logoLoginLightUrl: 'L.png', logoLoginDarkUrl: 'D.png' }),
    );
    const theme = TestBed.inject(ThemeStore);
    const cmp = create();
    expect(cmp.loginLogoUrl()).toBe('L.png');
    theme.setMode('dark');
    expect(cmp.loginLogoUrl()).toBe('D.png');
  });
});
