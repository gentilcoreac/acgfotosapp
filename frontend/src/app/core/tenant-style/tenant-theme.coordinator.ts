import { Injectable, effect, inject, untracked } from '@angular/core';
import { AuthStore } from '../auth';
import { TenantStyleService } from './tenant-style.service';
import { TenantStyleStore } from './tenant-style.store';

/**
 * Mantiene el tema sincronizado con el **tenant efectivo del token** (claim `tenant`): cuando cambia
 * por login / impersonar / parar / restore de sesión, trae el estilo de ese tenant
 * (`header-style/{id}`) y lo aplica. Sin token (logout / anónimo) vuelve al branding por dominio que
 * resolvió el bootstrap (o al tema base si el dominio no mapeaba a ningún tenant).
 *
 * No toca el modo light/dark (eso lo gobierna `ThemeStore` + la preferencia del usuario); solo
 * recolorea (paleta), favicon, título y logo. Se instancia eagerly en `provideTenantStyleInit`.
 */
@Injectable({ providedIn: 'root' })
export class TenantThemeCoordinator {
  private readonly auth = inject(AuthStore);
  private readonly service = inject(TenantStyleService);
  private readonly store = inject(TenantStyleStore);

  constructor() {
    effect(() => {
      const tenantId = this.auth.tenantId();
      // El único disparador es el tenant del token; el resto del trabajo va sin tracking.
      untracked(() => this.applyForTenant(tenantId));
    });
  }

  private applyForTenant(tenantId: number | null): void {
    if (tenantId === null) {
      const anonymous = this.store.anonymousStyle();
      if (anonymous) {
        this.service.apply(anonymous);
        this.store.set(anonymous);
      } else {
        this.service.reset();
        this.store.set(null);
      }
      return;
    }
    this.service.getHeaderStyle(tenantId).subscribe({
      next: (style) => {
        this.service.apply(style);
        this.store.set(style);
      },
      // Sin estilo (404 / sin permiso) → se mantiene el tema actual.
      error: () => undefined,
    });
  }
}
