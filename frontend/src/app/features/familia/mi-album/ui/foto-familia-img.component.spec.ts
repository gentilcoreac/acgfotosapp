import type { Mock } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { FamiliaGaleriaService } from '../../../../core/familia';
import { FotoFamiliaImgComponent } from './foto-familia-img.component';

describe('FotoFamiliaImgComponent', () => {
  let fixture: ComponentFixture<FotoFamiliaImgComponent>;
  let derivado: Mock;

  async function setup(fotoId = 5): Promise<void> {
    vi.stubGlobal('URL', {
      ...URL,
      createObjectURL: vi.fn().mockReturnValue('blob:mock'),
      revokeObjectURL: vi.fn(),
    });

    await TestBed.configureTestingModule({
      imports: [FotoFamiliaImgComponent],
      providers: [{ provide: FamiliaGaleriaService, useValue: { derivado } }],
    }).compileComponents();

    fixture = TestBed.createComponent(FotoFamiliaImgComponent);
    fixture.componentRef.setInput('fotoId', fotoId);
    fixture.detectChanges();
    fixture.detectChanges();
  }

  afterEach(() => vi.unstubAllGlobals());

  it('baja el blob autenticado (con el token de familia) y renderiza la imagen vía object URL', async () => {
    derivado = vi.fn().mockName('derivado').mockReturnValue(of(new Blob(['jpg'])));
    await setup();

    expect(derivado).toHaveBeenCalledWith(5, 'thumb');
    const img = fixture.nativeElement.querySelector('img') as HTMLImageElement;
    expect(img).toBeTruthy();
    expect(img.src).toContain('blob:mock');
  });

  it('si el derivado no está disponible (404, foto ajena o vencida) muestra el placeholder', async () => {
    derivado = vi
      .fn()
      .mockName('derivado')
      .mockReturnValue(throwError(() => new Error('404')));
    await setup();

    expect(fixture.nativeElement.querySelector('img')).toBeNull();
    expect(fixture.nativeElement.querySelector('.placeholder')).toBeTruthy();
  });
});
