import type { Mock, MockedObject } from 'vitest';
import { WritableSignal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { of } from 'rxjs';
import { EventosService } from '../data/eventos.service';
import { Evento } from '../domain/evento.model';
import { EventoEditComponent } from './evento-edit.component';

describe('EventoEditComponent', () => {
  let fixture: ComponentFixture<EventoEditComponent>;
  let save: Mock;
  let getById: Mock;
  let dialogRef: MockedObject<MatDialogRef<EventoEditComponent, Evento>>;

  async function setup(
    dialogData: { id?: number } = {},
    entity: Evento | null = null,
  ): Promise<void> {
    save = vi
      .fn()
      .mockName('save')
      .mockImplementation((e: Evento) => of({ ...e, id: e.id ?? 10 }));
    getById = vi.fn().mockName('getById').mockReturnValue(of(entity));
    dialogRef = {
      close: vi.fn().mockName('MatDialogRef.close'),
    } as unknown as MockedObject<MatDialogRef<EventoEditComponent, Evento>>;

    await TestBed.configureTestingModule({
      imports: [EventoEditComponent],
      providers: [
        provideNoopAnimations(),
        { provide: MatDialogRef, useValue: dialogRef },
        { provide: MAT_DIALOG_DATA, useValue: dialogData },
        { provide: EventosService, useValue: { crud: { getById, save } } },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(EventoEditComponent);
    fixture.detectChanges();
  }

  /** Modelo del form del componente (espeja el `EventoFormModel` privado). */
  interface FormModel {
    nombre: string;
    lugarOrganizacion: string;
    fecha: string;
    fechaExpiracion: string;
    estado: number;
    tamanos: { id: number; nombre: string; precio: string; activo: boolean }[];
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

  it('guarda mapeando el catálogo con orden por posición y precio numérico', async () => {
    await setup();
    setModel({
      nombre: 'Graduación 2026',
      lugarOrganizacion: '',
      fecha: '2026-11-20',
      fechaExpiracion: '',
      estado: 0,
      tamanos: [
        { id: 3, nombre: '13x18', precio: '2500,50', activo: true },
        { id: 0, nombre: '10x15', precio: '1500', activo: false },
      ],
    });
    submit();

    expect(save).toHaveBeenCalled();
    const payload = vi.mocked(save).mock.lastCall![0] as Evento;
    expect(payload.nombre).toBe('Graduación 2026');
    expect(payload.lugarOrganizacion).toBeNull();
    expect(payload.fecha).toBe('2026-11-20');
    expect(payload.fechaExpiracion).toBeNull();
    expect(payload.tamanosPrecios).toEqual([
      { id: 3, nombre: '13x18', precioUnitario: 2500.5, orden: 1, activo: true },
      { id: 0, nombre: '10x15', precioUnitario: 1500, orden: 2, activo: false },
    ]);
    expect(dialogRef.close).toHaveBeenCalled();
  });

  it('no guarda si una fila del catálogo está incompleta', async () => {
    await setup();
    setModel({
      nombre: 'Graduación 2026',
      lugarOrganizacion: '',
      fecha: '',
      fechaExpiracion: '',
      estado: 0,
      tamanos: [{ id: 0, nombre: '', precio: 'abc', activo: true }],
    });
    submit();

    expect(save).not.toHaveBeenCalled();
    expect(fixture.componentInstance['errors']().length).toBeGreaterThan(0);
  });

  it('agregar/mover/quitar mantienen el catálogo y el orden esperado', async () => {
    await setup();
    fixture.componentInstance['agregarTamano']();
    fixture.componentInstance['agregarTamano']();
    setModel({
      ...getModel(),
      tamanos: getModel().tamanos.map((t, i) => ({ ...t, nombre: `T${i + 1}`, precio: '100' })),
    });

    fixture.componentInstance['moverTamano'](1, -1);
    expect(getModel().tamanos.map((t) => t.nombre)).toEqual(['T2', 'T1']);

    fixture.componentInstance['quitarTamano'](0);
    expect(getModel().tamanos.map((t) => t.nombre)).toEqual(['T1']);
  });

  it('edición: carga la entidad y ordena el catálogo por orden', async () => {
    const evento: Evento = {
      id: 5,
      nombre: 'Egresados',
      lugarOrganizacion: 'Belgrano',
      fecha: '2026-11-20T00:00:00',
      fechaExpiracion: null,
      estado: 1,
      tamanosPrecios: [
        { id: 2, nombre: '13x18', precioUnitario: 2500.5, orden: 2, activo: true },
        { id: 1, nombre: '10x15', precioUnitario: 1500, orden: 1, activo: true },
      ],
    };
    await setup({ id: 5 }, evento);

    expect(getById).toHaveBeenCalledWith(5);
    expect(getModel().nombre).toBe('Egresados');
    expect(getModel().fecha).toBe('2026-11-20');
    expect(getModel().tamanos.map((t) => t.nombre)).toEqual(['10x15', '13x18']);
    expect(getModel().tamanos[1].precio).toBe('2500,5');
  });
});
