import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { LicenciaResumen } from '../domain/licencia-resumen.model';
import { agregarLicencias } from '../domain/licencia-metrics';
import { LicenciasDetalleDialogComponent } from './licencias-detalle-dialog.component';

/** Una tarjeta KPI ya resuelta a valores presentables. */
interface Kpi {
  label: string;
  value: number;
  icon: string;
  /** Resalta el ícono en tono de aviso (tertiary) cuando el KPI requiere atención (p. ej. por vencer). */
  alerta?: boolean;
  /** Resalta el ícono en tono de error (más urgente que `alerta`); para licencias vencidas. */
  error?: boolean;
}

/**
 * Indicador de licencias disponibles para el page header (paridad con la barra "licencias
 * disponibles" de Budgeting, en clave M3). Agrega el resumen del tenant en tres KPIs —Asignadas,
 * Disponibles, Por vencer— reutilizando el lenguaje visual de las KPI cards del handoff, y abre el
 * diálogo de detalle al hacer click. No hace fetch: recibe el resumen ya cargado por el padre, que
 * es el mismo dato que alimenta la columna de licencia y el banner (una sola consulta).
 */
@Component({
  selector: 'tbi-licencias-kpis',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatButtonModule, MatIconModule],
  template: `
    <section class="kpis" aria-label="Licencias del tenant">
      @for (kpi of kpis(); track kpi.label) {
        <button type="button" class="kpi" (click)="verDetalle()">
          <span
            class="kpi__icon"
            [class.kpi__icon--alerta]="kpi.alerta"
            [class.kpi__icon--error]="kpi.error"
          >
            <mat-icon>{{ kpi.icon }}</mat-icon>
          </span>
          <span class="kpi__text">
            <span class="kpi__value">{{ kpi.value }}</span>
            <span class="kpi__label">{{ kpi.label }}</span>
          </span>
        </button>
      }
    </section>
  `,
  styles: `
    .kpis {
      display: flex;
      flex-wrap: wrap;
      gap: 12px;
      margin-bottom: 1.5rem;
    }

    .kpi {
      flex: 1 1 160px;
      display: flex;
      align-items: center;
      gap: 12px;
      padding: 14px 16px;
      border: 1px solid color-mix(in srgb, var(--mat-sys-outline-variant) 55%, transparent);
      border-radius: 22px;
      background: var(--mat-sys-surface-container-low);
      cursor: pointer;
      text-align: left;
      transition: border-color 0.15s ease;
      font: inherit;
      color: inherit;
    }

    .kpi:hover {
      border-color: color-mix(in srgb, var(--mat-sys-primary) 45%, transparent);
    }

    .kpi__icon {
      flex: none;
      display: grid;
      place-items: center;
      width: 44px;
      height: 44px;
      border-radius: 14px;
      background: var(--mat-sys-primary-container);
      color: var(--mat-sys-on-primary-container);
    }

    .kpi__icon--alerta {
      background: var(--mat-sys-tertiary-container);
      color: var(--mat-sys-on-tertiary-container);
    }

    .kpi__icon--error {
      background: var(--mat-sys-error-container);
      color: var(--mat-sys-on-error-container);
    }

    .kpi__text {
      display: flex;
      flex-direction: column;
      line-height: 1.15;
    }

    .kpi__value {
      font-size: 26px;
      font-weight: 500;
      color: var(--mat-sys-on-surface);
    }

    .kpi__label {
      font: var(--mat-sys-body-medium);
      color: var(--mat-sys-on-surface-variant);
    }
  `,
})
export class LicenciasKpisComponent {
  private readonly dialog = inject(MatDialog);

  readonly licencias = input.required<LicenciaResumen[]>();

  protected readonly kpis = computed<Kpi[]>(() => {
    // "Por vencer" y "Vencidas" son estados distintos (la API ya los separa); el cálculo vive
    // centralizado en agregarLicencias (mismo usado por el homepage).
    const { asignadas, disponibles, porVencer, vencidas } = agregarLicencias(this.licencias());
    const kpis: Kpi[] = [
      { label: 'Asignadas', value: asignadas, icon: 'badge' },
      { label: 'Disponibles', value: disponibles, icon: 'verified_user' },
      { label: 'Por vencer', value: porVencer, icon: 'schedule', alerta: porVencer > 0 },
    ];
    // La tarjeta de vencidas sólo aparece si hay alguna (evita un "0 vencidas" permanente).
    if (vencidas > 0) {
      kpis.push({ label: 'Vencidas', value: vencidas, icon: 'error', error: true });
    }
    return kpis;
  });

  protected verDetalle(): void {
    this.dialog.open(LicenciasDetalleDialogComponent, {
      data: { licencias: this.licencias() },
      width: '600px',
      maxWidth: '95vw',
      autoFocus: false,
    });
  }
}
