import { DOCUMENT, Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { AppConfigService } from '../config';
import { ApiClient } from '../http';
import { buildTenantSysVars } from './theme-palette';
import { TenantPublicStyle } from './tenant-public-style.model';

/** Id del `<link>` con el CSS custom del tenant (`styleSheetCssUrl`). */
const TENANT_CSS_ID = 'tbi-tenant-css';

/**
 * Consume el estilo público del tenant (`tenants/public-style`, anónimo) y lo aplica al documento en
 * runtime: colores (paleta M3 → variables `--mat-sys-*`), favicon, título y el CSS custom del tenant
 * (`styleSheetCssUrl`, con guarda same-origin). Los logos/fondos los expone el `TenantStyleStore`
 * para el layout (header y login).
 */
@Injectable({ providedIn: 'root' })
export class TenantStyleService {
  private readonly api = inject(ApiClient);
  private readonly appConfig = inject(AppConfigService);
  private readonly document = inject(DOCUMENT);

  /** Estilo público por hostName o código (anónimo; se llama en el bootstrap, antes del login). */
  getPublicStyle(value: string): Observable<TenantPublicStyle> {
    return this.api.get<TenantPublicStyle>(
      'general/tenants/public-style/' + encodeURIComponent(value),
    );
  }

  /**
   * Estilo por id de tenant (autenticado, `header-style/{id}` → `TenantHeaderStyleDto`). Se usa al
   * re-tematizar según el tenant del usuario logueado / impersonado (ver `TenantThemeCoordinator`).
   */
  getHeaderStyle(tenantId: number): Observable<TenantPublicStyle> {
    return this.api.get<TenantPublicStyle>('general/tenants/header-style/' + tenantId);
  }

  /**
   * Identifica al tenant a cargar: `?tenant=` (override explícito, p. ej. para probar) > `defaultTenant`
   * de la config (dev / single-tenant) > `hostname` (producción: el dominio mapea al tenant).
   */
  resolveIdentifier(): string {
    const win = this.document.defaultView;
    const fromQuery = win ? new URLSearchParams(win.location.search).get('tenant') : null;
    if (fromQuery) {
      return fromQuery;
    }
    const fromConfig = this.appConfig.config().defaultTenant?.trim();
    if (fromConfig) {
      return fromConfig;
    }
    return win?.location.hostname ?? '';
  }

  /** Aplica colores (si los hay), favicon y título del tenant al documento. */
  apply(style: TenantPublicStyle): void {
    if (style.colorPrimarioLight && style.colorPrimarioDark) {
      this.applyColors(style.colorPrimarioLight, style.colorPrimarioDark);
    }
    if (style.faviconUrl) {
      this.setFavicon(style.faviconUrl);
    }
    this.applyCustomCss(style.styleSheetCssUrl);
    this.document.title = style.tituloWeb || this.appConfig.config().appTitle;
  }

  /** Revierte al tema base: quita los overrides `--mat-sys-*` inline, el CSS custom y el título. */
  reset(): void {
    const style = this.document.documentElement.style;
    for (let i = style.length - 1; i >= 0; i--) {
      const prop = style.item(i);
      if (prop.startsWith('--mat-sys-')) {
        style.removeProperty(prop);
      }
    }
    this.removeCustomCss();
    this.document.title = this.appConfig.config().appTitle;
  }

  /** Sobreescribe las `--mat-sys-*` de color en `<html>` con la paleta tonal del tenant. */
  private applyColors(lightHex: string, darkHex: string): void {
    const root = this.document.documentElement;
    for (const [name, value] of Object.entries(buildTenantSysVars(lightHex, darkHex))) {
      root.style.setProperty(name, value);
    }
  }

  private setFavicon(url: string): void {
    const head = this.document.head;
    let link = head.querySelector<HTMLLinkElement>('link[rel~="icon"]');
    if (!link) {
      link = this.document.createElement('link');
      link.rel = 'icon';
      head.appendChild(link);
    }
    link.href = url;
  }

  /**
   * Inyecta el CSS custom del tenant como `<link>`, **solo si es del mismo origen que la API** (el
   * storage del backend). Una URL externa se ignora: evita cargar CSS arbitrario de terceros (vector
   * de inyección). Sin URL válida, quita cualquier CSS custom previo.
   */
  private applyCustomCss(url: string | null): void {
    if (!url || !this.isSameOriginAsApi(url)) {
      this.removeCustomCss();
      return;
    }
    const head = this.document.head;
    let link = head.querySelector<HTMLLinkElement>('#' + TENANT_CSS_ID);
    if (!link) {
      link = this.document.createElement('link');
      link.id = TENANT_CSS_ID;
      link.rel = 'stylesheet';
      head.appendChild(link);
    }
    link.href = url;
  }

  private removeCustomCss(): void {
    this.document.head.querySelector('#' + TENANT_CSS_ID)?.remove();
  }

  /** `true` si `url` es del mismo origen que la API (donde el backend sirve el CSS del tenant). */
  private isSameOriginAsApi(url: string): boolean {
    try {
      return new URL(url).origin === new URL(this.appConfig.config().apiUrl).origin;
    } catch {
      return false;
    }
  }
}
