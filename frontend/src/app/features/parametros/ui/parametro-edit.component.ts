import { ChangeDetectionStrategy, Component, effect, inject, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { FormField, disabled, form, maxLength, required } from '@angular/forms/signals';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule } from '@angular/material/dialog';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { catchError, of } from 'rxjs';
import { EditComponentBase } from '../../../shared/forms/edit-component-base';
import { TbiButtonComponent } from '../../../shared/ui/tbi-button/tbi-button.component';
import {
  TbiSelectComponent,
  TbiSelectOption,
} from '../../../shared/ui/tbi-select/tbi-select.component';
import { TbiTextFieldComponent } from '../../../shared/ui/tbi-text-field/tbi-text-field.component';
import { ParametrosService } from '../data/parametros.service';
import { Parametro, TIPO_DATO_OPTIONS } from '../domain/parametro.model';

interface ParametroFormModel {
  nombre: string;
  valor: string;
  descripcion: string;
  tipoDato: number | null;
  aplicacionId: number | null;
  /** Nullable en la API; el ABM original lo exige y mantenemos ese criterio. */
  permisoId: number | null;
}

// TODO (Fase 4 - i18n): textos en español por ahora.
@Component({
  selector: 'tbi-parametro-edit',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FormField,
    MatDialogModule,
    MatButtonModule,
    MatProgressBarModule,
    TbiTextFieldComponent,
    TbiSelectComponent,
    TbiButtonComponent,
  ],
  templateUrl: './parametro-edit.component.html',
  styleUrl: './parametro-edit.component.scss',
})
export class ParametroEditComponent extends EditComponentBase<Parametro, ParametroFormModel> {
  private readonly service = inject(ParametrosService);
  protected readonly crud = this.service.crud;
  private loaded?: Parametro;

  protected readonly tipoDatoOptions = TIPO_DATO_OPTIONS;
  protected readonly aplicacionOptions = signal<TbiSelectOption<number>[]>([]);
  protected readonly permisoOptions = signal<TbiSelectOption<number>[]>([]);

  protected readonly model = signal<ParametroFormModel>({
    nombre: '',
    valor: '',
    descripcion: '',
    tipoDato: null,
    aplicacionId: null,
    permisoId: null,
  });

  // Orden de campos: el permiso depende de la aplicación elegida en este form, por eso van
  // adyacentes y al final (aplicación → permiso); el permiso no lista (deshabilitado) hasta
  // elegir aplicación. El disabled excluye su `required` de la validez (paridad con el original).
  protected readonly form = form(this.model, (path) => {
    required(path.nombre, { message: 'Requerido' });
    maxLength(path.nombre, 100, { message: 'Máximo 100 caracteres' });
    required(path.descripcion, { message: 'Requerido' });
    maxLength(path.descripcion, 500, { message: 'Máximo 500 caracteres' });
    required(path.tipoDato, { message: 'Requerido' });
    required(path.aplicacionId, { message: 'Requerido' });
    required(path.permisoId, { message: 'Requerido' });
    disabled(path.permisoId, { when: ({ valueOf }) => valueOf(path.aplicacionId) == null });
  });

  /** Refetchea sola cuando cambia la aplicación elegida (cancela el pedido en vuelo anterior). */
  private readonly permisosResource = rxResource({
    params: () => this.form.aplicacionId().value(),
    stream: ({ params: aplicacionId }) =>
      aplicacionId == null
        ? of(null)
        : this.service.getPermisosDeAplicacion(aplicacionId).pipe(
            // el toast de error lo emite el errorInterceptor (global)
            catchError(() => of(undefined)),
          ),
  });

  /** El patch inicial (edición) arma esta aplicación ANTES de volcar el modelo: cuando el effect
   * corra y la aplicación ACTUAL siga siendo esta, no poda `permisoId` — se compara contra el valor
   * vigente en vez de un booleano "consumido" (un booleano es frágil si dos cambios de aplicación
   * ocurren antes de que el effect llegue a correr para el primero: se coalescen en una sola
   * ejecución y el booleano se consumiría para el cambio equivocado). `undefined` = sin supresión
   * armada (distinto de `null`, que es "sin aplicación elegida", un id válido). */
  private suppressClearForAplicacionId: number | null | undefined = undefined;

  constructor() {
    super();
    // Los permisos dependen de la aplicación: al cambiarla se recarga su lista y se limpia el
    // permiso elegido (salvo el patch inicial, ver `suppressClearForAplicacionId`).
    effect(() => {
      const currentAplicacionId = this.form.aplicacionId().value();
      const permisos = this.permisosResource.value();
      if (permisos === undefined) {
        return;
      }
      const suppress = this.suppressClearForAplicacionId === currentAplicacionId;
      this.suppressClearForAplicacionId = undefined;
      if (!suppress) {
        this.model.update((m) => ({ ...m, permisoId: null }));
      }
      this.permisoOptions.set(
        permisos === null ? [] : permisos.map((p) => ({ value: p.id, label: p.nombre })),
      );
    });
  }

  override ngOnInit(): void {
    super.ngOnInit();
    this.service
      .getAplicaciones()
      .subscribe((aplicaciones) =>
        this.aplicacionOptions.set(aplicaciones.map((a) => ({ value: a.id, label: a.nombre }))),
      );
  }

  protected toEntity(): Parametro {
    // Mapeo explícito (no `...this.model()`): si el modelo crea campos UI-only, no viajan a la API.
    const value = this.model();
    return {
      ...this.loaded,
      nombre: value.nombre,
      valor: value.valor,
      descripcion: value.descripcion,
      tipoDato: value.tipoDato,
      aplicacionId: value.aplicacionId,
      permisoId: value.permisoId,
    } as Parametro;
  }

  protected patchForm(entity: Parametro): void {
    this.loaded = entity;
    // Arma la supresión para esta aplicación ANTES de volcar el modelo (el effect no poda permisoId)
    // — `permisosResource` refetchea sola.
    this.suppressClearForAplicacionId = entity.aplicacionId ?? null;
    this.model.set({
      nombre: entity.nombre ?? '',
      valor: entity.valor ?? '',
      descripcion: entity.descripcion ?? '',
      tipoDato: entity.tipoDato ?? null,
      aplicacionId: entity.aplicacionId ?? null,
      permisoId: entity.permisoId ?? null,
    });
  }
}
