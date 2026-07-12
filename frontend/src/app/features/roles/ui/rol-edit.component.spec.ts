import type { Mock, MockedObject } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { of } from 'rxjs';
import { AuthStore } from '../../../core/auth';
import { TbiTreeNode } from '../../../shared/ui/tbi-tree-select/tbi-tree-select.component';
import { RolesService } from '../data/roles.service';
import { Rol } from '../domain/rol.model';
import { RolEditComponent } from './rol-edit.component';

describe('RolEditComponent', () => {
  let fixture: ComponentFixture<RolEditComponent>;
  let save: Mock;
  let setDefaultTenant: Mock;
  let dialogRef: MockedObject<MatDialogRef<RolEditComponent, Rol>>;

  const tree: TbiTreeNode[] = [{ id: 1, name: 'Padre', children: [{ id: 2, name: 'Hijo' }] }];

  /** Configura el TestBed; `isRoot` controla si el usuario es root. */
  async function setup(isRoot: boolean): Promise<void> {
    save = vi
      .fn()
      .mockName('save')
      .mockImplementation((rol: Rol) => of({ ...rol, id: 10 }));
    setDefaultTenant = vi.fn().mockName('setDefaultTenant').mockReturnValue(of(undefined));
    dialogRef = {
      close: vi.fn().mockName('MatDialogRef.close'),
    } as unknown as MockedObject<MatDialogRef<RolEditComponent, Rol>>;

    await TestBed.configureTestingModule({
      imports: [RolEditComponent],
      providers: [
        provideNoopAnimations(),
        { provide: MatDialogRef, useValue: dialogRef },
        { provide: MAT_DIALOG_DATA, useValue: {} }, // sin id → alta
        { provide: AuthStore, useValue: { isRoot: () => isRoot } },
        {
          provide: RolesService,
          useValue: {
            crud: { getById: () => of(null), save },
            getPermisosTree: () => of(tree),
            setDefaultTenant,
          },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(RolEditComponent);
    fixture.detectChanges();
  }

  const setDescripcion = (text: string): void => {
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

  // El checkbox del flag se distingue por su texto: el tbi-tree-select también usa mat-checkbox.
  const flagCheckbox = (): HTMLElement | null => {
    const boxes = Array.from(
      fixture.nativeElement.querySelectorAll('mat-checkbox'),
    ) as HTMLElement[];
    return boxes.find((box) => box.textContent?.includes('defecto')) ?? null;
  };

  it('no muestra el toggle de tenant por defecto si no es root', async () => {
    await setup(false);
    expect(flagCheckbox()).toBeNull();
  });

  it('muestra el toggle de tenant por defecto si es root', async () => {
    await setup(true);
    expect(flagCheckbox()).not.toBeNull();
  });

  it('guarda el rol enviando los permisos como ids (alta)', async () => {
    await setup(false);
    setDescripcion('Lectura');
    submit();
    expect(save).toHaveBeenCalled();
    const payload = vi.mocked(save).mock.lastCall![0] as Rol;
    expect(payload.descripcion).toBe('Lectura');
    expect(payload.permisoIds).toEqual([]);
    expect(payload.rolPermisos).toBeUndefined();
    expect(setDefaultTenant).not.toHaveBeenCalled();
  });

  it('root: si cambia el flag, persiste por el endpoint dedicado tras guardar', async () => {
    await setup(true);
    setDescripcion('Admin');
    const flag = flagCheckbox()?.querySelector('input[type="checkbox"]') as HTMLInputElement;
    flag.click();
    fixture.detectChanges();
    submit();
    expect(save).toHaveBeenCalled();
    expect(setDefaultTenant).toHaveBeenCalledWith(10, true);
    expect(dialogRef.close).toHaveBeenCalled();
  });
});
