import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  computed,
  effect,
  inject,
  signal,
} from '@angular/core';
import { Observable, Subscription, catchError, forkJoin, of } from 'rxjs';
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
  private readonly destroyRef = inject(DestroyRef);

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

  private readonly valores = signal<Record<string, number | null>>({});
  private sub?: Subscription;

  constructor() {
    // Recarga al cambiar el contexto (tenant) o cuando se resuelven las capacidades (cambia el set
    // visible). Solo pide los conteos visibles.
    effect(() => {
      this.store.tenantId();
      this.reload(this.visibles());
    });
    this.destroyRef.onDestroy(() => this.sub?.unsubscribe());
  }

  private reload(visibles: Conteo[]): void {
    this.sub?.unsubscribe();
    this.valores.set({});
    if (visibles.length === 0) {
      return;
    }
    const calls: Record<string, Observable<number | null>> = {};
    for (const c of visibles) {
      calls[c.route] = c.load().pipe(catchError(() => of<number | null>(null)));
    }
    this.sub = forkJoin(calls).subscribe((res) => this.valores.set(res));
  }

  /** Valor del conteo (— mientras carga). */
  protected valor(route: string): string | number {
    const v = this.valores()[route];
    return v == null ? '—' : v;
  }
}
