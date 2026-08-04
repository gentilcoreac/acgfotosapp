import type { Mock, MockedObject } from 'vitest';
import { WritableSignal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { of } from 'rxjs';
import { OpcionesPublicacionService } from '../data/opciones-publicacion.service';
import { OpcionesPublicacion } from '../domain/opciones-publicacion.model';
import { OpcionesPublicacionEditComponent } from './opciones-publicacion-edit.component';

describe('OpcionesPublicacionEditComponent', () => {
  let fixture: ComponentFixture<OpcionesPublicacionEditComponent>;
  let save: Mock;
  let getById: Mock;
  let dialogRef: MockedObject<MatDialogRef<OpcionesPublicacionEditComponent, OpcionesPublicacion>>;

  async function setup(
    dialogData: { id?: number } = {},
    entity: OpcionesPublicacion | null = null,
  ): Promise<void> {
    save = vi
      .fn()
      .mockName('save')
      .mockImplementation((e: OpcionesPublicacion) => of({ ...e, id: e.id ?? 10 }));
    getById = vi.fn().mockName('getById').mockReturnValue(of(entity));
    dialogRef = {
      close: vi.fn().mockName('MatDialogRef.close'),
    } as unknown as MockedObject<MatDialogRef<OpcionesPublicacionEditComponent, OpcionesPublicacion>>;

    await TestBed.configureTestingModule({
      imports: [OpcionesPublicacionEditComponent],
      providers: [
        provideNoopAnimations(),
        { provide: MatDialogRef, useValue: dialogRef },
        { provide: MAT_DIALOG_DATA, useValue: dialogData },
        { provide: OpcionesPublicacionService, useValue: { crud: { getById, save } } },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(OpcionesPublicacionEditComponent);
    fixture.detectChanges();
  }

  interface FormModel {
    nombre: string;
    esDefault: boolean;
    ladoMayorPreview: number;
    ladoMayorThumb: number;
    calidad: number;
  }

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

  it('guarda con los valores del form', async () => {
    await setup();
    setModel({
      nombre: 'Publicación estándar',
      esDefault: true,
      ladoMayorPreview: 1600,
      ladoMayorThumb: 600,
      calidad: 80,
    });
    submit();

    expect(save).toHaveBeenCalled();
    const payload = vi.mocked(save).mock.lastCall![0] as OpcionesPublicacion;
    expect(payload.nombre).toBe('Publicación estándar');
    expect(payload.esDefault).toBe(true);
    expect(payload.ladoMayorPreview).toBe(1600);
    expect(payload.ladoMayorThumb).toBe(600);
    expect(payload.calidad).toBe(80);
    expect(dialogRef.close).toHaveBeenCalled();
  });

  it('no guarda con el nombre vacío', async () => {
    await setup();
    setModel({
      nombre: '',
      esDefault: false,
      ladoMayorPreview: 1600,
      ladoMayorThumb: 600,
      calidad: 80,
    });
    submit();

    expect(save).not.toHaveBeenCalled();
    expect(fixture.componentInstance['errors']().length).toBeGreaterThan(0);
  });

  it('no guarda con el lado mayor del preview fuera de rango', async () => {
    await setup();
    setModel({
      nombre: 'Publicación estándar',
      esDefault: false,
      ladoMayorPreview: 5000,
      ladoMayorThumb: 600,
      calidad: 80,
    });
    submit();

    expect(save).not.toHaveBeenCalled();
  });

  it('edición: carga la entidad en el modelo', async () => {
    const opciones: OpcionesPublicacion = {
      id: 5,
      nombre: 'Alta calidad',
      esDefault: false,
      ladoMayorPreview: 2000,
      ladoMayorThumb: 800,
      calidad: 90,
    };
    await setup({ id: 5 }, opciones);

    expect(getById).toHaveBeenCalledWith(5);
    expect(getModel()).toEqual({
      nombre: 'Alta calidad',
      esDefault: false,
      ladoMayorPreview: 2000,
      ladoMayorThumb: 800,
      calidad: 90,
    });
  });
});
