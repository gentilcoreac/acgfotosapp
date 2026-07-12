import {
  EnvironmentProviders,
  inject,
  makeEnvironmentProviders,
  provideAppInitializer,
} from '@angular/core';
import { AppConfigService } from './app-config.service';

/**
 * Registra la carga de la configuración runtime antes del bootstrap.
 *
 * Usa `provideAppInitializer` (API de Angular 20) en lugar del token
 * `APP_INITIALIZER` del original. Agregar a los providers de `appConfig`.
 */
export function provideAppConfig(): EnvironmentProviders {
  return makeEnvironmentProviders([provideAppInitializer(() => inject(AppConfigService).load())]);
}
