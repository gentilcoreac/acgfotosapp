import { Injectable, Signal, computed, inject, signal } from '@angular/core';
import { ThemeStore } from '../theme';
import { TenantPublicStyle } from './tenant-public-style.model';

/**
 * Guarda el estilo público del tenant activo (lo setea el bootstrap, ver `provideTenantStyleInit`) y
 * expone lo que el layout consume reactivamente. Los colores/favicon/título los aplica el
 * `TenantStyleService` directo al documento; acá viven los datos que dependen del modo (logo).
 */
@Injectable({ providedIn: 'root' })
export class TenantStyleStore {
  private readonly theme = inject(ThemeStore);

  private readonly _style = signal<TenantPublicStyle | null>(null);
  private readonly _anonymousStyle = signal<TenantPublicStyle | null>(null);

  /** Estilo del tenant activo (o null si no se resolvió / no hay branding). */
  readonly style: Signal<TenantPublicStyle | null> = this._style.asReadonly();

  /**
   * Estilo resuelto en el bootstrap por dominio/URL (anónimo). Es a lo que se vuelve al desloguear
   * (cuando el tema dejaba de seguir a un usuario). Null si el dominio no mapeaba a un tenant.
   */
  readonly anonymousStyle: Signal<TenantPublicStyle | null> = this._anonymousStyle.asReadonly();

  /** Logo del header según el modo actual (dark → DarkUrl, light → LightUrl); null si no hay. */
  readonly headerLogoUrl = computed<string | null>(() => {
    const style = this._style();
    if (!style) {
      return null;
    }
    return (this.theme.isDark() ? style.logoHeaderDarkUrl : style.logoHeaderLightUrl) ?? null;
  });

  set(style: TenantPublicStyle | null): void {
    this._style.set(style);
  }

  /** Guarda el estilo anónimo del bootstrap (para revertir al desloguear). */
  setAnonymous(style: TenantPublicStyle | null): void {
    this._anonymousStyle.set(style);
  }
}
