import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MAT_DIALOG_DATA } from '@angular/material/dialog';
import { of } from 'rxjs';
import { CarritoStore, FamiliaGaleriaService, FamiliaSessionStore, FotoFamilia } from '../../../../core/familia';
import {
  FotoFamiliaPreviewDialogComponent,
  FotoFamiliaPreviewDialogData,
} from './foto-familia-preview-dialog.component';

function foto(id: number): FotoFamilia {
  return { id, grupoId: 1, participanteId: 100, nombreArchivoOriginal: `IMG_${id}.jpg`, ancho: 800, alto: 600 };
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

  afterEach(() => {
    vi.unstubAllGlobals();
    sessionStorage.clear();
  });

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

  it('muestra el nombre de archivo de la foto actual', async () => {
    await create({ fotos: [foto(1), foto(2)], index: 0 });

    expect(fixture.nativeElement.querySelector('.archivo')?.textContent).toBe('IMG_1.jpg');

    fixture.componentInstance['siguiente']();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.archivo')?.textContent).toBe('IMG_2.jpg');
  });

  it('con una sola foto no muestra las flechas de navegación', async () => {
    await create({ fotos: [foto(1)], index: 0 });

    expect(fixture.nativeElement.querySelector('.nav')).toBeNull();
  });

  it('el botón de cerrar es una cruz (arriba a la derecha) y el contador va arriba (no en una barra inferior)', async () => {
    await create({ fotos: [foto(1), foto(2)], index: 0 });

    const cerrar = fixture.nativeElement.querySelector('.cerrar') as HTMLButtonElement;
    expect(cerrar.getAttribute('aria-label')).toBe('Cerrar');
    expect(fixture.nativeElement.querySelector('.contador')?.textContent).toContain('1 / 2');
    // ya no hay barra de acciones inferior con "Cerrar" de texto
    expect(fixture.nativeElement.querySelector('mat-dialog-actions')).toBeNull();
  });

  it('muestra el nombre legal ("Familia de X") en la esquina cuando hay sesión activa', async () => {
    await create({ fotos: [foto(1)], index: 0 });
    TestBed.inject(FamiliaSessionStore).setSession({
      token: 't',
      validTo: new Date(Date.now() + 60000).toISOString(),
      eventoId: 1,
      nombreEvento: 'Egresados 2026',
      participantes: [{ id: 100, nombre: 'Ana Pérez' }],
    });
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.legal')?.textContent).toBe('Familia de Ana Pérez');
  });

  it('muestra el tilde de "ya en el carrito" solo para la foto que ya tiene copias', async () => {
    await create({ fotos: [foto(1), foto(2)], index: 0 });
    expect(fixture.nativeElement.querySelector('.en-carrito')).toBeNull();

    TestBed.inject(CarritoStore).agregar(1, 10, 1);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('.en-carrito')).not.toBeNull();

    fixture.componentInstance['siguiente'](); // foto 2, sin copias en el carrito
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('.en-carrito')).toBeNull();
  });

  it('las flechas de teclado navegan el carrusel (sin depender del foco)', async () => {
    await create({ fotos: [foto(1), foto(2)], index: 0 });

    document.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowRight' }));
    fixture.detectChanges();

    expect(fixture.componentInstance['actual']().id).toBe(2);
  });
});
