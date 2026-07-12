import { Type } from '@angular/core';
import { DashboardContextService } from './dashboard-context.service';
import { ConteosKpisWidget } from '../widgets/conteos-kpis.widget';
import { LicenciasKpisWidget } from '../widgets/licencias-kpis.widget';
import { AccesosDirectosWidget } from '../widgets/accesos-directos.widget';
import { ActividadChartWidget } from '../widgets/actividad-chart.widget';

/** Entrada del registro del dashboard: el componente + a quién se le muestra. */
export interface DashboardWidget {
  readonly component: Type<unknown>;
  /**
   * Decisión **obligatoria** de audiencia: el home renderiza el widget sólo si devuelve `true`.
   * Se evalúa por **capacidad** (no por rol enumerado), así contempla root / admin / común — y los
   * que vengan — sin tocar el home. `() => true` = mostrar a todos: el widget adapta su contenido
   * (o se autolimita por datos) internamente.
   */
  readonly canShow: (ctx: DashboardContextService) => boolean;
}

/**
 * Registro de widgets del dashboard. Cada entrada DEBE declarar `canShow`: es **estructuralmente
 * imposible** registrar un widget sin decidir su audiencia (el home no lo instancia si no pasa).
 * Sumar un indicador = una entrada acá; sumar un tipo de usuario = nada (lo define la capacidad).
 */
export const DASHBOARD_WIDGETS: readonly DashboardWidget[] = [
  // Muestra a todos; se autolimita por capacidad por-sección (Tenants/Usuarios/Grupos) adentro.
  { component: ConteosKpisWidget, canShow: () => true },
  // Licencias = info administrativa por tenant cliente: no para root, sólo con acceso a Usuarios.
  { component: LicenciasKpisWidget, canShow: (ctx) => !ctx.isRoot() && ctx.canAccess('/usuarios') },
  // Accesos directos: muestra a todos; se autolimita por datos (la API ya filtra por permiso).
  { component: AccesosDirectosWidget, canShow: () => true },
  // Actividad de usuarios del tenant: para quien accede a Usuarios.
  { component: ActividadChartWidget, canShow: (ctx) => ctx.canAccess('/usuarios') },
];
