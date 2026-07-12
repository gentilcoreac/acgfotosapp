import { HttpErrorResponse } from '@angular/common/http';
import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  computed,
  inject,
  OnInit,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { ActivatedRoute, Router } from '@angular/router';
import { catchError, concatMap, map, of, switchMap, take, takeWhile, timer } from 'rxjs';
import { AplicacionContextStore } from '../../../core/aplicacion-context';
import { AuthService, AuthStore, ReconnectReason } from '../../../core/auth';
import { AppConfigService } from '../../../core/config';

/** Cadencia y tope de reintentos. La ventana del rate limit es de 60s → ~13 intentos cada 5s la cubren. */
const RETRY_MS = 5000;
const MAX_ATTEMPTS = 14;

type Outcome = 'ok' | 'logout' | 'retry';

/**
 * Mensaje de la pantalla según la causa de la reconexión. El 429 NO es lo mismo que la API caída:
 * decir "demasiadas solicitudes" cuando en realidad la API no responde confunde al usuario.
 */
const MESSAGE_BY_REASON: Record<ReconnectReason, string> = {
  'rate-limit':
    'Demasiadas solicitudes en poco tiempo. Estamos recuperando tu sesión, no hace falta que hagas nada.',
  network:
    'No pudimos conectar con el servidor. Reintentando automáticamente, no hace falta que hagas nada.',
  server:
    'El servidor está teniendo problemas. Reintentando automáticamente, no hace falta que hagas nada.',
};

/**
 * Pantalla de reconexión. Se muestra cuando la restauración de sesión (F5) falló por una causa
 * transitoria (429 rate limit / red / 5xx) en vez de mandar a login: la sesión probablemente sigue
 * válida. Reintenta el `refresh` en segundo plano hasta entrar (y ahí vuelve a `returnUrl`), se
 * rinde a login si el refresh es rechazado de verdad (401/403) o si se agota el tope de intentos.
 */
@Component({
  selector: 'tbi-reconnecting',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatProgressSpinnerModule],
  template: `
    <div class="reconnect">
      <div class="reconnect__card">
        <mat-progress-spinner mode="indeterminate" diameter="48" />
        <h1 class="reconnect__title">Reconectando…</h1>
        <p class="reconnect__text">{{ message() }}</p>
        <p class="reconnect__brand">{{ appTitle }}</p>
      </div>
    </div>
  `,
  styles: `
    .reconnect {
      display: flex;
      align-items: center;
      justify-content: center;
      min-height: 100dvh;
      padding: 1.5rem;
      background-color: var(--mat-sys-surface);
    }
    .reconnect__card {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 1rem;
      max-width: 360px;
      text-align: center;
    }
    .reconnect__title {
      margin: 0;
      font: var(--mat-sys-headline-small);
      color: var(--mat-sys-on-surface);
    }
    .reconnect__text {
      margin: 0;
      font: var(--mat-sys-body-medium);
      color: var(--mat-sys-on-surface-variant);
    }
    .reconnect__brand {
      margin-top: 0.5rem;
      font: var(--mat-sys-label-medium);
      color: var(--mat-sys-on-surface-variant);
    }
  `,
})
export class ReconnectingComponent implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly store = inject(AuthStore);
  private readonly aplicacionStore = inject(AplicacionContextStore);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly appTitle = inject(AppConfigService).config().appTitle;

  /** Mensaje según la causa de la reconexión; cae a rate-limit si no hay causa (no debería pasar). */
  protected readonly message = computed(
    () => MESSAGE_BY_REASON[this.store.reconnectReason() ?? 'rate-limit'],
  );

  ngOnInit(): void {
    // Si por algún motivo ya hay sesión, salir directo.
    if (this.store.isAuthenticated()) {
      this.finish(this.returnUrl());
      return;
    }

    let resolved = false;
    timer(0, RETRY_MS)
      .pipe(
        take(MAX_ATTEMPTS),
        // concatMap serializa: no encima un refresh sobre el anterior. Cada tick invoca un refresh
        // nuevo (el anterior ya finalizó). load() no falla (tiene su propio catchError).
        concatMap(() =>
          this.auth.refreshSession().pipe(
            switchMap(() => this.aplicacionStore.load()),
            map((): Outcome => 'ok'),
            catchError((err: HttpErrorResponse) =>
              of<Outcome>(err?.status === 401 || err?.status === 403 ? 'logout' : 'retry'),
            ),
          ),
        ),
        // Sigue mientras sea 'retry'; corta (incluyéndolo) en el primer 'ok'/'logout'.
        takeWhile((o) => o === 'retry', true),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (outcome) => {
          if (outcome === 'ok') {
            resolved = true;
            this.finish(this.returnUrl());
          } else if (outcome === 'logout') {
            resolved = true;
            this.finish('/login');
          }
        },
        // Se agotaron los intentos sin reconectar (la ventana no cedió o la API sigue caída): a login.
        complete: () => {
          if (!resolved) {
            this.finish('/login');
          }
        },
      });
  }

  private returnUrl(): string {
    const url = this.route.snapshot.queryParamMap.get('returnUrl');
    return url && url.startsWith('/') ? url : '/';
  }

  private finish(url: string): void {
    this.store.setReconnecting(false);
    void this.router.navigateByUrl(url);
  }
}
