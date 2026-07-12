import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  computed,
  effect,
  inject,
  signal,
} from '@angular/core';
import { Subscription, catchError, of } from 'rxjs';
import { AuthStore } from '../../../core/auth';
import { UsuariosService } from '../../usuarios/data/usuarios.service';
import { ACTIVIDAD_MENSUAL_MESES, UsuarioActividadMes } from '../../usuarios/domain/usuario.model';
import {
  TbiBarChartComponent,
  TbiBarSeries,
} from '../../../shared/ui/tbi-bar-chart/tbi-bar-chart.component';

/** Abreviaturas de mes (es) para las etiquetas del gráfico. `mes` del backend es 1-12. */
const MESES_ABBR = [
  'ene',
  'feb',
  'mar',
  'abr',
  'may',
  'jun',
  'jul',
  'ago',
  'sep',
  'oct',
  'nov',
  'dic',
];

/**
 * Widget del gráfico de actividad mensual (usuarios activos + altas). Una sola llamada
 * (`getActividadMensual`). La **audiencia la decide el registro** (acceso a `/usuarios`): este widget
 * se instancia sólo cuando corresponde. Ocupa el ancho completo del grid.
 */
@Component({
  selector: 'tbi-actividad-chart-widget',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TbiBarChartComponent],
  template: `
    @if (serie() !== null) {
      <section class="chart">
        <header class="chart__header">
          <h2>Usuarios por mes</h2>
          <span>Últimos {{ mesesVentana }} meses</span>
        </header>
        @if (vacia()) {
          <p class="chart__empty">Sin actividad registrada en el período.</p>
        } @else {
          <tbi-bar-chart [categories]="categorias()" [series]="series()" />
        }
      </section>
    }
  `,
  styles: `
    :host {
      display: contents;
    }

    // Ocupa todo el ancho del grid del home, debajo de las KPI cards.
    .chart {
      grid-column: 1 / -1;
      padding: 20px 22px;
      border: 1px solid color-mix(in srgb, var(--mat-sys-outline-variant) 55%, transparent);
      border-radius: 22px;
      background: var(--mat-sys-surface-container-low);
    }

    .chart__header {
      display: flex;
      align-items: baseline;
      justify-content: space-between;
      gap: 1rem;
      margin-bottom: 0.5rem;

      h2 {
        margin: 0;
        font: var(--mat-sys-title-medium);
        color: var(--mat-sys-on-surface);
      }

      span {
        font: var(--mat-sys-body-small);
        color: var(--mat-sys-on-surface-variant);
      }
    }

    .chart__empty {
      margin: 1.5rem 0;
      text-align: center;
      color: var(--mat-sys-on-surface-variant);
    }
  `,
})
export class ActividadChartWidget {
  private readonly usuariosService = inject(UsuariosService);
  private readonly store = inject(AuthStore);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly mesesVentana = ACTIVIDAD_MENSUAL_MESES;

  private readonly serieData = signal<UsuarioActividadMes[] | null>(null);
  /** Expuesto al template (la serie cargada, o null mientras carga). */
  protected readonly serie = this.serieData.asReadonly();
  private sub?: Subscription;

  protected readonly categorias = computed(() =>
    (this.serieData() ?? []).map((m) => MESES_ABBR[m.mes - 1]),
  );
  protected readonly series = computed<TbiBarSeries[]>(() => {
    const serie = this.serieData() ?? [];
    return [
      { name: 'Activos', tone: 'primary', values: serie.map((m) => m.activos) },
      { name: 'Altas', tone: 'tertiary', values: serie.map((m) => m.altas) },
    ];
  });
  protected readonly vacia = computed(() =>
    (this.serieData() ?? []).every((m) => m.activos === 0 && m.altas === 0),
  );

  constructor() {
    // Recarga al cambiar el contexto (tenant del token: login / impersonar / parar).
    effect(() => {
      this.store.tenantId();
      this.reload();
    });
    this.destroyRef.onDestroy(() => this.sub?.unsubscribe());
  }

  private reload(): void {
    this.sub?.unsubscribe();
    this.serieData.set(null);
    this.sub = this.usuariosService
      .getActividadMensual()
      .pipe(catchError(() => of<UsuarioActividadMes[]>([])))
      .subscribe((s) => this.serieData.set(s));
  }
}
