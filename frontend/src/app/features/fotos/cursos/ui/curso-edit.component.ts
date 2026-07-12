import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { FormField, applyEach, form, maxLength, required } from '@angular/forms/signals';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { catchError, map, of } from 'rxjs';
import { EditComponentBase } from '../../../../shared/forms/edit-component-base';
import { TbiButtonComponent } from '../../../../shared/ui/tbi-button/tbi-button.component';
import { TbiCellInputComponent } from '../../../../shared/ui/tbi-cell-input/tbi-cell-input.component';
import {
  TbiSelectComponent,
  TbiSelectOption,
} from '../../../../shared/ui/tbi-select/tbi-select.component';
import { TbiTextFieldComponent } from '../../../../shared/ui/tbi-text-field/tbi-text-field.component';
import { EventosService } from '../../eventos/data/eventos.service';
import { Evento } from '../../eventos/domain/evento.model';
import { CursosService } from '../data/cursos.service';
import { Curso } from '../domain/curso.model';

/**
 * Fila de álbum en el form. `codigoAcceso` es solo de salida (se muestra, no se edita): en las
 * filas nuevas queda `null` hasta que la API lo genere al guardar.
 */
interface AlbumRow {
  /** Id de la fila persistida; 0 = fila nueva (la API reconcilia por id y genera el código). */
  id: number;
  nombreAlumno: string;
  codigoAcceso: string | null;
}

interface CursoFormModel {
  eventoId: number | null;
  nombre: string;
  albumes: AlbumRow[];
}

// TODO (Fase 4 - i18n): textos en español por ahora.
@Component({
  selector: 'tbi-curso-edit',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FormField,
    MatDialogModule,
    MatButtonModule,
    MatIconModule,
    MatProgressBarModule,
    MatTooltipModule,
    TbiButtonComponent,
    TbiCellInputComponent,
    TbiSelectComponent,
    TbiTextFieldComponent,
  ],
  templateUrl: './curso-edit.component.html',
  styleUrl: './curso-edit.component.scss',
})
export class CursoEditComponent extends EditComponentBase<Curso, CursoFormModel> {
  private readonly service = inject(CursosService);
  private readonly eventosService = inject(EventosService);
  protected readonly crud = this.service.crud;
  private loaded?: Curso;

  /** Eventos del tenant para el select (lookup fijo: corre una vez al abrir el diálogo). */
  private readonly eventosResource = rxResource({
    stream: () =>
      this.eventosService.crud.getAll().pipe(
        map((result) => result.items),
        // el toast de error lo emite el errorInterceptor (global)
        catchError(() => of<Evento[]>([])),
      ),
  });
  protected readonly eventoOptions = computed<TbiSelectOption<number>[]>(() =>
    (this.eventosResource.value() ?? []).map((e) => ({ value: e.id ?? 0, label: e.nombre })),
  );

  protected readonly model = signal<CursoFormModel>({
    eventoId: null,
    nombre: '',
    albumes: [],
  });

  // Mismas reglas de forma que CursoInputDtoValidator (la API revalida).
  protected readonly form = form(this.model, (path) => {
    required(path.eventoId, { message: 'Requerido' });
    required(path.nombre, { message: 'Requerido' });
    maxLength(path.nombre, 100, { message: 'Máximo 100 caracteres' });
    applyEach(path.albumes, (album) => {
      required(album.nombreAlumno, { message: 'Requerido' });
      maxLength(album.nombreAlumno, 150, { message: 'Máximo 150 caracteres' });
    });
  });

  protected override invalidSubmitMessage =
    'Completá los campos obligatorios resaltados (revisá también los álbumes).';

  /** Agrega una fila nueva (id 0 = alta; la API le genera el código de acceso al guardar). */
  protected agregarAlbum(): void {
    this.model.update((m) => ({
      ...m,
      albumes: [...m.albumes, { id: 0, nombreAlumno: '', codigoAcceso: null }],
    }));
    this.dirtyExtra.set(true);
  }

  /** Quita la fila: al no viajar en el update, la API la da de baja (bloqueada si tiene fotos). */
  protected quitarAlbum(index: number): void {
    this.model.update((m) => ({
      ...m,
      albumes: m.albumes.filter((_, i) => i !== index),
    }));
    this.dirtyExtra.set(true);
  }

  protected toEntity(): Curso {
    const value = this.model();
    return {
      ...this.loaded,
      eventoId: value.eventoId ?? 0,
      nombre: value.nombre.trim(),
      albumes: value.albumes.map((a) => ({
        id: a.id,
        nombreAlumno: a.nombreAlumno.trim(),
        // Solo de salida: la API lo ignora en el input (viaja para no perder el shape).
        codigoAcceso: a.codigoAcceso,
      })),
    };
  }

  protected patchForm(entity: Curso): void {
    this.loaded = entity;
    this.model.set({
      eventoId: entity.eventoId,
      nombre: entity.nombre,
      albumes: (entity.albumes ?? []).map((a) => ({
        id: a.id,
        nombreAlumno: a.nombreAlumno,
        codigoAcceso: a.codigoAcceso ?? null,
      })),
    });
  }
}
