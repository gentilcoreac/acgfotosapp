import { DOCUMENT, Injectable, Signal, computed, inject, signal } from '@angular/core';
import { AppConfigService } from '../config';
import { ThemeMode } from './theme.model';

/** Clave de localStorage donde se persiste el modo elegido explícitamente por el usuario. */
const STORAGE_KEY = 'tbi.theme';

/** Clase que activa la transición suave de color; debe coincidir con `styles/_utilities.scss`. */
const TRANSITION_CLASS = 'tbi-theme-transition';
/** Duración de la transición (ms); algo mayor que la del CSS para no cortarla antes de tiempo. */
const TRANSITION_MS = 350;

/**
 * Estado del modo de color (light/dark) basado en signals.
 *
 * El modo se aplica seteando `color-scheme` en `<html>`: Material emite las variables de sistema
 * `--mat-sys-*` como `light-dark(claro, oscuro)`, así que `color-scheme` decide cuál resuelve sin
 * re-emitir el tema. Se fija en `documentElement` (no en `body`) para cubrir también los overlays
 * del CDK (diálogos, menús, snackbars), que se montan fuera del árbol del layout.
 *
 * Solo el toggle explícito del usuario persiste; `initialize()` resuelve el modo inicial sin
 * escribir, para no congelar el default de tenant/SO.
 */
@Injectable({ providedIn: 'root' })
export class ThemeStore {
  private readonly document = inject(DOCUMENT);
  private readonly appConfig = inject(AppConfigService);

  private readonly _mode = signal<ThemeMode>('light');

  /** Modo de color actual. */
  readonly mode: Signal<ThemeMode> = this._mode.asReadonly();
  /** `true` si el modo actual es oscuro (para íconos/labels reactivos). */
  readonly isDark = computed<boolean>(() => this._mode() === 'dark');

  /**
   * Resuelve y aplica el modo inicial en el arranque (ver `provideThemeInit`). Prioridad:
   * localStorage > `externalDefault` (hook para `DarkModeByDefault` del tenant, Fase 2) >
   * `AppConfig.defaultTheme` > preferencia del SO. No persiste (solo aplica).
   */
  initialize(externalDefault?: ThemeMode | null): void {
    const mode =
      this.readStored() ??
      externalDefault ??
      this.normalize(this.appConfig.config().defaultTheme) ??
      this.osPreference();
    this._mode.set(mode);
    this.applyMode(mode);
  }

  /** Fija el modo (acción del usuario): actualiza estado, persiste y aplica al DOM (con transición). */
  setMode(mode: ThemeMode): void {
    this._mode.set(mode);
    this.persist(mode);
    this.animateChange();
    this.applyMode(mode);
  }

  /** Alterna entre light y dark. */
  toggle(): void {
    this.setMode(this.isDark() ? 'light' : 'dark');
  }

  /** Aplica el modo seteando `color-scheme` en `<html>`. */
  private applyMode(mode: ThemeMode): void {
    this.document.documentElement.style.colorScheme = mode;
  }

  /**
   * Anima el cambio de tema: agrega una clase a `<html>` durante la transición y la saca al terminar.
   * Acotado al toggle del usuario (no al `initialize`), para no penalizar hovers ni el primer render.
   */
  private animateChange(): void {
    const root = this.document.documentElement;
    root.classList.add(TRANSITION_CLASS);
    this.document.defaultView?.setTimeout(
      () => root.classList.remove(TRANSITION_CLASS),
      TRANSITION_MS,
    );
  }

  private readStored(): ThemeMode | null {
    try {
      return this.normalize(this.document.defaultView?.localStorage.getItem(STORAGE_KEY));
    } catch {
      return null;
    }
  }

  private persist(mode: ThemeMode): void {
    try {
      this.document.defaultView?.localStorage.setItem(STORAGE_KEY, mode);
    } catch {
      // localStorage no disponible (incógnito / restricciones) → el modo queda solo en memoria.
    }
  }

  /** Preferencia de color del SO (`prefers-color-scheme`), con fallback a light. */
  private osPreference(): ThemeMode {
    return this.document.defaultView?.matchMedia('(prefers-color-scheme: dark)').matches
      ? 'dark'
      : 'light';
  }

  /** Normaliza un valor arbitrario a un `ThemeMode` válido (o null si no lo es). */
  private normalize(value: string | null | undefined): ThemeMode | null {
    return value === 'light' || value === 'dark' ? value : null;
  }
}
