import type { Mock, MockedObject } from 'vitest';
import { WritableSignal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { of } from 'rxjs';
import { AuthStore } from '../../../core/auth';
import { PermisosService } from '../data/permisos.service';
import { Permiso } from '../domain/permiso.model';
import { PermisoEditComponent } from './permiso-edit.component';

describe('PermisoEditComponent', () => {
  let fixture: ComponentFixture<PermisoEditComponent>;
  let save: Mock;
  let setEsRestringido: Mock;
  let dialogRef: MockedObject<MatDialogRef<PermisoEditComponent, Permiso>>;

  const aplicaciones = [{ id: 1, nombre: 'General' }];
  // Árbol Módulo→Controller→Endpoint (como lo devuelve endpoints/hierarchical-items): los
  // agrupadores con id 0, las hojas con el endpointId real.
  const endpointsTree = [
    {
      id: 0,
      name: 'AcgFotos.Base',
      children: [
        {
          id: 0,
          name: 'Rol',
          children: [
            { id: 5, name: 'GET - api/roles' },
            { id: 7, name: 'POST - api/roles/update' },
          ],
        },
      ],
    },
  ];

  /** Configura el TestBed; `isRoot` controla si el usuario es root. */
  async function setup(isRoot: boolean): Promise<void> {
    save = vi
      .fn()
      .mockName('save')
      .mockImplementation((p: Permiso) => of({ ...p, id: 10 }));
    setEsRestringido = vi.fn().mockName('setEsRestringido').mockReturnValue(of(undefined));
    dialogRef = {
      close: vi.fn().mockName('MatDialogRef.close'),
    } as unknown as MockedObject<MatDialogRef<PermisoEditComponent, Permiso>>;

    await TestBed.configureTestingModule({
      imports: [PermisoEditComponent],
      providers: [
        provideNoopAnimations(),
        { provide: MatDialogRef, useValue: dialogRef },
        { provide: MAT_DIALOG_DATA, useValue: {} }, // sin id → alta
        { provide: AuthStore, useValue: { isRoot: () => isRoot } },
        {
          provide: PermisosService,
          useValue: {
            crud: { getById: () => of(null), save },
            getAplicaciones: () => of(aplicaciones),
            getPermisosDeAplicacion: () => of([]),
            getEndpointsTree: () => of(endpointsTree),
            setEsRestringido,
          },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(PermisoEditComponent);
    fixture.detectChanges();
  }

  /** Modelo del form del componente (espeja el `PermisoFormModel` privado). */
  interface FormModel {
    nombre: string;
    codigoPermiso: string;
    descripcion: string;
    activo: boolean;
    aplicacionId: number | null;
    permisoPadreId: number | null;
    endpoints: number[];
    esRestringido: boolean;
  }

  // El modelo es protected; en el spec lo seteamos directo para no manejar el overlay del
  // mat-select en headless.
  const setModel = (value: FormModel): void => {
    (fixture.componentInstance as unknown as { model: WritableSignal<FormModel> }).model.set(value);
    fixture.detectChanges();
  };

  const submit = (): void => {
    (fixture.nativeElement.querySelector('form') as HTMLFormElement).dispatchEvent(
      new Event('submit'),
    );
    fixture.detectChanges();
  };

  const toggleByText = (text: string): HTMLElement | null => {
    const toggles = Array.from(
      fixture.nativeElement.querySelectorAll('mat-slide-toggle'),
    ) as HTMLElement[];
    return toggles.find((t) => t.textContent?.includes(text)) ?? null;
  };

  it('no muestra el toggle Restringido si no es root', async () => {
    await setup(false);
    expect(toggleByText('Restringido')).toBeNull();
  });

  it('muestra el toggle Restringido si es root', async () => {
    await setup(true);
    expect(toggleByText('Restringido')).not.toBeNull();
  });

  it('guarda enviando endpoints como ids y omitiendo campos sensibles (alta)', async () => {
    await setup(false);
    setModel({
      nombre: 'P1',
      codigoPermiso: 'COD_P1',
      descripcion: 'Permiso 1',
      activo: true,
      aplicacionId: 1,
      permisoPadreId: null,
      endpoints: [5, 7],
      esRestringido: false,
    });
    submit();
    expect(save).toHaveBeenCalled();
    const payload = vi.mocked(save).mock.lastCall![0] as Permiso;
    expect(payload.nombre).toBe('P1');
    expect(payload.codigoPermiso).toBe('COD_P1');
    expect(payload.endpoints).toEqual([{ endpointId: 5 }, { endpointId: 7 }]);
    expect(payload.esRestringido).toBeUndefined();
    expect(setEsRestringido).not.toHaveBeenCalled();
  });

  it('root: si cambia esRestringido, persiste por el endpoint dedicado tras guardar', async () => {
    await setup(true);
    setModel({
      nombre: 'P',
      codigoPermiso: 'COD_P',
      descripcion: 'D',
      activo: true,
      aplicacionId: 1,
      permisoPadreId: null,
      endpoints: [],
      esRestringido: true,
    });
    submit();
    expect(save).toHaveBeenCalled();
    expect(setEsRestringido).toHaveBeenCalledWith(10, true);
    expect(dialogRef.close).toHaveBeenCalled();
  });
});
