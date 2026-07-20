import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { Observable, catchError, forkJoin, of } from 'rxjs';
import { AuthStore } from '../../../core/auth';
import { TbiKpiCardComponent } from '../../../shared/ui/tbi-kpi-card/tbi-kpi-card.component';
import { HomeMetricsService } from '../data/home-metrics.service';
import { DashboardContextService } from '../dashboard/dashboard-context.service';

/** Un conteo de entidad: tarjeta + cómo se trae + a qué sección pertenece (para el gateo). */
interface Conteo {
  label: string;
  icon: string;
  route: string;
  load: () => Observable<number>;
}

/**
 * Widget de KPIs de conteo (Tenants / Usuarios / Grupos). Cada conteo se muestra **solo si el
 * usuario puede acceder a su sección** (capacidad) y se trae **solo si es visible** (no se pide lo
 * que no se puede ver → evita llamadas 401). Agregar un conteo = una entrada en `conteos`.
 *
 * `:host { display: contents }` hace que las tarjetas sean ítems directos del grid del home.
 */
@Component({
  selector: 'tbi-conteos-kpis-widget',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TbiKpiCardComponent],
  template: `
    @for (c of visibles(); track c.route) {
      <tbi-kpi-card [icon]="c.icon" [label]="c.label" [value]="valor(c.route)" [link]="c.route" />
    }
  `,
  styles: `
    :host {
      display: contents;
    }
  `,
})
export class ConteosKpisWidget {
  private readonly ctx = inject(DashboardContextService);
  private readonly metrics = inject(HomeMetricsService);
  private readonly store = inject(AuthStore);

  private readonly conteos: Conteo[] = [
    {
      label: 'Tenants',
      icon: 'apartment',
      route: '/tenants',
      load: () => this.metrics.countTenants(),
    },
    {
      label: 'Usuarios',
      icon: 'group',
      route: '/usuarios',
      load: () => this.metrics.countUsuarios(),
    },
    { label: 'Grupos', icon: 'groups', route: '/grupos', load: () => this.metrics.countGrupos() },
  ];

  /** Conteos visibles según las capacidades del usuario. */
  protected readonly visibles = computed(() =>
    this.conteos.filter((c) => this.ctx.canAccess(c.route)),
  );

  /** Refetchea sola al cambiar el contexto (tenant) o cuando se resuelven las capacidades (cambia el
   * set visible). Sólo pide los conteos visibles. Cancela el pedido en vuelo anterior
   * automáticamente (antes `Subscription`/`destroyRef` a mano). */
  private readonly valoresResource = rxResource({
    params: () => ({ tenantId: this.store.tenantId(), visibles: this.visibles() }),
    stream: ({ params }) => {
      if (params.visibles.length === 0) {
        return of<Record<string, number | null>>({});
      }
      const calls: Record<string, Observable<number | null>> = {};
      for (const c of params.visibles) {
        calls[c.route] = c.load().pipe(catchError(() => of<number | null>(null)));
      }
      return forkJoin(calls);
    },
  });

  /** Valor del conteo (— mientras carga). */
  protected valor(route: string): string | number {
    const v = this.valoresResource.value()?.[route];
    return v == null ? '—' : v;
  }
}
