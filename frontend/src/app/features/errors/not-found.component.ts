import { ChangeDetectionStrategy, Component } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { RouterLink } from '@angular/router';

/**
 * Página 404. Se muestra cuando se navega a una ruta inexistente (`**` del router) o cuando un
 * request a la API devuelve 404 (el `errorInterceptor` redirige acá). Vive dentro del layout
 * autenticado, así el usuario conserva el menú para seguir navegando.
 */
@Component({
  selector: 'tbi-not-found',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatButtonModule, MatIconModule, RouterLink],
  template: `
    <div class="error-page" data-testid="not-found-page">
      <mat-icon class="error-page__icon">sentiment_very_dissatisfied</mat-icon>
      <p class="error-page__code">404</p>
      <h1 class="error-page__title">Página no encontrada</h1>
      <p class="error-page__text">La página que buscás no existe o fue movida.</p>
      <a matButton="filled" routerLink="/" data-testid="error-go-home">Ir al inicio</a>
    </div>
  `,
  styleUrl: './error-page.scss',
})
export class NotFoundComponent {}
