import type { MockedObject } from 'vitest';
import { ApplicationRef } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { AuthStore } from '../auth';
import { AppConfigService } from '../config';
import { TenantPublicStyle } from './tenant-public-style.model';
import { TenantStyleService } from './tenant-style.service';
import { TenantStyleStore } from './tenant-style.store';
import { TenantThemeCoordinator } from './tenant-theme.coordinator';

/** JWT de prueba (sin firma) con los claims dados. */
function fakeJwt(payload: Record<string, unknown>): string {
  const enc = (o: object) => btoa(JSON.stringify(o)).replace(/=/g, '');
  return `${enc({ alg: 'none' })}.${enc(payload)}.sig`;
}

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

describe('TenantThemeCoordinator', () => {
  let serviceSpy: MockedObject<TenantStyleService>;
  let auth: AuthStore;
  let store: TenantStyleStore;
  let appRef: ApplicationRef;

  beforeEach(() => {
    localStorage.removeItem('tbi.theme');
    serviceSpy = {
      getHeaderStyle: vi.fn().mockName('TenantStyleService.getHeaderStyle'),
      apply: vi.fn().mockName('TenantStyleService.apply'),
      reset: vi.fn().mockName('TenantStyleService.reset'),
    } as unknown as MockedObject<TenantStyleService>;
    serviceSpy.getHeaderStyle.mockReturnValue(of(makeStyle()));

    TestBed.configureTestingModule({
      providers: [
        { provide: TenantStyleService, useValue: serviceSpy },
        {
          provide: AppConfigService,
          useValue: { config: () => ({ defaultTheme: 'light', appTitle: 'AcgFotos' }) },
        },
      ],
    });
    TestBed.inject(TenantThemeCoordinator); // instancia → registra el effect
    auth = TestBed.inject(AuthStore);
    store = TestBed.inject(TenantStyleStore);
    appRef = TestBed.inject(ApplicationRef);
  });

  afterEach(() => {
    localStorage.removeItem('tbi.theme');
    document.documentElement.style.colorScheme = '';
  });

  it('al tener token con tenant, trae y aplica el estilo de ese tenant', () => {
    auth.setSession(fakeJwt({ tenant: '7' }), new Date(Date.now() + 60000));
    appRef.tick();
    expect(serviceSpy.getHeaderStyle).toHaveBeenCalledWith(7);
    expect(serviceSpy.apply).toHaveBeenCalled();
  });

  it('al impersonar (cambia el tenant del token) re-aplica el del tenant destino', () => {
    auth.setSession(fakeJwt({ tenant: '1' }), new Date(Date.now() + 60000));
    appRef.tick();
    auth.setSession(fakeJwt({ tenant: '4', impersonatedBy: '1' }), new Date(Date.now() + 60000));
    appRef.tick();
    expect(serviceSpy.getHeaderStyle).toHaveBeenCalledWith(4);
  });

  it('sin token (logout) y sin estilo anónimo, revierte al tema base (reset)', () => {
    auth.setSession(fakeJwt({ tenant: '7' }), new Date(Date.now() + 60000));
    appRef.tick();
    auth.clearSession();
    appRef.tick();
    expect(serviceSpy.reset).toHaveBeenCalled();
  });

  it('sin token pero con estilo anónimo, vuelve a aplicar ese estilo', () => {
    const anon = makeStyle({ codigo: 'dominio' });
    store.setAnonymous(anon);
    auth.clearSession();
    appRef.tick();
    expect(serviceSpy.apply).toHaveBeenCalledWith(anon);
    expect(serviceSpy.reset).not.toHaveBeenCalled();
  });
});
