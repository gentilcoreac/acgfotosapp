import type { Mock } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { FotosService } from '../data/fotos.service';
import { FotoImgComponent } from './foto-img.component';

describe('FotoImgComponent', () => {
  let fixture: ComponentFixture<FotoImgComponent>;
  let derivado: Mock;
  let createObjectURL: Mock;
  let revokeObjectURL: Mock;

  async function setup(fotoId = 5): Promise<void> {
    createObjectURL = vi.fn().mockReturnValue('blob:mock');
    revokeObjectURL = vi.fn();
    // jsdom no implementa object URLs.
    vi.stubGlobal('URL', { ...URL, createObjectURL, revokeObjectURL });

    await TestBed.configureTestingModule({
      imports: [FotoImgComponent],
      providers: [{ provide: FotosService, useValue: { derivado } }],
    }).compileComponents();

    fixture = TestBed.createComponent(FotoImgComponent);
    fixture.componentRef.setInput('fotoId', fotoId);
    fixture.detectChanges();
    fixture.detectChanges(); // ciclo extra: el effect del object URL corre tras resolver el resource
  }

  afterEach(() => vi.unstubAllGlobals());

  it('baja el blob autenticado y renderiza la imagen vía object URL', async () => {
    derivado = vi.fn().mockName('derivado').mockReturnValue(of(new Blob(['jpg'])));
    await setup();

    expect(derivado).toHaveBeenCalledWith(5, 'thumb');
    const img = fixture.nativeElement.querySelector('img') as HTMLImageElement;
    expect(img).toBeTruthy();
    expect(img.src).toContain('blob:mock');
  });

  it('si el derivado no está disponible (404) muestra el placeholder', async () => {
    derivado = vi
      .fn()
      .mockName('derivado')
      .mockReturnValue(throwError(() => new Error('404')));
    await setup();

    expect(fixture.nativeElement.querySelector('img')).toBeNull();
    expect(fixture.nativeElement.querySelector('.placeholder')).toBeTruthy();
  });

  it('al destruirse revoca el object URL', async () => {
    derivado = vi.fn().mockReturnValue(of(new Blob(['jpg'])));
    await setup();

    fixture.destroy();

    expect(revokeObjectURL).toHaveBeenCalledWith('blob:mock');
  });
});
