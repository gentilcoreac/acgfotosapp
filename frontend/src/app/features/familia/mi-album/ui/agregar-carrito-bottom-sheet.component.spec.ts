import type { Mock } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MAT_BOTTOM_SHEET_DATA, MatBottomSheetRef } from '@angular/material/bottom-sheet';
import { of } from 'rxjs';
import { FamiliaGaleriaService } from '../../../../core/familia';
import { AgregarCarritoBottomSheetComponent, AgregarCarritoBottomSheetData } from './agregar-carrito-bottom-sheet.component';

describe('AgregarCarritoBottomSheetComponent', () => {
  let fixture: ComponentFixture<AgregarCarritoBottomSheetComponent>;
  let dismiss: Mock;
  let onAmpliar: Mock;

  async function setup(): Promise<void> {
    vi.stubGlobal('URL', {
      ...URL,
      createObjectURL: vi.fn().mockReturnValue('blob:mock'),
      revokeObjectURL: vi.fn(),
    });

    dismiss = vi.fn();
    onAmpliar = vi.fn();
    const data: AgregarCarritoBottomSheetData = {
      fotoId: 5,
      tamanosPrecios: [],
      onAmpliar,
    };

    await TestBed.configureTestingModule({
      imports: [AgregarCarritoBottomSheetComponent],
      providers: [
        { provide: MAT_BOTTOM_SHEET_DATA, useValue: data },
        { provide: MatBottomSheetRef, useValue: { dismiss } },
        {
          provide: FamiliaGaleriaService,
          useValue: { derivado: vi.fn().mockReturnValue(of(new Blob(['jpg']))) },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(AgregarCarritoBottomSheetComponent);
    fixture.detectChanges();
  }

  afterEach(() => vi.unstubAllGlobals());

  it('muestra la miniatura de la foto que se está agregando', async () => {
    await setup();
    expect(fixture.nativeElement.querySelector('.agregar-carrito-sheet__thumb')).toBeTruthy();
  });

  it('al ampliar la miniatura cierra el sheet y delega en el visor del contexto padre', async () => {
    await setup();

    (fixture.nativeElement.querySelector('.agregar-carrito-sheet__thumb-btn') as HTMLButtonElement).click();

    expect(dismiss).toHaveBeenCalled();
    expect(onAmpliar).toHaveBeenCalled();
  });
});
