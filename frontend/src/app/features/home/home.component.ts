import { ChangeDetectionStrategy, Component, Injector, computed, inject } from '@angular/core';
import { NgComponentOutlet } from '@angular/common';
import { DashboardContextService } from './dashboard/dashboard-context.service';
import { DASHBOARD_WIDGETS, DashboardWidget } from './dashboard/dashboard-widgets';

// Landing autenticado: dashboard compuesto por widgets. El home NO sabe de roles ni hace ifs por
// tipo de usuario — recorre el registro y renderiza cada widget **sólo si su `canShow(ctx)` pasa**.
// Así es imposible que un widget se muestre sin contemplar su audiencia (root / admin / común / …),
// y un tipo de usuario nuevo no toca el home (la visibilidad es por capacidad). Cada widget carga
// sus propios datos (una sola llamada por fuente, ver cada widget).
// TODO (Fase 4 - i18n): textos en español por ahora.
@Component({
  selector: 'tbi-home',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [NgComponentOutlet],
  providers: [DashboardContextService],
  templateUrl: './home.component.html',
  styleUrl: './home.component.scss',
})
export class HomeComponent {
  private readonly ctx = inject(DashboardContextService);
  // Injector del home para que los widgets creados por NgComponentOutlet resuelvan el
  // DashboardContextService provisto acá.
  protected readonly injector = inject(Injector);

  protected readonly subtitulo = computed(() =>
    this.ctx.isRoot() ? 'Resumen del sistema.' : 'Resumen de tu organización.',
  );

  protected readonly widgets = DASHBOARD_WIDGETS;

  /** El home decide la visibilidad por widget (gate centralizado). Reactivo: lee señales del ctx. */
  protected canShow(widget: DashboardWidget): boolean {
    return widget.canShow(this.ctx);
  }
}
