import type { Mock, MockedObject } from 'vitest';
import { WritableSignal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { of } from 'rxjs';
import { TbiSearchSelectItem } from '../../../shared/ui/tbi-search-select/tbi-search-select.component';
import { GruposService } from '../data/grupos.service';
import { Grupo } from '../domain/grupo.model';
import { GrupoEditComponent } from './grupo-edit.component';

/** Acceso tipado a miembros protected para los asserts. */
interface GrupoEditInternals {
  model: WritableSignal<{ nombre: string; usuarios: TbiSearchSelectItem[] }>;
  toggleRol(rolId: number, checked: boolean): void;
  selectedRolIds: () => ReadonlySet<number>;
}

describe('GrupoEditComponent', () => {
  let fixture: ComponentFixture<GrupoEditComponent>;
  let save: Mock;
  let getById: Mock;
  let dialogRef: MockedObject<MatDialogRef<GrupoEditComponent, Grupo>>;

  /** Configura el TestBed. `data` simula alta (sin id) o edición (con id); `entity` es lo que devuelve getById. */
  async function setup(
    data: {
      id?: number;
    } = {},
    entity?: Grupo,
  ): Promise<void> {
    save = vi
      .fn()
      .mockName('save')
      .mockImplementation((g: Grupo) => of({ ...g, id: g.id ?? 10 }));
    getById = vi
      .fn()
      .mockName('getById')
      .mockReturnValue(of(entity ?? null));
    dialogRef = {
      close: vi.fn().mockName('MatDialogRef.close'),
    } as unknown as MockedObject<MatDialogRef<GrupoEditComponent, Grupo>>;

    await TestBed.configureTestingModule({
      imports: [GrupoEditComponent],
      providers: [
        provideNoopAnimations(),
        { provide: MatDialogRef, useValue: dialogRef },
        { provide: MAT_DIALOG_DATA, useValue: data },
        {
          provide: GruposService,
          useValue: {
            crud: { getById, save },
            searchUsuarios: () => of([]),
            getRoles: () =>
              of([
                {
                  id: 7,
                  descripcion: 'Lector',
                  licencias: [{ id: 3, descripcion: 'Planificador' }],
                },
              ]),
          },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(GrupoEditComponent);
    fixture.detectChanges();
  }

  const internals = (): GrupoEditInternals =>
    fixture.componentInstance as unknown as GrupoEditInternals;

  const setNombre = (text: string): void => {
    const input: HTMLInputElement = fixture.nativeElement.querySelector('tbi-text-field input');
    input.value = text;
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();
  };

  const submit = (): void => {
    const form: HTMLFormElement = fixture.nativeElement.querySelector('form');
    form.dispatchEvent(new Event('submit'));
    fixture.detectChanges();
  };

  it('no guarda si el form es inválido (falta nombre o miembros)', async () => {
    await setup();
    submit();
    expect(save).not.toHaveBeenCalled();
  });

  it('guarda enviando miembros y roles como ids, omitiendo las colecciones de respuesta (alta)', async () => {
    await setup();
    setNombre('Administradores');
    internals().model.update((m) => ({
      ...m,
      usuarios: [
        { id: 1, label: 'Ana' },
        { id: 2, label: 'Bruno' },
      ],
    }));
    internals().toggleRol(7, true);
    fixture.detectChanges();
    submit();

    expect(save).toHaveBeenCalled();
    const payload = vi.mocked(save).mock.lastCall![0] as Grupo;
    expect(payload.nombre).toBe('Administradores');
    expect(payload.usuarioIds).toEqual([1, 2]);
    expect(payload.rolIds).toEqual([7]);
    expect(payload.usuarioGrupos).toBeUndefined();
    expect(payload.grupoRoles).toBeUndefined();
    expect(dialogRef.close).toHaveBeenCalled();
  });

  it('muestra un chip con la licencia de cada rol (§11.1)', async () => {
    await setup();
    const chips = fixture.nativeElement.querySelectorAll('.edit__rol-licencias mat-chip');
    expect(chips.length).toBe(1);
    expect((chips[0].textContent as string).trim()).toBe('Planificador');
  });

  it('avisa cuando el grupo mezcla usuarios de distinta licencia (§11.2)', async () => {
    await setup(
      { id: 5 },
      {
        id: 5,
        nombre: 'Mixto',
        usuarioGrupos: [
          { usuarioId: 1, usuarioTipoLicenciaActivaId: 3 },
          { usuarioId: 2, usuarioTipoLicenciaActivaId: 5 },
        ],
        grupoRoles: [],
      },
    );
    expect(fixture.nativeElement.querySelector('.edit__alerta')).toBeTruthy();
  });

  it('no avisa cuando todos los miembros tienen la misma licencia (§11.2)', async () => {
    await setup(
      { id: 6 },
      {
        id: 6,
        nombre: 'Homogéneo',
        usuarioGrupos: [
          { usuarioId: 1, usuarioTipoLicenciaActivaId: 3 },
          { usuarioId: 2, usuarioTipoLicenciaActivaId: 3 },
        ],
        grupoRoles: [],
      },
    );
    expect(fixture.nativeElement.querySelector('.edit__alerta')).toBeNull();
  });

  it('patchForm carga nombre, chips de miembros y marca los roles del grupo (edición)', async () => {
    const entity: Grupo = {
      id: 5,
      nombre: 'Ventas',
      usuarioGrupos: [
        { usuarioId: 2, usuarioNombre: 'Ana', usuarioApellido: 'Smith', usuarioUserName: 'asmith' },
      ],
      grupoRoles: [{ rolId: 7 }],
    };
    await setup({ id: 5 }, entity);

    expect(internals().model().nombre).toBe('Ventas');
    // toMatchObject: Signal Forms taggea los items de arrays del modelo con un Symbol interno de
    // tracking (identidad de items en el FieldTree) y toEqual compara symbols en Vitest.
    expect(internals().model().usuarios).toMatchObject([{ id: 2, label: 'Ana Smith (asmith)' }]);
    expect(internals().selectedRolIds().has(7)).toBe(true);
  });
});
