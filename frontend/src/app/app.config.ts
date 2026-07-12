import {
  ApplicationConfig,
  provideBrowserGlobalErrorListeners,
  provideZonelessChangeDetection,
} from '@angular/core';
import { provideHttpClient, withFetch, withInterceptors } from '@angular/common/http';
import { MatPaginatorIntl } from '@angular/material/paginator';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { provideRouter } from '@angular/router';

import { routes } from './app.routes';
import { provideAppConfig } from './core/config';
import { authInterceptor, provideAuthSessionRestore } from './core/auth';
import { errorInterceptor } from './core/http';
import { provideTenantStyleInit } from './core/tenant-style';
import { paginatorIntlEs } from './shared/ui/tbi-table/paginator-intl-es';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideZonelessChangeDetection(),
    provideAnimationsAsync(),
    provideAppConfig(),
    // Resuelve el tenant y aplica su estilo (colores/favicon/título) + el modo de color inicial,
    // apenas carga la config y antes del primer render. Tolerante: sin tenant → tema base.
    provideTenantStyleInit(),
    // Orden: authInterceptor (externo) agrega Bearer y maneja 401; errorInterceptor
    // (interno) normaliza el resto de los errores y deja pasar el 401 al auth.
    provideHttpClient(withFetch(), withInterceptors([authInterceptor, errorInterceptor])),
    // Recupera la sesión en el arranque (F5) vía la cookie de refresh; tolera fallo.
    provideAuthSessionRestore(),
    // Paginador de Material en español (textos del framework, no claves i18n propias).
    { provide: MatPaginatorIntl, useFactory: paginatorIntlEs },
    provideRouter(routes),
  ],
};
