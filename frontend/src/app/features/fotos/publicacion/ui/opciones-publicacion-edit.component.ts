import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormField, form, maxLength, required, validate } from '@angular/forms/signals';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatDialogModule } from '@angular/material/dialog';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { EditComponentBase } from '../../../../shared/forms/edit-component-base';
import { TbiButtonComponent } from '../../../../shared/ui/tbi-button/tbi-button.component';
import { TbiSliderComponent } from '../../../../shared/ui/tbi-slider/tbi-slider.component';
import { TbiTextFieldComponent } from '../../../../shared/ui/tbi-text-field/tbi-text-field.component';
import { OpcionesPublicacionService } from '../data/opciones-publicacion.service';
import { OpcionesPublicacion } from '../domain/opciones-publicacion.model';

type OpcionesPublicacionFormModel = Pick<
  OpcionesPublicacion,
  'nombre' | 'esDefault' | 'ladoMayorPreview' | 'ladoMayorThumb' | 'calidad'
>;

// TODO (Fase 4 - i18n): textos en español por ahora.
@Component({
  selector: 'tbi-opciones-publicacion-edit',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FormField,
    MatDialogModule,
    MatButtonModule,
    MatCheckboxModule,
    MatProgressBarModule,
    TbiButtonComponent,
    TbiSliderComponent,
    TbiTextFieldComponent,
  ],
  templateUrl: './opciones-publicacion-edit.component.html',
  styleUrl: './opciones-publicacion-edit.component.scss',
})
export class OpcionesPublicacionEditComponent extends EditComponentBase<
  OpcionesPublicacion,
  OpcionesPublicacionFormModel
> {
  private readonly service = inject(OpcionesPublicacionService);
  protected readonly crud = this.service.crud;
  private loaded?: OpcionesPublicacion;

  protected readonly model = signal<OpcionesPublicacionFormModel>({
    nombre: '',
    esDefault: false,
    ladoMayorPreview: 1600,
    ladoMayorThumb: 600,
    calidad: 80,
  });

  // Mismos rangos que OpcionesPublicacionDtoValidator (la API revalida).
  protected readonly form = form(this.model, (path) => {
    required(path.nombre, { message: 'Requerido' });
    maxLength(path.nombre, 100, { message: 'Máximo 100 caracteres' });
    validate(path.ladoMayorPreview, ({ value }) =>
      value() >= 100 && value() <= 4000
        ? undefined
        : { kind: 'rango', message: 'El lado mayor del preview debe estar entre 100 y 4000px.' },
    );
    validate(path.ladoMayorThumb, ({ value }) =>
      value() >= 50 && value() <= 2000
        ? undefined
        : { kind: 'rango', message: 'El lado mayor del thumbnail debe estar entre 50 y 2000px.' },
    );
    validate(path.calidad, ({ value }) =>
      value() >= 1 && value() <= 100
        ? undefined
        : { kind: 'rango', message: 'La calidad debe estar entre 1 y 100.' },
    );
  });

  protected toEntity(): OpcionesPublicacion {
    const value = this.model();
    return {
      ...this.loaded,
      nombre: value.nombre.trim(),
      esDefault: value.esDefault,
      ladoMayorPreview: value.ladoMayorPreview,
      ladoMayorThumb: value.ladoMayorThumb,
      calidad: value.calidad,
    };
  }

  protected patchForm(entity: OpcionesPublicacion): void {
    this.loaded = entity;
    this.model.set({
      nombre: entity.nombre,
      esDefault: entity.esDefault,
      ladoMayorPreview: entity.ladoMayorPreview,
      ladoMayorThumb: entity.ladoMayorThumb,
      calidad: entity.calidad,
    });
  }
}
