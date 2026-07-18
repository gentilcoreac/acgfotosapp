import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MAT_DIALOG_DATA } from '@angular/material/dialog';
import { of } from 'rxjs';
import { FamiliaGaleriaService, FotoFamilia } from '../../../../core/familia';
import {
  FotoFamiliaPreviewDialogComponent,
  FotoFamiliaPreviewDialogData,
} from './foto-familia-preview-dialog.component';

function foto(id: number): FotoFamilia {
  return { id, grupoId: 1, participanteId: 100, ancho: 800, alto: 600 };
}

describe('FotoFamiliaPreviewDialogComponent', () => {
  let fixture: ComponentFixture<FotoFamiliaPreviewDialogComponent>;

  async function create(data: FotoFamiliaPreviewDialogData): Promise<void> {
    vi.stubGlobal('URL', {
      ...URL,
      createObjectURL: vi.fn().mockReturnValue('blob:mock'),
      revokeObjectURL: vi.fn(),
    });

    await TestBed.configureTestingModule({
      imports: [FotoFamiliaPreviewDialogComponent],
      providers: [
        { provide: MAT_DIALOG_DATA, useValue: data },
        {
          provide: FamiliaGaleriaService,
          useValue: { derivado: vi.fn().mockReturnValue(of(new Blob(['x']))) },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(FotoFamiliaPreviewDialogComponent);
    fixture.detectChanges();
  }

  afterEach(() => vi.unstubAllGlobals());

  it('arranca en la foto del índice recibido', async () => {
    await create({ fotos: [foto(1), foto(2), foto(3)], index: 1 });

    expect(fixture.componentInstance['actual']().id).toBe(2);
  });

  it('siguiente() avanza y da la vuelta al llegar al final', async () => {
    await create({ fotos: [foto(1), foto(2), foto(3)], index: 2 });

    fixture.componentInstance['siguiente']();

    expect(fixture.componentInstance['actual']().id).toBe(1);
  });

  it('anterior() retrocede y da la vuelta al llegar al principio', async () => {
    await create({ fotos: [foto(1), foto(2), foto(3)], index: 0 });

    fixture.componentInstance['anterior']();

    expect(fixture.componentInstance['actual']().id).toBe(3);
  });

  it('con una sola foto no muestra las flechas de navegación', async () => {
    await create({ fotos: [foto(1)], index: 0 });

    expect(fixture.nativeElement.querySelector('.nav')).toBeNull();
  });

  it('las flechas de teclado navegan el carrusel', async () => {
    await create({ fotos: [foto(1), foto(2)], index: 0 });

    fixture.nativeElement.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowRight' }));
    fixture.detectChanges();

    expect(fixture.componentInstance['actual']().id).toBe(2);
  });
});
