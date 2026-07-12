import type { MockedObject } from 'vitest';
import { DOCUMENT } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { AppConfigService } from '../config';
import { ApiClient } from '../http';
import { TenantPublicStyle } from './tenant-public-style.model';
import { TenantStyleService } from './tenant-style.service';

interface FakeDoc {
  documentElement: HTMLElement;
  head: HTMLHeadElement;
  title: string;
  createElement: (tag: string) => HTMLElement;
  defaultView: {
    location: {
      search: string;
      hostname: string;
    };
  };
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

describe('TenantStyleService', () => {
  let service: TenantStyleService;
  let apiSpy: MockedObject<ApiClient>;
  let fakeDoc: FakeDoc;
  let defaultTenant: string;

  beforeEach(() => {
    defaultTenant = '';
    apiSpy = {
      get: vi.fn().mockName('ApiClient.get'),
    } as unknown as MockedObject<ApiClient>;
    fakeDoc = {
      documentElement: document.createElement('html'),
      head: document.createElement('head'),
      title: '',
      createElement: (tag: string) => document.createElement(tag),
      defaultView: { location: { search: '', hostname: 'app.acme.com' } },
    };

    TestBed.configureTestingModule({
      providers: [
        { provide: ApiClient, useValue: apiSpy },
        { provide: DOCUMENT, useValue: fakeDoc as unknown as Document },
        {
          provide: AppConfigService,
          useValue: {
            config: () => ({ defaultTenant, appTitle: 'AcgFotos', apiUrl: 'https://api.test.com' }),
          },
        },
      ],
    });
    service = TestBed.inject(TenantStyleService);
  });

  it('getPublicStyle pega a la URL correcta (encodeURIComponent)', () => {
    apiSpy.get.mockReturnValue(of(makeStyle()));
    service.getPublicStyle('a b').subscribe();
    expect(apiSpy.get).toHaveBeenCalledWith('general/tenants/public-style/a%20b');
  });

  it('getHeaderStyle pega a header-style/{id}', () => {
    apiSpy.get.mockReturnValue(of(makeStyle()));
    service.getHeaderStyle(7).subscribe();
    expect(apiSpy.get).toHaveBeenCalledWith('general/tenants/header-style/7');
  });

  describe('resolveIdentifier', () => {
    it('prioriza ?tenant= sobre todo', () => {
      fakeDoc.defaultView.location.search = '?tenant=qa';
      defaultTenant = 'acme';
      expect(service.resolveIdentifier()).toBe('qa');
    });

    it('usa defaultTenant cuando no hay query', () => {
      defaultTenant = 'acme';
      expect(service.resolveIdentifier()).toBe('acme');
    });

    it('cae al hostname cuando no hay query ni defaultTenant', () => {
      defaultTenant = '';
      expect(service.resolveIdentifier()).toBe('app.acme.com');
    });
  });

  describe('apply', () => {
    it('setea colores (--mat-sys-*), favicon y título', () => {
      service.apply(
        makeStyle({
          colorPrimarioLight: '#C0392B',
          colorPrimarioDark: '#E74C3C',
          faviconUrl: 'http://h/f.png',
          tituloWeb: 'ACME BI',
        }),
      );
      expect(fakeDoc.documentElement.style.getPropertyValue('--mat-sys-primary')).toContain(
        'light-dark(',
      );
      expect(fakeDoc.head.querySelector('link[rel~="icon"]')?.getAttribute('href')).toBe(
        'http://h/f.png',
      );
      expect(fakeDoc.title).toBe('ACME BI');
    });

    it('sin colores no setea variables; sin tituloWeb cae al appTitle', () => {
      service.apply(makeStyle({ tituloWeb: null }));
      expect(fakeDoc.documentElement.style.getPropertyValue('--mat-sys-primary')).toBe('');
      expect(fakeDoc.title).toBe('AcgFotos');
    });
  });

  it('reset quita los overrides --mat-sys-* y restablece el título', () => {
    service.apply(
      makeStyle({ colorPrimarioLight: '#C0392B', colorPrimarioDark: '#E74C3C', tituloWeb: 'ACME' }),
    );
    expect(fakeDoc.documentElement.style.getPropertyValue('--mat-sys-primary')).toContain(
      'light-dark(',
    );

    service.reset();
    expect(fakeDoc.documentElement.style.getPropertyValue('--mat-sys-primary')).toBe('');
    expect(fakeDoc.title).toBe('AcgFotos');
  });

  describe('styleSheetCssUrl (CSS custom, guarda same-origin)', () => {
    const cssLink = () => fakeDoc.head.querySelector('#tbi-tenant-css');

    it('inyecta el <link> si la URL es del mismo origen que la API', () => {
      service.apply(makeStyle({ styleSheetCssUrl: 'https://api.test.com/t/x.css' }));
      expect(cssLink()?.getAttribute('href')).toBe('https://api.test.com/t/x.css');
    });

    it('NO inyecta el <link> si la URL es de otro origen', () => {
      service.apply(makeStyle({ styleSheetCssUrl: 'https://evil.com/x.css' }));
      expect(cssLink()).toBeNull();
    });

    it('reset quita el <link> del CSS custom', () => {
      service.apply(makeStyle({ styleSheetCssUrl: 'https://api.test.com/t/x.css' }));
      expect(cssLink()).toBeTruthy();
      service.reset();
      expect(cssLink()).toBeNull();
    });
  });
});
