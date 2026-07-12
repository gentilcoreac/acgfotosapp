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
import { LicenciasService } from '../../licencias/data/licencias.service';
import { LicenciaResumen } from '../../licencias/domain/licencia-resumen.model';
import { agregarLicencias } from '../../licencias/domain/licencia-metrics';
import {
  TbiKpiCardComponent,
  TbiKpiTone,
} from '../../../shared/ui/tbi-kpi-card/tbi-kpi-card.component';

interface LicenciaKpi {
  icon: string;
  label: string;
  value: string | number;
  tone?: TbiKpiTone;
}

/**
 * Widget de KPIs de licencias (Usadas X/Y, Por vencer, Vencidas). Las tres salen de **una sola**
 * llamada (`getResumen`); el cálculo está centralizado en `agregarLicencias`.
 *
 * La **audiencia la decide el registro** (`DASHBOARD_WIDGETS`: no root + acceso a `/usuarios`): este
 * widget se instancia sólo cuando corresponde, así que acá no hay gateo — sólo trae y pinta.
 * `:host { display: contents }`: las tarjetas son ítems del grid del home.
 */
@Component({
  selector: 'tbi-licencias-kpis-widget',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TbiKpiCardComponent],
  template: `
    @for (k of cards(); track k.label) {
      <tbi-kpi-card
        [icon]="k.icon"
        [label]="k.label"
        [value]="k.value"
        [tone]="k.tone ?? 'default'"
        link="/usuarios"
      />
    }
  `,
  styles: `
    :host {
      display: contents;
    }
  `,
})
export class LicenciasKpisWidget {
  private readonly licenciasService = inject(LicenciasService);
  private readonly store = inject(AuthStore);
  private readonly destroyRef = inject(DestroyRef);

  private readonly resumen = signal<LicenciaResumen[] | null>(null);
  private sub?: Subscription;

  protected readonly cards = computed<LicenciaKpi[]>(() => {
    const resumen = this.resumen();
    if (resumen == null) {
      return [];
    }
    const { total, asignadas, porVencer, vencidas } = agregarLicencias(resumen);
    const cards: LicenciaKpi[] = [
      { icon: 'badge', label: 'Licencias usadas', value: `${asignadas} / ${total}` },
      {
        icon: 'schedule',
        label: 'Por vencer',
        value: porVencer,
        tone: porVencer > 0 ? 'warning' : 'default',
      },
    ];
    if (vencidas > 0) {
      cards.push({ icon: 'error', label: 'Vencidas', value: vencidas, tone: 'error' });
    }
    return cards;
  });

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
    this.resumen.set(null);
    this.sub = this.licenciasService
      .getResumen()
      .pipe(catchError(() => of<LicenciaResumen[]>([])))
      .subscribe((r) => this.resumen.set(r));
  }
}
