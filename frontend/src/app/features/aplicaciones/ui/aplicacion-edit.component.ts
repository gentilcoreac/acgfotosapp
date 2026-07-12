import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormField, disabled, form, required } from '@angular/forms/signals';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule } from '@angular/material/dialog';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { EditComponentBase } from '../../../shared/forms/edit-component-base';
import { TbiButtonComponent } from '../../../shared/ui/tbi-button/tbi-button.component';
import { TbiTextFieldComponent } from '../../../shared/ui/tbi-text-field/tbi-text-field.component';
import { AplicacionesService } from '../data/aplicaciones.service';
import { Aplicacion } from '../domain/aplicacion.model';

/**
 * `iconoSelected` es UI-only (no viaja a la API): true = se define un nombre de Material Icon
 * (`icono`); false = se define una URL (`iconoUrl`). Se modela junto a la entidad porque Signal
 * Forms necesita que el campo del form exista en el modelo (a diferencia del FormGroup anterior,
 * que podía tener un control fuera del tipo de la entidad).
 */
type AplicacionFormModel = Aplicacion & { iconoSelected: boolean };

// TODO (Fase 4 - i18n): textos en español por ahora.
@Component({
  selector: 'tbi-aplicacion-edit',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FormField,
    MatDialogModule,
    MatButtonModule,
    MatProgressBarModule,
    MatSlideToggleModule,
    TbiTextFieldComponent,
    TbiButtonComponent,
  ],
  templateUrl: './aplicacion-edit.component.html',
  styleUrl: './aplicacion-edit.component.scss',
})
export class AplicacionEditComponent extends EditComponentBase<Aplicacion, AplicacionFormModel> {
  private readonly service = inject(AplicacionesService);
  protected readonly crud = this.service.crud;

  protected readonly model = signal<AplicacionFormModel>({
    nombre: '',
    codigo: '',
    activo: true,
    icono: null,
    iconoUrl: null,
    iconoSelected: true,
  });

  // Ícono opcional (la API acepta ambos en null); no se valida como requerido para no trabar el
  // guardado. Sólo nombre y código son obligatorios. El campo no elegido se deshabilita (no se
  // valida ni viaja como requerido) según el toggle `iconoSelected`.
  protected readonly form = form(this.model, (path) => {
    required(path.nombre, { message: 'Requerido' });
    required(path.codigo, { message: 'Requerido' });
    disabled(path.icono, { when: ({ valueOf }) => !valueOf(path.iconoSelected) });
    disabled(path.iconoUrl, { when: ({ valueOf }) => valueOf(path.iconoSelected) });
  });

  protected toEntity(): Aplicacion {
    const { iconoSelected, ...raw } = this.model();
    // Excluyentes: sólo viaja el elegido, el otro se nula.
    return {
      ...raw,
      icono: iconoSelected ? raw.icono : null,
      iconoUrl: iconoSelected ? null : raw.iconoUrl,
    };
  }

  protected patchForm(entity: Aplicacion): void {
    // Si la entidad tiene iconoUrl, arranca en modo URL; si no, modo nombre de ícono.
    const iconoSelected = !entity.iconoUrl;
    this.model.set({ ...entity, iconoSelected });
  }
}
