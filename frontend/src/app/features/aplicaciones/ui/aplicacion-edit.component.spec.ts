import type { Mock, MockedObject } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { of } from 'rxjs';
import { AplicacionesService } from '../data/aplicaciones.service';
import { Aplicacion } from '../domain/aplicacion.model';
import { AplicacionEditComponent } from './aplicacion-edit.component';

describe('AplicacionEditComponent', () => {
  let fixture: ComponentFixture<AplicacionEditComponent>;
  let save: Mock;
  let getById: Mock;
  let dialogRef: MockedObject<MatDialogRef<AplicacionEditComponent, Aplicacion>>;

  /** Configura el TestBed. `data` controla alta (sin id) vs edición (con id + entidad). */
  async function setup(
    data: {
      id?: number;
    } = {},
    loaded?: Aplicacion,
  ): Promise<void> {
    save = vi
      .fn()
      .mockName('save')
      .mockImplementation((a: Aplicacion) => of({ ...a, id: 7 }));
    getById = vi
      .fn()
      .mockName('getById')
      .mockReturnValue(of(loaded ?? null));
    dialogRef = {
      close: vi.fn().mockName('MatDialogRef.close'),
    } as unknown as MockedObject<MatDialogRef<AplicacionEditComponent, Aplicacion>>;

    await TestBed.configureTestingModule({
      imports: [AplicacionEditComponent],
      providers: [
        provideNoopAnimations(),
        { provide: MatDialogRef, useValue: dialogRef },
        { provide: MAT_DIALOG_DATA, useValue: data },
        {
          provide: AplicacionesService,
          useValue: { crud: { getById, save } },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(AplicacionEditComponent);
    fixture.detectChanges();
  }

  // Setea un tbi-text-field por índice (0 = nombre, 1 = código, 2 = icono/iconoUrl).
  const setTextField = (index: number, text: string): void => {
    const inputs = fixture.nativeElement.querySelectorAll(
      'tbi-text-field input',
    ) as NodeListOf<HTMLInputElement>;
    inputs[index].value = text;
    inputs[index].dispatchEvent(new Event('input'));
    fixture.detectChanges();
  };

  // mat-slide-toggle por su texto (Activa / Definir nombre|URL de ícono).
  const toggleByText = (text: string): HTMLInputElement | null => {
    const toggles = Array.from(
      fixture.nativeElement.querySelectorAll('mat-slide-toggle'),
    ) as HTMLElement[];
    const found = toggles.find((t) => t.textContent?.includes(text));
    return (found?.querySelector('button[role="switch"]') as HTMLInputElement) ?? null;
  };

  const submit = (): void => {
    const form: HTMLFormElement = fixture.nativeElement.querySelector('form');
    form.dispatchEvent(new Event('submit'));
    fixture.detectChanges();
  };

  it('alta: arranca en modo nombre de ícono y guarda con iconoUrl en null', async () => {
    await setup();
    setTextField(0, 'Mi App'); // nombre
    setTextField(1, 'MIAPP'); // código
    setTextField(2, 'dashboard'); // icono (modo nombre por defecto)
    submit();

    expect(save).toHaveBeenCalled();
    const payload = vi.mocked(save).mock.lastCall![0] as Aplicacion;
    expect(payload.nombre).toBe('Mi App');
    expect(payload.codigo).toBe('MIAPP');
    expect(payload.icono).toBe('dashboard');
    expect(payload.iconoUrl).toBeNull();
    expect(dialogRef.close).toHaveBeenCalled();
  });

  it('alta: guarda aunque no se complete el ícono (icono/iconoUrl son opcionales)', async () => {
    await setup();
    setTextField(0, 'Sin Icono');
    setTextField(1, 'NOICON');
    // No se completa el campo de ícono.
    submit();

    expect(save).toHaveBeenCalled();
    const payload = vi.mocked(save).mock.lastCall![0] as Aplicacion;
    expect(payload.nombre).toBe('Sin Icono');
    expect(payload.icono).toBeNull();
    expect(payload.iconoUrl).toBeNull();
    expect(dialogRef.close).toHaveBeenCalled();
  });

  it('alta: al cambiar a modo URL guarda con icono en null', async () => {
    await setup();
    setTextField(0, 'Mi App');
    setTextField(1, 'MIAPP');
    // Cambia al modo URL.
    toggleByText('ícono')?.click();
    fixture.detectChanges();
    setTextField(2, 'https://cdn/app.png'); // ahora el 3er field es iconoUrl
    submit();

    expect(save).toHaveBeenCalled();
    const payload = vi.mocked(save).mock.lastCall![0] as Aplicacion;
    expect(payload.iconoUrl).toBe('https://cdn/app.png');
    expect(payload.icono).toBeNull();
  });

  it('edición: una app con iconoUrl arranca en modo URL', async () => {
    const loaded: Aplicacion = {
      id: 7,
      nombre: 'App',
      codigo: 'APP',
      activo: true,
      icono: null,
      iconoUrl: 'https://cdn/app.png',
    };
    await setup({ id: 7 }, loaded);
    expect(getById).toHaveBeenCalledWith(7);
    // En modo URL el toggle de selección de ícono está apagado.
    expect(
      (
        fixture.componentInstance as unknown as {
          model: () => Aplicacion & { iconoSelected: boolean };
        }
      ).model().iconoSelected,
    ).toBe(false);
  });
});
