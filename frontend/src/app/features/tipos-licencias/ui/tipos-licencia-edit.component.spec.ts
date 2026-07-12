import type { Mock, MockedObject } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { of } from 'rxjs';
import { AuthStore } from '../../../core/auth';
import { TiposLicenciaService } from '../data/tipos-licencia.service';
import { TipoLicencia } from '../domain/tipo-licencia.model';
import { TiposLicenciaEditComponent } from './tipos-licencia-edit.component';

describe('TiposLicenciaEditComponent', () => {
  let fixture: ComponentFixture<TiposLicenciaEditComponent>;
  let save: Mock;
  let setDefaultTenant: Mock;
  let dialogRef: MockedObject<MatDialogRef<TiposLicenciaEditComponent, TipoLicencia>>;

  const roles = [
    { id: 1, descripcion: 'Administrador' },
    { id: 2, descripcion: 'Lector' },
  ];

  /** Configura el TestBed; `isRoot` controla si el usuario es root. */
  async function setup(isRoot: boolean): Promise<void> {
    save = vi
      .fn()
      .mockName('save')
      .mockImplementation((tl: TipoLicencia) => of({ ...tl, id: 10 }));
    setDefaultTenant = vi.fn().mockName('setDefaultTenant').mockReturnValue(of(undefined));
    dialogRef = {
      close: vi.fn().mockName('MatDialogRef.close'),
    } as unknown as MockedObject<MatDialogRef<TiposLicenciaEditComponent, TipoLicencia>>;

    await TestBed.configureTestingModule({
      imports: [TiposLicenciaEditComponent],
      providers: [
        provideNoopAnimations(),
        { provide: MatDialogRef, useValue: dialogRef },
        { provide: MAT_DIALOG_DATA, useValue: {} }, // sin id → alta
        { provide: AuthStore, useValue: { isRoot: () => isRoot } },
        {
          provide: TiposLicenciaService,
          useValue: {
            crud: { getById: () => of(null), save },
            getRoles: () => of(roles),
            setDefaultTenant,
          },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(TiposLicenciaEditComponent);
    fixture.detectChanges();
  }

  // Setea un tbi-text-field por índice (0 = código, 1 = descripción).
  const setTextField = (index: number, text: string): void => {
    const inputs = fixture.nativeElement.querySelectorAll(
      'tbi-text-field input',
    ) as NodeListOf<HTMLInputElement>;
    inputs[index].value = text;
    inputs[index].dispatchEvent(new Event('input'));
    fixture.detectChanges();
  };

  const submit = (): void => {
    const form: HTMLFormElement = fixture.nativeElement.querySelector('form');
    form.dispatchEvent(new Event('submit'));
    fixture.detectChanges();
  };

  // El checkbox del flag se distingue por su texto ("defecto"); los de roles usan el nombre del rol.
  const checkboxByText = (text: string): HTMLElement | null => {
    const boxes = Array.from(
      fixture.nativeElement.querySelectorAll('mat-checkbox'),
    ) as HTMLElement[];
    return boxes.find((box) => box.textContent?.includes(text)) ?? null;
  };

  it('no muestra el toggle de tenant por defecto si no es root', async () => {
    await setup(false);
    expect(checkboxByText('defecto')).toBeNull();
  });

  it('muestra el toggle de tenant por defecto si es root', async () => {
    await setup(true);
    expect(checkboxByText('defecto')).not.toBeNull();
  });

  it('guarda enviando los roles como ids (alta)', async () => {
    await setup(false);
    setTextField(0, 'ofX');
    setTextField(1, 'Licencia X');
    const rol = checkboxByText('Administrador')?.querySelector(
      'input[type="checkbox"]',
    ) as HTMLInputElement;
    rol.click();
    fixture.detectChanges();
    submit();
    expect(save).toHaveBeenCalled();
    const payload = vi.mocked(save).mock.lastCall![0] as TipoLicencia;
    expect(payload.codigoTipoLicencia).toBe('ofX');
    expect(payload.descripcion).toBe('Licencia X');
    expect(payload.rolIds).toEqual([1]);
    expect(payload.tipoLicenciaRoles).toBeUndefined();
    expect(setDefaultTenant).not.toHaveBeenCalled();
  });

  it('root: si cambia el flag, persiste por el endpoint dedicado tras guardar', async () => {
    await setup(true);
    setTextField(0, 'ofX');
    setTextField(1, 'Licencia X');
    const flag = checkboxByText('defecto')?.querySelector(
      'input[type="checkbox"]',
    ) as HTMLInputElement;
    flag.click();
    fixture.detectChanges();
    submit();
    expect(save).toHaveBeenCalled();
    expect(setDefaultTenant).toHaveBeenCalledWith(10, true);
    expect(dialogRef.close).toHaveBeenCalled();
  });
});
