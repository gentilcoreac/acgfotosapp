import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatTableModule } from '@angular/material/table';
import { MatTooltipModule } from '@angular/material/tooltip';
import { EMPTY, Observable, catchError, tap } from 'rxjs';
import { ConfirmData } from '../../../shared/feedback/confirm-dialog.component';
import { NotificationService } from '../../../shared/feedback/notification.service';
import { TbiCellInputComponent } from '../../../shared/ui/tbi-cell-input/tbi-cell-input.component';
import { TbiRowActionComponent } from '../../../shared/ui/tbi-row-action/tbi-row-action.component';
import {
  TbiSelectComponent,
  TbiSelectOption,
} from '../../../shared/ui/tbi-select/tbi-select.component';
import { ParametrosValorTenantService } from '../data/parametros-valor-tenant.service';
import {
  ParametroValorRow,
  ParametroValorTenantInput,
  TIPO_DATO,
  displayValor,
  esBooleanoTrue,
  toTenantOption,
} from '../domain/parametro-valor-tenant.model';

/**
 * Customización de parámetros por tenant (root-only). Se elige un tenant y una de sus aplicaciones
 * y se listan los parámetros con su valor **efectivo**; cada fila se edita **inline** (como el ABM
 * original, pero con nuestros átomos: `tbi-cell-input`, `mat-slide-toggle`, `tbi-row-action` con
 * spinner). El lápiz abre la edición de la fila; el check confirma (upsert del override); el
 * restaurar borra el override y vuelve al valor por defecto (con confirmación).
 *
 * Sólo una fila se edita a la vez (`editingId`). El control del valor depende del `tipoDato`:
 * texto/entero → `tbi-cell-input` (compacto, no agranda la fila); booleano → `mat-slide-toggle`.
 */
// TODO (Fase 4 - i18n): textos en español por ahora.
@Component({
  selector: 'tbi-parametros-valor-tenant',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatIconModule,
    MatTableModule,
    MatSlideToggleModule,
    MatProgressBarModule,
    MatTooltipModule,
    TbiSelectComponent,
    TbiCellInputComponent,
    TbiRowActionComponent,
  ],
  templateUrl: './parametros-valor-tenant.component.html',
  styleUrl: './parametros-valor-tenant.component.scss',
})
export class ParametrosValorTenantComponent {
  private readonly service = inject(ParametrosValorTenantService);
  private readonly notify = inject(NotificationService);

  protected readonly TIPO_DATO = TIPO_DATO;
  protected readonly displayValor = displayValor;
  protected readonly displayedColumns = ['id', 'nombre', 'descripcion', 'valor', 'acciones'];

  // Selectores (reactive forms; la app arranca deshabilitada hasta elegir tenant).
  protected readonly tenantControl = new FormControl<number | null>(null);
  protected readonly aplicacionControl = new FormControl<number | null>({
    value: null,
    disabled: true,
  });

  protected readonly tenantOptions = signal<TbiSelectOption<number>[]>([]);
  protected readonly aplicaciones = signal<{ id: number; nombre: string }[]>([]);
  protected readonly aplicacionOptions = computed<TbiSelectOption<number>[]>(() =>
    this.aplicaciones().map((a) => ({ value: a.id, label: a.nombre })),
  );

  protected readonly rows = signal<ParametroValorRow[]>([]);
  protected readonly loading = signal(false);
  protected readonly loadingApps = signal(false);
  /** `true` una vez que se ejecutó una búsqueda (para diferenciar "vacío" de "todavía no buscó"). */
  protected readonly searched = signal(false);

  // Edición inline: id de la fila en edición + borrador del valor (texto/entero y booleano aparte).
  protected readonly editingId = signal<number | null>(null);
  protected readonly valorControl = new FormControl<string>('', { nonNullable: true });
  protected readonly boolEdit = signal(false);

  constructor() {
    this.service.getTenants().subscribe({
      next: (tenants) => this.tenantOptions.set(tenants.map(toTenantOption)),
      // el toast de error lo emite el errorInterceptor (global)
      error: () => undefined,
    });

    // Cambio de tenant: resetea aplicación/grilla y recarga las aplicaciones habilitadas del tenant.
    this.tenantControl.valueChanges.pipe(takeUntilDestroyed()).subscribe((tenantId) => {
      this.resetSeleccion();
      if (tenantId == null) {
        this.aplicacionControl.disable({ emitEvent: false });
        return;
      }
      this.loadAplicaciones(tenantId);
    });

    // Cambio de aplicación: con tenant + app elegidos, carga los parámetros.
    this.aplicacionControl.valueChanges.pipe(takeUntilDestroyed()).subscribe((aplicacionId) => {
      this.editingId.set(null);
      const tenantId = this.tenantControl.value;
      if (tenantId == null || aplicacionId == null) {
        this.rows.set([]);
        this.searched.set(false);
        return;
      }
      this.loadParametros(tenantId, aplicacionId);
    });
  }

