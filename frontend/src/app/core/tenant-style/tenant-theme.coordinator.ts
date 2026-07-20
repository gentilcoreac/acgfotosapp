import { Injectable, effect, inject } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { catchError, of } from 'rxjs';
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

  /** Refetchea sola cuando cambia el tenant del token (cancela el pedido en vuelo anterior — antes
   * un `subscribe()` suelto en cada cambio, sin cancelar el anterior). `null` = sin estilo (404/sin
   * permiso) o el resource todavía no fetcheó (tenant anónimo) — en ambos casos se mantiene el tema
   * actual. */
  private readonly headerStyleResource = rxResource({
    params: () => this.auth.tenantId() ?? undefined,
    stream: ({ params }) => this.service.getHeaderStyle(params).pipe(catchError(() => of(null))),
  });

  constructor() {
    effect(() => {
      const tenantId = this.auth.tenantId();
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
      const style = this.headerStyleResource.value();
      if (style) {
        this.service.apply(style);
        this.store.set(style);
      }
    });
  }
}
