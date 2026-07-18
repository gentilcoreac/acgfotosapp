import type { Mock } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { FamiliaGaleriaService, FamiliaSessionStore, ParticipanteSesion } from '../../../../core/familia';
import { FotoFamiliaImgComponent } from './foto-familia-img.component';

describe('FotoFamiliaImgComponent', () => {
  let fixture: ComponentFixture<FotoFamiliaImgComponent>;
  let derivado: Mock;

  async function setup(fotoId = 5, participantes: ParticipanteSesion[] = []): Promise<void> {
    vi.stubGlobal('URL', {
      ...URL,
      createObjectURL: vi.fn().mockReturnValue('blob:mock'),
      revokeObjectURL: vi.fn(),
    });

    await TestBed.configureTestingModule({
      imports: [FotoFamiliaImgComponent],
      providers: [{ provide: FamiliaGaleriaService, useValue: { derivado } }],
    }).compileComponents();

    if (participantes.length > 0) {
      TestBed.inject(FamiliaSessionStore).setSession({
        token: 't',
        validTo: new Date(Date.now() + 60000).toISOString(),
        eventoId: 1,
        nombreEvento: 'Egresados 2026',
        participantes,
      });
    }

    fixture = TestBed.createComponent(FotoFamiliaImgComponent);
    fixture.componentRef.setInput('fotoId', fotoId);
    fixture.detectChanges();
    fixture.detectChanges();
  }

  beforeEach(() => sessionStorage.clear());
  afterEach(() => {
    sessionStorage.clear();
    vi.unstubAllGlobals();
  });

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

  it('con sesión de familia activa, tilea "Familia de {nombre}" como marca de agua sobre la imagen', async () => {
    derivado = vi.fn().mockName('derivado').mockReturnValue(of(new Blob(['jpg'])));
    await setup(5, [{ id: 100, nombre: 'Ana Pérez' }]);

    const marca = fixture.nativeElement.querySelector('.marca-familia') as HTMLElement;
    expect(marca).toBeTruthy();
    expect(marca.style.backgroundImage).toContain(encodeURIComponent('Familia de Ana Pérez'));
  });

  it('sin sesión activa no agrega la marca de agua (nada que mostrar todavía)', async () => {
    derivado = vi.fn().mockName('derivado').mockReturnValue(of(new Blob(['jpg'])));
    await setup(5, []);

    expect(fixture.nativeElement.querySelector('.marca-familia')).toBeNull();
  });
});
