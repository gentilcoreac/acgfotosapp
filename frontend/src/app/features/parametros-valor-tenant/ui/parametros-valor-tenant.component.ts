import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
  signal,
} from '@angular/core';
import { rxResource, takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatTableModule } from '@angular/material/table';
import { MatTooltipModule } from '@angular/material/tooltip';
import { EMPTY, Observable, catchError, of, tap } from 'rxjs';
import { ConfirmData } from '../../../shared/feedback/confirm-dialog.component';
import { NotificationService } from '../../../shared/feedback/notification.service';
import { TbiCellInputComponent } from '../../../shared/ui/tbi-cell-input/tbi-cell-input.component';
import { TbiRowActionComponent } from '../../../shared/ui/tbi-row-action/tbi-row-action.component';
import {
  TbiSelectComponent,
  TbiSelectOption,
} from '../../../shared/ui/tbi-select/tbi-select.component';
import { lookupResource } from '../../../shared/util/lookup-resource';
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

  private readonly tenantsResource = lookupResource(() => this.service.getTenants(), []);
  protected readonly tenantOptions = computed(() =>
    (this.tenantsResource.value() ?? []).map(toTenantOption),
  );

  /** Señal de los `FormControl` (Reactive Forms, no Signal Forms acá) — alimenta los `rxResource`
   * dependientes de abajo. */
  private readonly tenantId = toSignal(this.tenantControl.valueChanges, {
    initialValue: this.tenantControl.value,
  });
  private readonly aplicacionId = toSignal(this.aplicacionControl.valueChanges, {
    initialValue: this.aplicacionControl.value,
  });

  /** Refetchea sola cuando cambia el tenant elegido (cancela el pedido en vuelo anterior). */
  private readonly aplicacionesResource = rxResource({
    params: () => this.tenantId() ?? undefined,
    stream: ({ params }) =>
      this.service.getAplicacionesPorTenant(params).pipe(catchError(() => of([]))),
  });
  protected readonly aplicaciones = computed(() => this.aplicacionesResource.value() ?? []);
  protected readonly aplicacionOptions = computed<TbiSelectOption<number>[]>(() =>
    this.aplicaciones().map((a) => ({ value: a.id, label: a.nombre })),
  );
  protected readonly loadingApps = computed(() => this.aplicacionesResource.isLoading());

  /** Refetchea sola cuando cambia tenant o aplicación (cancela el pedido en vuelo anterior). */
  private readonly parametrosResource = rxResource({
    params: () => {
      const tenantId = this.tenantId();
      const aplicacionId = this.aplicacionId();
      return tenantId != null && aplicacionId != null ? { tenantId, aplicacionId } : undefined;
    },
    stream: ({ params }) =>
      this.service
        .getParametros(params.tenantId, params.aplicacionId)
        .pipe(catchError(() => of([]))),
  });
  /** Signal propia (no un `computed` del resource): se edita en el momento tras guardar/restaurar,
   * sin depender de un refetch. */
  protected readonly rows = signal<ParametroValorRow[]>([]);
  protected readonly loading = computed(() => this.parametrosResource.isLoading());
  /** `true` una vez que se ejecutó una búsqueda (para diferenciar "vacío" de "todavía no buscó"). */
  protected readonly searched = signal(false);

  // Edición inline: id de la fila en edición + borrador del valor (texto/entero y booleano aparte).
  protected readonly editingId = signal<number | null>(null);
  protected readonly valorControl = new FormControl<string>('', { nonNullable: true });
  protected readonly boolEdit = signal(false);

  constructor() {
    // Vuelca el resultado del fetch de parámetros a la signal propia (editable localmente).
    effect(() => {
      const rows = this.parametrosResource.value();
      if (rows !== undefined) {
        this.rows.set(rows);
        this.searched.set(true);
      }
    });

    // Si el tenant tiene una sola aplicación, la autoselecciona (dispara el fetch de parámetros
    // solo, vía `aplicacionId`) — comparado contra el valor VIGENTE del control, no un flag
    // "consumido" (mismo criterio que toda la migración a rxResource).
    effect(() => {
      const apps = this.aplicacionesResource.value();
      if (apps && apps.length === 1 && this.aplicacionControl.value !== apps[0].id) {
        this.aplicacionControl.setValue(apps[0].id);
      }
    });

    // Habilita el selector de aplicación apenas hay una respuesta (éxito o fail-open a vacío).
    effect(() => {
      if (this.tenantId() != null && this.aplicacionesResource.value() !== undefined) {
        this.aplicacionControl.enable({ emitEvent: false });
      }
    });

    // Cambio de tenant: resetea aplicación/grilla (estado de UI, no fetch — `aplicacionesResource`
    // refetchea sola vía `tenantId`).
    this.tenantControl.valueChanges.pipe(takeUntilDestroyed()).subscribe((tenantId) => {
      this.resetSeleccion();
      if (tenantId == null) {
        this.aplicacionControl.disable({ emitEvent: false });
      }
    });

    // Cambio de aplicación: limpia la edición en curso; si se deselecciona, limpia la grilla
    // (`parametrosResource` refetchea sola vía `aplicacionId` cuando se elige una).
    this.aplicacionControl.valueChanges.pipe(takeUntilDestroyed()).subscribe((aplicacionId) => {
      this.editingId.set(null);
      if (aplicacionId == null) {
        this.rows.set([]);
        this.searched.set(false);
      }
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

  /** Actualiza una fila inmutablemente (el resto de la grilla no se re-renderiza de más). */
  private patchRow(parametroId: number, patch: Partial<ParametroValorRow>): void {
    this.rows.update((list) => list.map((r) => (r.id === parametroId ? { ...r, ...patch } : r)));
  }

  /** Limpia aplicación y grilla al cambiar de tenant. */
  private resetSeleccion(): void {
    this.aplicacionControl.setValue(null, { emitEvent: false });
    this.rows.set([]);
    this.searched.set(false);
    this.editingId.set(null);
  }
}
