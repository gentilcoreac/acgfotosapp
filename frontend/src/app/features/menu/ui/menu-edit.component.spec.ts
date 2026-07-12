import type { Mock, MockedObject } from 'vitest';
import { WritableSignal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FieldTree } from '@angular/forms/signals';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { of } from 'rxjs';
import { TbiSelectOption } from '../../../shared/ui/tbi-select/tbi-select.component';
import { MenusService } from '../data/menus.service';
import { Menu } from '../domain/menu.model';
import { MenuEditComponent } from './menu-edit.component';

describe('MenuEditComponent', () => {
  let fixture: ComponentFixture<MenuEditComponent>;
  let save: Mock;
  let getById: Mock;
  let getPermisosDeAplicacion: Mock;
  let getMenusDeAplicacion: Mock;
  let dialogRef: MockedObject<MatDialogRef<MenuEditComponent, Menu>>;

  /** Modelo del form del componente (espeja el `MenuFormModel` privado). */
  interface FormModel {
    aplicacionId: number | null;
    nombre: string;
    codigo: string;
    orden: string;
    menuPadreId: number | null;
    permisoId: number | null;
  }

  /** Acceso tipado a los miembros protegidos que el test necesita inspeccionar. */
  interface Internals {
    model: WritableSignal<FormModel>;
    form: FieldTree<FormModel>;
    menuPadreOptions(): TbiSelectOption<number | null>[];
    permisoOptions(): TbiSelectOption<number | null>[];
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
    loaded?: Menu,
  ): Promise<void> {
    save = vi
      .fn()
      .mockName('save')
      .mockImplementation((m: Menu) => of({ ...m, id: 7 }));
    getById = vi
      .fn()
      .mockName('getById')
      .mockReturnValue(of(loaded ?? null));
    getPermisosDeAplicacion = vi
      .fn()
      .mockName('getPermisosDeAplicacion')
      .mockReturnValue(of([{ id: 10, nombre: 'Permiso 10' }]));
    getMenusDeAplicacion = vi
      .fn()
      .mockName('getMenusDeAplicacion')
      .mockReturnValue(
        of([
          { id: 5, nombre: 'Padre', codigo: 'PAD' },
          { id: 7, nombre: 'Self', codigo: 'SELF' },
        ]),
      );
    dialogRef = {
      close: vi.fn().mockName('MatDialogRef.close'),
    } as unknown as MockedObject<MatDialogRef<MenuEditComponent, Menu>>;

    await TestBed.configureTestingModule({
      imports: [MenuEditComponent],
      providers: [
        provideNoopAnimations(),
        { provide: MatDialogRef, useValue: dialogRef },
        { provide: MAT_DIALOG_DATA, useValue: data },
        {
          provide: MenusService,
          useValue: {
            crud: { getById, save },
            getAplicaciones: () => of([{ id: 1, nombre: 'App 1' }]),
            getPermisosDeAplicacion,
            getMenusDeAplicacion,
          },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(MenuEditComponent);
    fixture.detectChanges();
  }

  const submit = (): void => {
    const form: HTMLFormElement = fixture.nativeElement.querySelector('form');
    form.dispatchEvent(new Event('submit'));
    fixture.detectChanges();
  };

  it('sin aplicación elegida, permiso y menú padre arrancan deshabilitados', async () => {
    await setup();
    const { form } = internals();
    expect(form.permisoId().disabled()).toBe(true);
    expect(form.menuPadreId().disabled()).toBe(true);
  });

  it('al elegir aplicación, habilita y carga permiso y menú padre', async () => {
    await setup();
    setField({ aplicacionId: 1 });

    expect(getPermisosDeAplicacion).toHaveBeenCalledWith(1);
    expect(getMenusDeAplicacion).toHaveBeenCalledWith(1);
    const { form } = internals();
    expect(form.permisoId().disabled()).toBe(false);
    expect(form.menuPadreId().disabled()).toBe(false);
  });

  it('alta: guarda con orden numérico y opcionales (ruta/ícono) en null', async () => {
    await setup();
    setField({ aplicacionId: 1, nombre: 'Reportes', codigo: 'REP', orden: '3' });
    submit();

    expect(save).toHaveBeenCalled();
    const payload = vi.mocked(save).mock.lastCall![0] as Menu;
    expect(payload.aplicacionId).toBe(1);
    expect(payload.nombre).toBe('Reportes');
    expect(payload.codigo).toBe('REP');
    expect(payload.orden).toBe(3);
    expect(payload.routePath).toBeNull();
    expect(payload.imagenWeb).toBeNull();
    expect(dialogRef.close).toHaveBeenCalled();
  });

  it('alta: orden = 0 es inválido y no guarda (la API exige NotEmpty)', async () => {
    await setup();
    setField({ aplicacionId: 1, nombre: 'X', codigo: 'X', orden: '0' });
    submit();

    expect(save).not.toHaveBeenCalled();
  });

  it('edición: carga la entidad y excluye al propio menú del select de padre', async () => {
    const loaded: Menu = {
      id: 7,
      nombre: 'Self',
      codigo: 'SELF',
      estado: true,
      imagenWeb: null,
      orden: 2,
      aplicacionId: 1,
      menuPadreId: null,
      permisoId: null,
      visibleSideMenu: true,
      visibleDash: false,
      routePath: null,
    };
    await setup({ id: 7 }, loaded);

    expect(getById).toHaveBeenCalledWith(7);
    const { form, menuPadreOptions } = internals();
    expect(form.permisoId().disabled()).toBe(false);
    // El menú 7 (el editado) no debe ofrecerse como padre de sí mismo; el 5 sí.
    expect(menuPadreOptions().some((o) => o.value === 7)).toBe(false);
    expect(menuPadreOptions().some((o) => o.value === 5)).toBe(true);
  });

  it('edición: el patch inicial NO poda el permiso/menú padre cargados', async () => {
    const loaded: Menu = {
      id: 7,
      nombre: 'Self',
      codigo: 'SELF',
      estado: true,
      imagenWeb: null,
      orden: 2,
      aplicacionId: 1,
      menuPadreId: 5,
      permisoId: 10,
      visibleSideMenu: true,
      visibleDash: false,
      routePath: null,
    };
    await setup({ id: 7 }, loaded);

    const { model } = internals();
    expect(model().menuPadreId).toBe(5);
    expect(model().permisoId).toBe(10);
  });

  it('cambiar la aplicación (post-patch) SÍ poda el permiso/menú padre elegidos', async () => {
    const loaded: Menu = {
      id: 7,
      nombre: 'Self',
      codigo: 'SELF',
      estado: true,
      imagenWeb: null,
      orden: 2,
      aplicacionId: 1,
      menuPadreId: 5,
      permisoId: 10,
      visibleSideMenu: true,
      visibleDash: false,
      routePath: null,
    };
    await setup({ id: 7 }, loaded);
    setField({ aplicacionId: 2 });

    const { model } = internals();
    expect(model().menuPadreId).toBeNull();
    expect(model().permisoId).toBeNull();
    expect(getPermisosDeAplicacion).toHaveBeenCalledWith(2);
  });
});
