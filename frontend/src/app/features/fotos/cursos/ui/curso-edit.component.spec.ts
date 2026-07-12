import type { Mock, MockedObject } from 'vitest';
import { WritableSignal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { of } from 'rxjs';
import { EventosService } from '../../eventos/data/eventos.service';
import { CursosService } from '../data/cursos.service';
import { Curso } from '../domain/curso.model';
import { CursoEditComponent } from './curso-edit.component';

describe('CursoEditComponent', () => {
  let fixture: ComponentFixture<CursoEditComponent>;
  let save: Mock;
  let getById: Mock;
  let dialogRef: MockedObject<MatDialogRef<CursoEditComponent, Curso>>;

  async function setup(
    dialogData: { id?: number } = {},
    entity: Curso | null = null,
  ): Promise<void> {
    save = vi
      .fn()
      .mockName('save')
      .mockImplementation((c: Curso) => of({ ...c, id: c.id ?? 10 }));
    getById = vi.fn().mockName('getById').mockReturnValue(of(entity));
    dialogRef = {
      close: vi.fn().mockName('MatDialogRef.close'),
    } as unknown as MockedObject<MatDialogRef<CursoEditComponent, Curso>>;

    await TestBed.configureTestingModule({
      imports: [CursoEditComponent],
      providers: [
        provideNoopAnimations(),
        { provide: MatDialogRef, useValue: dialogRef },
        { provide: MAT_DIALOG_DATA, useValue: dialogData },
        { provide: CursosService, useValue: { crud: { getById, save } } },
        {
          provide: EventosService,
          useValue: {
            crud: {
              getAll: () =>
                of({ items: [{ id: 7, nombre: 'Graduación 2026', estado: 1 }], totalCount: 1 }),
            },
          },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(CursoEditComponent);
    fixture.detectChanges();
  }

  /** Modelo del form del componente (espeja el `CursoFormModel` privado). */
  interface FormModel {
    eventoId: number | null;
    nombre: string;
    albumes: { id: number; nombreAlumno: string; codigoAcceso: string | null }[];
  }

  // El modelo es protected; en el spec lo seteamos directo (no hay FormGroup).
  const setModel = (value: FormModel): void => {
    (fixture.componentInstance as unknown as { model: WritableSignal<FormModel> }).model.set(value);
    fixture.detectChanges();
  };

  const getModel = (): FormModel =>
    (fixture.componentInstance as unknown as { model: WritableSignal<FormModel> }).model();

  const submit = (): void => {
    (fixture.nativeElement.querySelector('form') as HTMLFormElement).dispatchEvent(
      new Event('submit'),
    );
    fixture.detectChanges();
  };

  it('guarda mapeando los álbumes (fila nueva id 0, sin código todavía)', async () => {
    await setup();
    setModel({
      eventoId: 7,
      nombre: '6to A',
      albumes: [
        { id: 3, nombreAlumno: 'Ana Pérez', codigoAcceso: 'AAAA-1111' },
        { id: 0, nombreAlumno: ' Juan López ', codigoAcceso: null },
      ],
    });
    submit();

    expect(save).toHaveBeenCalled();
    const payload = vi.mocked(save).mock.lastCall![0] as Curso;
    expect(payload.eventoId).toBe(7);
    expect(payload.nombre).toBe('6to A');
    expect(payload.albumes).toEqual([
      { id: 3, nombreAlumno: 'Ana Pérez', codigoAcceso: 'AAAA-1111' },
      { id: 0, nombreAlumno: 'Juan López', codigoAcceso: null },
    ]);
    expect(dialogRef.close).toHaveBeenCalled();
  });

  it('no guarda sin evento elegido o con un álbum sin alumno', async () => {
    await setup();
    setModel({
      eventoId: null,
      nombre: '6to A',
      albumes: [{ id: 0, nombreAlumno: '', codigoAcceso: null }],
    });
    submit();

    expect(save).not.toHaveBeenCalled();
    expect(fixture.componentInstance['errors']().length).toBeGreaterThan(0);
  });

  it('agregar/quitar álbumes actualiza el modelo', async () => {
    await setup();
    fixture.componentInstance['agregarAlbum']();
    fixture.componentInstance['agregarAlbum']();
    expect(getModel().albumes.length).toBe(2);
    expect(getModel().albumes[0]).toEqual({ id: 0, nombreAlumno: '', codigoAcceso: null });

    fixture.componentInstance['quitarAlbum'](0);
    expect(getModel().albumes.length).toBe(1);
  });

  it('edición: carga la entidad con los códigos de acceso de cada álbum', async () => {
    const curso: Curso = {
      id: 5,
      eventoId: 7,
      nombre: '6to A',
      albumes: [{ id: 1, nombreAlumno: 'Ana Pérez', codigoAcceso: 'AAAA-1111' }],
    };
    await setup({ id: 5 }, curso);

    expect(getById).toHaveBeenCalledWith(5);
    expect(getModel().eventoId).toBe(7);
    // toMatchObject: Signal Forms agrega un Symbol de tracking a cada item del array del modelo.
    expect(getModel().albumes).toMatchObject([
      { id: 1, nombreAlumno: 'Ana Pérez', codigoAcceso: 'AAAA-1111' },
    ]);
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('AAAA-1111');
  });
});
