import { DashboardContextService } from './dashboard-context.service';
import { DASHBOARD_WIDGETS } from './dashboard-widgets';
import { LicenciasKpisWidget } from '../widgets/licencias-kpis.widget';
import { ActividadChartWidget } from '../widgets/actividad-chart.widget';
import { AccesosDirectosWidget } from '../widgets/accesos-directos.widget';

/** Contexto falso por audiencia (capacidades): root ve todo; el resto, lo que tenga permitido. */
function fakeCtx(opts: { isRoot?: boolean; allowed?: string[] }): DashboardContextService {
  const isRoot = opts.isRoot ?? false;
  const allowed = new Set(opts.allowed ?? []);
  return {
    isRoot: () => isRoot,
    isImpersonating: () => false,
    ready: () => true,
    canAccess: (path: string) => isRoot || allowed.has(path),
  } as unknown as DashboardContextService;
}

/** Componentes que el home renderizaría para un contexto dado. */
function shownFor(ctx: DashboardContextService) {
  return DASHBOARD_WIDGETS.filter((w) => w.canShow(ctx)).map((w) => w.component);
}

describe('DASHBOARD_WIDGETS — audiencia por widget', () => {
  it('TODO widget declara canShow (no se puede registrar sin decidir audiencia)', () => {
    for (const widget of DASHBOARD_WIDGETS) {
      expect(typeof widget.canShow, widget.component.name).toBe('function');
    }
  });

  it('usuario común (sin permisos): NO ve licencias ni actividad, pero SÍ accesos directos', () => {
    const shown = shownFor(fakeCtx({ allowed: [] }));
    expect(shown).not.toContain(LicenciasKpisWidget);
    expect(shown).not.toContain(ActividadChartWidget);
    // Accesos directos es la "lanzadera" del común: el widget se muestra (se autolimita por datos).
    expect(shown).toContain(AccesosDirectosWidget);
  });

  it('admin de cliente (acceso a /usuarios): ve licencias y actividad', () => {
    const shown = shownFor(fakeCtx({ allowed: ['/usuarios', '/grupos'] }));
    expect(shown).toContain(LicenciasKpisWidget);
    expect(shown).toContain(ActividadChartWidget);
  });

  it('root: NO ve licencias (concepto por tenant cliente) pero sí actividad', () => {
    const shown = shownFor(fakeCtx({ isRoot: true }));
    expect(shown).not.toContain(LicenciasKpisWidget);
    expect(shown).toContain(ActividadChartWidget);
  });
});
