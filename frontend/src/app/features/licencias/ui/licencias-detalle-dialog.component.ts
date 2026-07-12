import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatTableModule } from '@angular/material/table';
import { TbiStatusChipComponent } from '../../../shared/ui/tbi-status-chip/tbi-status-chip.component';
import { LicenciaResumen } from '../domain/licencia-resumen.model';
import { estadoVigencia } from '../domain/licencia-metrics';

/** Datos que recibe el diálogo: el resumen ya cargado por el padre (no vuelve a pedir a la API). */
export interface LicenciasDetalleDialogData {
  licencias: LicenciaResumen[];
}

/**
 * Diálogo con el detalle de licencias del tenant (paridad con `licencias-view` de Budgeting):
 * por cada tipo, Total / Asignadas / Disponibles / Vencimiento. La vigencia se muestra con un
 * `tbi-status-chip` (vencida = error, por vencer = warning, vigente = success). Recibe el resumen
 * ya cargado por el indicador que lo abre; no hace fetch propio.
 */
@Component({
  selector: 'tbi-licencias-detalle-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    MatDialogModule,
    MatButtonModule,
    MatIconModule,
    MatTableModule,
    TbiStatusChipComponent,
  ],
  template: `
    <h2 mat-dialog-title>Licencias disponibles</h2>
    <mat-dialog-content>
      <table mat-table [dataSource]="licencias" class="licencias-table">
        <ng-container matColumnDef="descripcion">
          <th mat-header-cell *matHeaderCellDef>Licencia</th>
          <td mat-cell *matCellDef="let l">{{ l.descripcion }}</td>
        </ng-container>

        <ng-container matColumnDef="cantidadTotal">
          <th mat-header-cell *matHeaderCellDef class="num">Total</th>
          <td mat-cell *matCellDef="let l" class="num">{{ l.cantidadTotal }}</td>
        </ng-container>

        <ng-container matColumnDef="cantidadAsignada">
          <th mat-header-cell *matHeaderCellDef class="num">Asignadas</th>
          <td mat-cell *matCellDef="let l" class="num">{{ l.cantidadAsignada }}</td>
        </ng-container>

        <ng-container matColumnDef="cantidadDisponible">
          <th mat-header-cell *matHeaderCellDef class="num">Disponibles</th>
          <td mat-cell *matCellDef="let l" class="num">{{ l.cantidadDisponible }}</td>
        </ng-container>

        <ng-container matColumnDef="vencimiento">
          <th mat-header-cell *matHeaderCellDef>Vencimiento</th>
          <td mat-cell *matCellDef="let l">
            <div class="venc">
              <span>{{ fecha(l) }}</span>
              <tbi-status-chip [label]="estado(l).label" [tone]="estado(l).tone" />
            </div>
          </td>
        </ng-container>

        <tr mat-header-row *matHeaderRowDef="columnas"></tr>
        <tr mat-row *matRowDef="let row; columns: columnas"></tr>
      </table>

      @if (licencias.length === 0) {
        <p class="empty">El tenant no tiene licencias contratadas.</p>
      }
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button matButton mat-dialog-close>Cerrar</button>
    </mat-dialog-actions>
  `,
  styles: `
    .licencias-table {
      width: 100%;
      background: transparent;
    }

    .num {
      text-align: right;
    }

    .venc {
      display: flex;
      align-items: center;
      gap: 10px;
    }

    .empty {
      color: var(--mat-sys-on-surface-variant);
      text-align: center;
      padding: 1.5rem 0;
    }
  `,
})
export class LicenciasDetalleDialogComponent {
  protected readonly licencias = inject<LicenciasDetalleDialogData>(MAT_DIALOG_DATA).licencias;
  protected readonly columnas = [
    'descripcion',
    'cantidadTotal',
    'cantidadAsignada',
    'cantidadDisponible',
    'vencimiento',
  ];

  protected fecha(l: LicenciaResumen): string {
    return l.expirationDate ? new Date(l.expirationDate).toLocaleDateString() : '—';
  }

  protected estado(l: LicenciaResumen): { label: string; tone: 'success' | 'warning' | 'error' } {
    switch (estadoVigencia(l)) {
      case 'vencida':
        return { label: 'Vencida', tone: 'error' };
      case 'por-vencer':
        return { label: 'Por vencer', tone: 'warning' };
      default:
        return { label: 'Vigente', tone: 'success' };
    }
  }
}
