import { TestBed } from '@angular/core/testing';
import { AppConfigService } from '../config';
import { ThemeStore } from '../theme';
import { TenantPublicStyle } from './tenant-public-style.model';
import { TenantStyleStore } from './tenant-style.store';

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

describe('TenantStyleStore', () => {
  let store: TenantStyleStore;
  let theme: ThemeStore;

  beforeEach(() => {
    localStorage.removeItem('tbi.theme');
    TestBed.configureTestingModule({
      providers: [
        { provide: AppConfigService, useValue: { config: () => ({ defaultTheme: 'light' }) } },
      ],
    });
    store = TestBed.inject(TenantStyleStore);
    theme = TestBed.inject(ThemeStore);
  });

  afterEach(() => {
    localStorage.removeItem('tbi.theme');
    document.documentElement.style.colorScheme = '';
  });

  it('headerLogoUrl es null sin estilo cargado', () => {
    expect(store.headerLogoUrl()).toBeNull();
  });

  it('headerLogoUrl sigue el modo (light → LightUrl, dark → DarkUrl)', () => {
    store.set(makeStyle({ logoHeaderLightUrl: 'L.png', logoHeaderDarkUrl: 'D.png' }));
    theme.setMode('light');
    expect(store.headerLogoUrl()).toBe('L.png');
    theme.setMode('dark');
    expect(store.headerLogoUrl()).toBe('D.png');
  });

  it('headerLogoUrl es null si el tenant no tiene logo de header', () => {
    store.set(makeStyle({ logoHeaderLightUrl: null, logoHeaderDarkUrl: null }));
    theme.setMode('light');
    expect(store.headerLogoUrl()).toBeNull();
  });
});
