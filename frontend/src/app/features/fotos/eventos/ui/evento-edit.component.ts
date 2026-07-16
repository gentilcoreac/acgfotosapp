import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import {
  FormField,
  applyEach,
  form,
  maxLength,
  pattern,
  required,
} from '@angular/forms/signals';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatDialogModule } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { EditComponentBase } from '../../../../shared/forms/edit-component-base';
import { TbiButtonComponent } from '../../../../shared/ui/tbi-button/tbi-button.component';
import { TbiCellInputComponent } from '../../../../shared/ui/tbi-cell-input/tbi-cell-input.component';
import { TbiDatePickerComponent } from '../../../../shared/ui/tbi-date-picker/tbi-date-picker.component';
import {
  TbiSelectComponent,
  TbiSelectOption,
} from '../../../../shared/ui/tbi-select/tbi-select.component';
import { TbiTextFieldComponent } from '../../../../shared/ui/tbi-text-field/tbi-text-field.component';
import { EventosService } from '../data/eventos.service';
import { ESTADO_EVENTO_LABEL, EstadoEvento, Evento } from '../domain/evento.model';

/**
 * Fila del catálogo en el form. El precio se edita como **string** (es lo que maneja
 * `tbi-cell-input`; admite coma o punto decimal) y se convierte en `toEntity`. El orden no se
 * edita: lo da la posición de la fila (flechas subir/bajar).
 */
interface TamanoPrecioRow {
  /** Id de la fila persistida; 0 = fila nueva (la API reconcilia por id). */
  id: number;
  nombre: string;
  precio: string;
  activo: boolean;
}

interface EventoFormModel {
  nombre: string;
  lugarOrganizacion: string;
  /** `yyyy-MM-dd` (shape de `tbi-date-picker`); vacío = sin fecha. */
  fecha: string;
  fechaExpiracion: string;
  estado: EstadoEvento;
  tamanos: TamanoPrecioRow[];
}

/** Precio editado (`"1.500,50"` no: solo `1500,50`/`1500.50`) → number para la API. */
function parsePrecio(value: string): number {
  return Number(value.replace(',', '.'));
}

// TODO (Fase 4 - i18n): textos en español por ahora.
@Component({
  selector: 'tbi-evento-edit',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FormField,
    MatDialogModule,
    MatButtonModule,
    MatCheckboxModule,
    MatIconModule,
    MatProgressBarModule,
    MatTooltipModule,
    TbiButtonComponent,
    TbiCellInputComponent,
    TbiDatePickerComponent,
    TbiSelectComponent,
    TbiTextFieldComponent,
  ],
  templateUrl: './evento-edit.component.html',
  styleUrl: './evento-edit.component.scss',
})
export class EventoEditComponent extends EditComponentBase<Evento, EventoFormModel> {
  private readonly service = inject(EventosService);
  protected readonly crud = this.service.crud;
  private loaded?: Evento;

  protected readonly estadoOptions: TbiSelectOption<EstadoEvento>[] = Object.entries(
    ESTADO_EVENTO_LABEL,
  ).map(([value, label]) => ({ value: Number(value) as EstadoEvento, label }));

  protected readonly model = signal<EventoFormModel>({
    nombre: '',
    lugarOrganizacion: '',
    fecha: '',
    fechaExpiracion: '',
    estado: 0,
    tamanos: [],
  });

  // Mismas reglas de forma que EventoInputDtoValidator (la API revalida). El precio se valida como
  // texto: dígitos con hasta 2 decimales (sin signo → no puede ser negativo).
  protected readonly form = form(this.model, (path) => {
    required(path.nombre, { message: 'Requerido' });
    maxLength(path.nombre, 200, { message: 'Máximo 200 caracteres' });
    maxLength(path.lugarOrganizacion, 200, { message: 'Máximo 200 caracteres' });
    applyEach(path.tamanos, (tamano) => {
      required(tamano.nombre, { message: 'Requerido' });
      maxLength(tamano.nombre, 50, { message: 'Máximo 50 caracteres' });
      required(tamano.precio, { message: 'Requerido' });
      pattern(tamano.precio, /^\d{1,10}([.,]\d{1,2})?$/, { message: 'Precio inválido' });
    });
  });

  protected override invalidSubmitMessage =
    'Completá los campos obligatorios resaltados (revisá también el catálogo de tamaños).';

  /** Agrega una fila nueva al final del catálogo (id 0 = alta para la API). */
  protected agregarTamano(): void {
    this.model.update((m) => ({
      ...m,
      tamanos: [...m.tamanos, { id: 0, nombre: '', precio: '', activo: true }],
    }));
    this.dirtyExtra.set(true);
  }

  /** Quita la fila: al no viajar en el update, la API la da de baja. */
  protected quitarTamano(index: number): void {
    this.model.update((m) => ({
      ...m,
      tamanos: m.tamanos.filter((_, i) => i !== index),
    }));
    this.dirtyExtra.set(true);
  }

  /** Mueve la fila un lugar (`delta` ±1); el orden persistido es la posición. */
  protected moverTamano(index: number, delta: -1 | 1): void {
    const destino = index + delta;
    this.model.update((m) => {
      if (destino < 0 || destino >= m.tamanos.length) {
        return m;
      }
      const tamanos = [...m.tamanos];
      [tamanos[index], tamanos[destino]] = [tamanos[destino], tamanos[index]];
      return { ...m, tamanos };
    });
    this.dirtyExtra.set(true);
  }

  protected toEntity(): Evento {
    const value = this.model();
    return {
      ...this.loaded,
      nombre: value.nombre.trim(),
      lugarOrganizacion: value.lugarOrganizacion.trim() || null,
      fecha: value.fecha || null,
      fechaExpiracion: value.fechaExpiracion || null,
      estado: value.estado,
      tamanosPrecios: value.tamanos.map((t, index) => ({
        id: t.id,
        nombre: t.nombre.trim(),
        precioUnitario: parsePrecio(t.precio),
        orden: index + 1,
        activo: t.activo,
      })),
    };
  }

  protected patchForm(entity: Evento): void {
    this.loaded = entity;
    this.model.set({
      nombre: entity.nombre,
      lugarOrganizacion: entity.lugarOrganizacion ?? '',
      fecha: entity.fecha?.split('T')[0] ?? '',
      fechaExpiracion: entity.fechaExpiracion?.split('T')[0] ?? '',
      estado: entity.estado,
      tamanos: [...(entity.tamanosPrecios ?? [])]
        .sort((a, b) => a.orden - b.orden)
        .map((t) => ({
          id: t.id,
          nombre: t.nombre,
          precio: String(t.precioUnitario).replace('.', ','),
          activo: t.activo,
        })),
    });
  }
}
