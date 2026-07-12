import type { Mock, MockedObject } from 'vitest';
import { WritableSignal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FieldTree } from '@angular/forms/signals';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { of } from 'rxjs';
import { TbiSelectOption } from '../../../shared/ui/tbi-select/tbi-select.component';
import { ParametrosService } from '../data/parametros.service';
import { Parametro } from '../domain/parametro.model';
import { ParametroEditComponent } from './parametro-edit.component';

describe('ParametroEditComponent', () => {
  let fixture: ComponentFixture<ParametroEditComponent>;
  let save: Mock;
  let getById: Mock;
  let getPermisosDeAplicacion: Mock;
  let dialogRef: MockedObject<MatDialogRef<ParametroEditComponent, Parametro>>;

  /** Modelo del form del componente (espeja el `ParametroFormModel` privado). */
  interface FormModel {
    nombre: string;
    valor: string;
    descripcion: string;
    tipoDato: number | null;
    aplicacionId: number | null;
    permisoId: number | null;
  }

  /** Acceso tipado a los miembros protegidos que el test necesita inspeccionar. */
  interface Internals {
    model: WritableSignal<FormModel>;
    form: FieldTree<FormModel>;
    permisoOptions(): TbiSelectOption<number>[];
  }
  const internals = (): Internals => fixture.componentInstance as unknown as Internals;

  /** Setea un campo del modelo y corre los effects (equivale a que el usuario cambie el control). */
  const setField = (patch: Partial<FormModel>): void => {
    internals().model.update((m) => ({ ...m, ...patch }));
    fixture.detectChanges();
  };

  /** Configura el TestBed. `data` controla alta (sin id) vs edición (con id + entidad). */
  async function setup(
    data: {
      id?: number;
    } = {},
    loaded?: Parametro,
  ): Promise<void> {
    save = vi
      .fn()
      .mockName('save')
      .mockImplementation((p: Parametro) => of({ ...p, id: 7 }));
    getById = vi
      .fn()
      .mockName('getById')
      .mockReturnValue(of(loaded ?? null));
    getPermisosDeAplicacion = vi
      .fn()
      .mockName('getPermisosDeAplicacion')
      .mockReturnValue(of([{ id: 10, nombre: 'Permiso 10' }]));
    dialogRef = {
      close: vi.fn().mockName('MatDialogRef.close'),
    } as unknown as MockedObject<MatDialogRef<ParametroEditComponent, Parametro>>;

    await TestBed.configureTestingModule({
      imports: [ParametroEditComponent],
      providers: [
        provideNoopAnimations(),
        { provide: MatDialogRef, useValue: dialogRef },
        { provide: MAT_DIALOG_DATA, useValue: data },
        {
          provide: ParametrosService,
          useValue: {
            crud: { getById, save },
            getAplicaciones: () => of([{ id: 1, nombre: 'App 1' }]),
            getPermisosDeAplicacion,
          },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(ParametroEditComponent);
    fixture.detectChanges();
  }

  const submit = (): void => {
    const form: HTMLFormElement = fixture.nativeElement.querySelector('form');
    form.dispatchEvent(new Event('submit'));
    fixture.detectChanges();
  };

  it('sin aplicación elegida, el permiso arranca deshabilitado y sin opciones', async () => {
    await setup();
    const { form, permisoOptions } = internals();
    expect(form.permisoId().disabled()).toBe(true);
    expect(permisoOptions()).toEqual([]);
  });

  it('al elegir aplicación, habilita y carga los permisos', async () => {
    await setup();
    setField({ aplicacionId: 1 });

    expect(getPermisosDeAplicacion).toHaveBeenCalledWith(1);
    const { form, permisoOptions } = internals();
    expect(form.permisoId().disabled()).toBe(false);
    expect(permisoOptions()).toEqual([{ value: 10, label: 'Permiso 10' }]);
  });

  it('submit inválido (sin campos requeridos) no guarda', async () => {
    await setup();
    submit();

    expect(save).not.toHaveBeenCalled();
  });

  it('alta: guarda el payload completo', async () => {
    await setup();
    setField({
      nombre: 'MaxIntentos',
      valor: '3',
      descripcion: 'Máximo de intentos',
      tipoDato: 1,
      aplicacionId: 1,
    });
    setField({ permisoId: 10 });
    submit();

    expect(save).toHaveBeenCalled();
    const payload = vi.mocked(save).mock.lastCall![0] as Parametro;
    expect(payload.nombre).toBe('MaxIntentos');
    expect(payload.valor).toBe('3');
    expect(payload.descripcion).toBe('Máximo de intentos');
    expect(payload.tipoDato).toBe(1);
    expect(payload.aplicacionId).toBe(1);
    expect(payload.permisoId).toBe(10);
    expect(dialogRef.close).toHaveBeenCalled();
  });

  it('edición: carga la entidad y NO poda el permiso ya cargado (patch inicial)', async () => {
    const loaded: Parametro = {
      id: 7,
      nombre: 'MaxIntentos',
      valor: '3',
      descripcion: 'Máximo de intentos',
      tipoDato: 1,
      aplicacionId: 1,
      permisoId: 10,
    };
    await setup({ id: 7 }, loaded);

    expect(getById).toHaveBeenCalledWith(7);
    const { model, form } = internals();
    expect(model().permisoId).toBe(10);
    expect(form.permisoId().disabled()).toBe(false);
  });

  it('cambiar la aplicación (post-patch) SÍ poda el permiso elegido', async () => {
    const loaded: Parametro = {
      id: 7,
      nombre: 'MaxIntentos',
      valor: '3',
      descripcion: 'Máximo de intentos',
      tipoDato: 1,
      aplicacionId: 1,
      permisoId: 10,
    };
    await setup({ id: 7 }, loaded);
    setField({ aplicacionId: 2 });

    const { model } = internals();
    expect(model().permisoId).toBeNull();
    expect(getPermisosDeAplicacion).toHaveBeenCalledWith(2);
  });

  it('el payload no incluye campos UI-only (mapeo explícito, no spread ciego)', async () => {
    const loaded: Parametro = {
      id: 7,
      nombre: 'MaxIntentos',
      valor: '3',
      descripcion: 'Máximo de intentos',
      tipoDato: 1,
      aplicacionId: 1,
      permisoId: 10,
      aplicacionNombre: 'App 1',
    };
    await setup({ id: 7 }, loaded);
    submit();

    const payload = vi.mocked(save).mock.lastCall![0] as Parametro;
    expect(payload.id).toBe(7);
    expect(payload.nombre).toBe('MaxIntentos');
  });
});
