import { Injectable, Signal, computed, signal } from '@angular/core';
import { AppConfig } from './app-config.model';

/** Ruta (relativa al base href) del archivo de configuración runtime. */
const CONFIG_URL = 'configurations/app.config.json';

/**
 * Carga y expone la configuración runtime de la app.
 *
 * Se inicializa una sola vez en el bootstrap (ver `provideAppConfig`) leyendo
 * `app.config.json`. La config queda en un signal de solo lectura; **no** se
 * muta ningún objeto global (a diferencia del `environment` del original).
 *
 * Pedir los valores a este servicio — nunca leer `environment` directamente.
 */
@Injectable({ providedIn: 'root' })
export class AppConfigService {
  private readonly _config = signal<AppConfig | null>(null);

  /**
   * Config cargada. Lanza si se lee antes del bootstrap (no debería pasar:
   * `provideAppConfig` la resuelve vía `provideAppInitializer`).
   */
  readonly config: Signal<AppConfig> = computed(() => {
    const config = this._config();
    if (!config) {
      throw new Error('AppConfig no inicializada. ¿Falta provideAppConfig() en el bootstrap?');
    }
    return config;
  });

  private loadPromise?: Promise<void>;

  /**
   * Carga la config desde el archivo del ambiente. Idempotente: múltiples llamadas
   * (p. ej. `provideAppConfig` + el restore de sesión en bootstrap) comparten un único
   * fetch. Si falla, resetea para permitir reintentar.
   */
  load(): Promise<void> {
    this.loadPromise ??= this.fetchConfig().catch((error) => {
      this.loadPromise = undefined;
      throw error;
    });
    return this.loadPromise;
  }

  private async fetchConfig(): Promise<void> {
    const response = await fetch(CONFIG_URL, { cache: 'no-cache' });
    if (!response.ok) {
      throw new Error(
        `No se pudo cargar la configuración (${response.status}) desde ${CONFIG_URL}`,
      );
    }
    this._config.set((await response.json()) as AppConfig);
  }

  // #region URL builders
  /** URL base de la API sin barra final. */
  private get baseUrl(): string {
    return this.config().apiUrl.replace(/\/+$/, '');
  }

  /** Construye una URL de API: `{base}/api/{path}`. */
  buildApiUrl(path = ''): string {
    const segment = path.replace(/^\/+/, '');
    return segment ? `${this.baseUrl}/api/${segment}` : `${this.baseUrl}/api`;
  }

  /** Construye una URL de módulo: `{base}/api/{module}/{entity}` (módulo por defecto `general`). */
  buildModuleApiUrl(entity = '', module = 'general'): string {
    const segment = entity.replace(/^\/+/, '');
    const moduleUrl = `${this.baseUrl}/api/${module}`;
    return segment ? `${moduleUrl}/${segment}` : moduleUrl;
  }
  // #endregion
}