  /** Inicia la edición inline de una fila (precarga el borrador según el tipo de dato). */
  protected startEdit(row: ParametroValorRow): void {
    this.editingId.set(row.id);
    if (row.tipoDato === TIPO_DATO.Booleano) {
      this.boolEdit.set(esBooleanoTrue(row.valor));
    } else {
      this.valorControl.setValue(row.valor ?? '');
    }
  }

  protected cancelEdit(): void {
    this.editingId.set(null);
  }

  /**
   * Thunk de guardado para `tbi-row-action` (muestra spinner mientras la API responde). Hace upsert
   * del override; al confirmar actualiza la fila con el id del override y sale del modo edición.
   */
  protected saveFn(row: ParametroValorRow): () => Observable<unknown> {
    return () => {
      const valor =
        row.tipoDato === TIPO_DATO.Booleano
          ? this.boolEdit()
            ? 'true'
            : 'false'
          : this.valorControl.value;
      const input: ParametroValorTenantInput = {
        id: row.parametroValorId ?? undefined,
        tenantId: this.tenantControl.value as number,
        parametroId: row.id,
        valor,
      };
      return this.service.crud.save(input).pipe(
        tap((saved) => {
          this.patchRow(row.id, { valor, parametroValorId: saved.id ?? row.parametroValorId });
          this.editingId.set(null);
          this.notify.success('Valor personalizado guardado.');
        }),
        catchError(() => EMPTY),
      );
    };
  }

  /** Datos del diálogo de confirmación del restaurar (lo abre `tbi-row-action`). */
  protected restoreConfirm(row: ParametroValorRow): ConfirmData {
    return {
      title: 'Restaurar valor por defecto',
      message: `El valor personalizado de "${row.nombre}" se reemplazará por el valor por defecto.`,
      confirmLabel: 'Restaurar',
    };
  }

  /**
   * Thunk de restauración para `tbi-row-action`: borra el override y deja la fila con el valor por
   * defecto. Sólo aplica si la fila tiene un override (`parametroValorId`).
   */
  protected restoreFn(row: ParametroValorRow): () => Observable<unknown> {
    return () => {
      if (row.parametroValorId == null) {
        return EMPTY;
      }
      return this.service.crud.delete(row.parametroValorId).pipe(
        tap(() => {
          this.patchRow(row.id, {
            valor: row.parametroValorDefaultValue,
            parametroValorId: null,
          });
          this.editingId.set(null);
          this.notify.success('Valor restaurado al valor por defecto.');
        }),
        catchError(() => EMPTY),
      );
    };
  }

  private loadAplicaciones(tenantId: number): void {
    this.loadingApps.set(true);
    this.service.getAplicacionesPorTenant(tenantId).subscribe({
      next: (apps) => {
        this.aplicaciones.set(apps);
        this.loadingApps.set(false);
        this.aplicacionControl.enable({ emitEvent: false });
        // Si el tenant tiene una sola aplicación, la elegimos y disparamos la carga (como el original).
        if (apps.length === 1) {
          this.aplicacionControl.setValue(apps[0].id);
        }
      },
      error: () => {
        this.loadingApps.set(false);
        this.aplicaciones.set([]);
        this.aplicacionControl.disable({ emitEvent: false });
      },
    });
  }

  private loadParametros(tenantId: number, aplicacionId: number): void {
    this.loading.set(true);
    this.searched.set(false);
    this.service.getParametros(tenantId, aplicacionId).subscribe({
      next: (rows) => {
        this.rows.set(rows);
        this.loading.set(false);
        this.searched.set(true);
      },
      error: () => {
        this.rows.set([]);
        this.loading.set(false);
        this.searched.set(true);
      },
    });
  }

  /** Actualiza una fila inmutablemente (el resto de la grilla no se re-renderiza de más). */
  private patchRow(parametroId: number, patch: Partial<ParametroValorRow>): void {
    this.rows.update((list) => list.map((r) => (r.id === parametroId ? { ...r, ...patch } : r)));
  }

  /** Limpia aplicación y grilla al cambiar de tenant. */
  private resetSeleccion(): void {
    this.aplicacionControl.setValue(null, { emitEvent: false });
    this.aplicaciones.set([]);
    this.rows.set([]);
    this.searched.set(false);
    this.editingId.set(null);
  }
}
