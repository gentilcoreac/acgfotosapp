import {
  EnvironmentProviders,
  inject,
  makeEnvironmentProviders,
  provideAppInitializer,
  provideEnvironmentInitializer,
} from '@angular/core';
import { catchError, firstValueFrom, of } from 'rxjs';
import { AppConfigService } from '../config';
import { ThemeStore } from '../theme';
import { TenantStyleService } from './tenant-style.service';
import { TenantStyleStore } from './tenant-style.store';
import { TenantThemeCoordinator } from './tenant-theme.coordinator';

/**
 * En el arranque: carga la config, resuelve el tenant y trae su estilo público (anónimo, tolerante a
 * fallo), aplica colores/favicon/título y deja el logo en el store. Luego inicializa el `ThemeStore`
 * con el `darkModeByDefault` del tenant como semilla (localStorage del usuario sigue teniendo prioridad).
 *
 * **Reemplaza a `provideThemeInit`**: la inicialización del modo de color vive acá para poder usar el
 * default del tenant. Si no hay tenant / falla el fetch, el tema cae a `AppConfig.defaultTheme`/SO.
 *
 * Además instancia el `TenantThemeCoordinator`, que mantiene el tema sincronizado con el tenant del
 * usuario logueado / impersonado (re-tematiza al cambiar el token; al desloguear vuelve a este estilo).
 */
export function provideTenantStyleInit(): EnvironmentProviders {
  return makeEnvironmentProviders([
    provideAppInitializer(() => {
      const config = inject(AppConfigService);
      const service = inject(TenantStyleService);
      const store = inject(TenantStyleStore);
      const theme = inject(ThemeStore);

      return config.load().then(async () => {
        const id = service.resolveIdentifier();
        const style = id
          ? await firstValueFrom(service.getPublicStyle(id).pipe(catchError(() => of(null))))
          : null;
        if (style) {
          service.apply(style);
        }
        store.set(style);
        store.setAnonymous(style);
        theme.initialize(style ? (style.darkModeByDefault ? 'dark' : 'light') : null);
      });
    }),
    // Instancia eager del coordinator → su effect empieza a seguir el tenant del token.
    provideEnvironmentInitializer(() => {
      inject(TenantThemeCoordinator);
    }),
  ]);
}
