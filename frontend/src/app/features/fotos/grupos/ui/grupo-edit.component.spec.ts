import type { Mock, MockedObject } from 'vitest';
import { WritableSignal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { of } from 'rxjs';
import { EventosService } from '../../eventos/data/eventos.service';
import { GruposService } from '../data/grupos.service';
import { Grupo } from '../domain/grupo.model';
import { GrupoEditComponent } from './grupo-edit.component';

describe('GrupoEditComponent', () => {
  let fixture: ComponentFixture<GrupoEditComponent>;
  let save: Mock;
  let getById: Mock;
  let dialogRef: MockedObject<MatDialogRef<GrupoEditComponent, Grupo>>;

  async function setup(
    dialogData: { id?: number } = {},
    entity: Grupo | null = null,
  ): Promise<void> {
    save = vi
      .fn()
      .mockName('save')
      .mockImplementation((c: Grupo) => of({ ...c, id: c.id ?? 10 }));
    getById = vi.fn().mockName('getById').mockReturnValue(of(entity));
    dialogRef = {
      close: vi.fn().mockName('MatDialogRef.close'),
    } as unknown as MockedObject<MatDialogRef<GrupoEditComponent, Grupo>>;

    await TestBed.configureTestingModule({
      imports: [GrupoEditComponent],
      providers: [
        provideNoopAnimations(),
        { provide: MatDialogRef, useValue: dialogRef },
        { provide: MAT_DIALOG_DATA, useValue: dialogData },
        { provide: GruposService, useValue: { crud: { getById, save } } },
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

    fixture = TestBed.createComponent(GrupoEditComponent);
    fixture.detectChanges();
  }

  /** Modelo del form del componente (espeja el `GrupoFormModel` privado). */
  interface FormModel {
    eventoId: number | null;
    nombre: string;
    participantes: { id: number; nombre: string; codigoAcceso: string | null }[];
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

  it('guarda mapeando los participantes (fila nueva id 0, sin código todavía)', async () => {
    await setup();
    setModel({
      eventoId: 7,
      nombre: '6to A',
      participantes: [
        { id: 3, nombre: 'Ana Pérez', codigoAcceso: 'AAAA-1111' },
        { id: 0, nombre: ' Juan López ', codigoAcceso: null },
      ],
    });
    submit();

    expect(save).toHaveBeenCalled();
    const payload = vi.mocked(save).mock.lastCall![0] as Grupo;
    expect(payload.eventoId).toBe(7);
    expect(payload.nombre).toBe('6to A');
    expect(payload.participantes).toEqual([
      { id: 3, nombre: 'Ana Pérez', codigoAcceso: 'AAAA-1111' },
      { id: 0, nombre: 'Juan López', codigoAcceso: null },
    ]);
    expect(dialogRef.close).toHaveBeenCalled();
  });

  it('no guarda sin evento elegido o con un participante sin participante', async () => {
    await setup();
    setModel({
      eventoId: null,
      nombre: '6to A',
      participantes: [{ id: 0, nombre: '', codigoAcceso: null }],
    });
    submit();

    expect(save).not.toHaveBeenCalled();
    expect(fixture.componentInstance['errors']().length).toBeGreaterThan(0);
  });

  it('agregar/quitar participantes actualiza el modelo', async () => {
    await setup();
    fixture.componentInstance['agregarParticipante']();
    fixture.componentInstance['agregarParticipante']();
    expect(getModel().participantes.length).toBe(2);
    expect(getModel().participantes[0]).toEqual({ id: 0, nombre: '', codigoAcceso: null });

    fixture.componentInstance['quitarParticipante'](0);
    expect(getModel().participantes.length).toBe(1);
  });

  it('edición: carga la entidad con los códigos de acceso de cada participante', async () => {
    const grupo: Grupo = {
      id: 5,
      eventoId: 7,
      nombre: '6to A',
      participantes: [{ id: 1, nombre: 'Ana Pérez', codigoAcceso: 'AAAA-1111' }],
    };
    await setup({ id: 5 }, grupo);

    expect(getById).toHaveBeenCalledWith(5);
    expect(getModel().eventoId).toBe(7);
    // toMatchObject: Signal Forms agrega un Symbol de tracking a cada item del array del modelo.
    expect(getModel().participantes).toMatchObject([
      { id: 1, nombre: 'Ana Pérez', codigoAcceso: 'AAAA-1111' },
    ]);
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('AAAA-1111');
  });
});
