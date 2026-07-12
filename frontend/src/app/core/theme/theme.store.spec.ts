import { DOCUMENT } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { AppConfigService } from '../config';
import { ThemeStore } from './theme.store';

const STORAGE_KEY = 'tbi.theme';
const TRANSITION_CLASS = 'tbi-theme-transition';

describe('ThemeStore', () => {
  beforeEach(() => {
    vi.useFakeTimers({ advanceTimeDelta: 1, shouldAdvanceTime: true });
  });
  afterEach(() => {
    vi.useRealTimers();
  });
  let store: ThemeStore;
  let doc: Document;
  /** `defaultTheme` que devuelve el stub de config (mutable por test; el store lo lee en `initialize`). */
  let configDefaultTheme: string;

  beforeEach(() => {
    configDefaultTheme = 'light';
    localStorage.removeItem(STORAGE_KEY);

    TestBed.configureTestingModule({
      providers: [
        {
          provide: AppConfigService,
          useValue: { config: () => ({ defaultTheme: configDefaultTheme }) },
        },
      ],
    });
    store = TestBed.inject(ThemeStore);
    doc = TestBed.inject(DOCUMENT);
  });

  afterEach(() => {
    localStorage.removeItem(STORAGE_KEY);
    doc.documentElement.style.colorScheme = '';
    doc.documentElement.classList.remove(TRANSITION_CLASS);
  });

  it('arranca en light (antes de initialize)', () => {
    expect(store.mode()).toBe('light');
    expect(store.isDark()).toBe(false);
  });

  it('setMode aplica el color-scheme en <html>, persiste y actualiza las señales', () => {
    store.setMode('dark');
    expect(store.mode()).toBe('dark');
    expect(store.isDark()).toBe(true);
    expect(doc.documentElement.style.colorScheme).toBe('dark');
    expect(localStorage.getItem(STORAGE_KEY)).toBe('dark');
  });

  it('toggle alterna entre light y dark', () => {
    store.setMode('light');
    store.toggle();
    expect(store.mode()).toBe('dark');
    store.toggle();
    expect(store.mode()).toBe('light');
  });

  it('setMode activa la transición y la desactiva al terminar', async () => {
    store.setMode('dark');
    expect(doc.documentElement.classList.contains(TRANSITION_CLASS)).toBe(true);
    await vi.advanceTimersByTimeAsync(400);
    expect(doc.documentElement.classList.contains(TRANSITION_CLASS)).toBe(false);
  });

  it('initialize NO activa la transición (sin flash en el primer render)', () => {
    store.initialize(null);
    expect(doc.documentElement.classList.contains(TRANSITION_CLASS)).toBe(false);
  });

  it('initialize prioriza localStorage sobre el resto', () => {
    localStorage.setItem(STORAGE_KEY, 'light');
    configDefaultTheme = 'dark';
    store.initialize('dark');
    expect(store.mode()).toBe('light');
    expect(doc.documentElement.style.colorScheme).toBe('light');
  });

  it('initialize usa externalDefault (tenant) cuando no hay nada guardado', () => {
    configDefaultTheme = 'light';
    store.initialize('dark');
    expect(store.mode()).toBe('dark');
  });

  it('initialize cae a AppConfig.defaultTheme si no hay storage ni externalDefault', () => {
    configDefaultTheme = 'dark';
    store.initialize(null);
    expect(store.mode()).toBe('dark');
  });

  it('initialize no persiste (no congela el default de tenant/SO)', () => {
    configDefaultTheme = 'dark';
    store.initialize(null);
    expect(localStorage.getItem(STORAGE_KEY)).toBeNull();
  });

  it('initialize cae a la preferencia del SO cuando defaultTheme no es válido', () => {
    configDefaultTheme = 'auto'; // valor inválido → se normaliza a null
    vi.spyOn(window, 'matchMedia').mockReturnValue({ matches: true } as MediaQueryList);
    store.initialize(null);
    expect(store.mode()).toBe('dark');
  });
});
