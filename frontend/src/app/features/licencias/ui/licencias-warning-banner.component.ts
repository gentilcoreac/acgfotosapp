import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import { LicenciaResumen } from '../domain/licencia-resumen.model';
import { estadoVigencia } from '../domain/licencia-metrics';

/** Una licencia con problema de vigencia, ya resuelta a texto para el banner. */
interface AlertaLicencia {
  descripcion: string;
  vencida: boolean;
  fecha: string;
}

/**
 * Banner de aviso de licencias vencidas o por vencer (paridad con el warning de Budgeting). Toma
 * el resumen de licencias del tenant y, si alguna está vencida o dentro del umbral de "por vencer"
 * (lo calcula la API: `isExpired` / `isExpiringSoon`), muestra un aviso. El tono escala a error si
 * hay alguna vencida; si sólo hay próximas a vencer, usa el tono de advertencia (tertiary). Si no
 * hay nada que avisar, no renderiza nada.
 */
@Component({
  selector: 'tbi-licencias-warning-banner',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatIconModule],
  template: `
    @if (alertas().length > 0) {
      <div class="banner" [class.banner--error]="hayVencidas()" role="alert">
        <mat-icon class="banner__icon">{{ hayVencidas() ? 'error' : 'schedule' }}</mat-icon>
        <div class="banner__body">
          <strong class="banner__title">
            {{ hayVencidas() ? 'Licencias vencidas' : 'Licencias por vencer' }}
          </strong>
          <ul class="banner__list">
            @for (a of alertas(); track a.descripcion) {
              <li>
                <strong>{{ a.descripcion }}</strong>
                {{ a.vencida ? 'venció el' : 'vence el' }} {{ a.fecha }}
              </li>
            }
          </ul>
        </div>
      </div>
    }
  `,
  styles: `
    .banner {
      display: flex;
      gap: 12px;
      align-items: flex-start;
      padding: 12px 16px;
      margin-bottom: 1.25rem;
      border-radius: 16px;
      background: var(--mat-sys-tertiary-container);
      color: var(--mat-sys-on-tertiary-container);
    }

    .banner--error {
      background: var(--mat-sys-error-container);
      color: var(--mat-sys-on-error-container);
    }

    .banner__icon {
      flex: none;
      margin-top: 1px;
    }

    .banner__title {
      font: var(--mat-sys-title-small);
    }

    .banner__list {
      margin: 4px 0 0;
      padding-left: 18px;
      font: var(--mat-sys-body-medium);
    }

    .banner__list li + li {
      margin-top: 2px;
    }
  `,
})
export class LicenciasWarningBannerComponent {
  readonly licencias = input.required<LicenciaResumen[]>();

  /** Licencias vencidas o por vencer, ordenadas (vencidas primero) y con fecha ya formateada. */
  protected readonly alertas = computed<AlertaLicencia[]>(() =>
    this.licencias()
      .map((l) => ({ licencia: l, estado: estadoVigencia(l) }))
      .filter((x) => x.estado !== 'vigente')
      .map((x) => ({
        descripcion: x.licencia.descripcion,
        vencida: x.estado === 'vencida',
        fecha: x.licencia.expirationDate
          ? new Date(x.licencia.expirationDate).toLocaleDateString()
          : '—',
      }))
      .sort((a, b) => Number(b.vencida) - Number(a.vencida)),
  );

  protected readonly hayVencidas = computed(() => this.alertas().some((a) => a.vencida));
}
