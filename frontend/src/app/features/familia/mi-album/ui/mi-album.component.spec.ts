import type { Mock } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MatDialog } from '@angular/material/dialog';
import { of } from 'rxjs';
import {
  FamiliaGaleriaService,
  FamiliaSessionStore,
  FotoFamilia,
  ParticipanteSesion,
} from '../../../../core/familia';
import { MiAlbumComponent } from './mi-album.component';

function foto(parcial: Partial<FotoFamilia>): FotoFamilia {
  return { id: 1, grupoId: 3, participanteId: null, ancho: 800, alto: 600, ...parcial };
}

const UNA_FAMILIA: ParticipanteSesion[] = [{ id: 100, nombre: 'Ana Pérez' }];

describe('MiAlbumComponent', () => {
  let fixture: ComponentFixture<MiAlbumComponent>;
  let dialogOpen: Mock;

  async function create(
    fotos: FotoFamilia[] = [],
    participantes: ParticipanteSesion[] = UNA_FAMILIA,
  ): Promise<{ cmp: MiAlbumComponent; el: HTMLElement }> {
    vi.stubGlobal('URL', {
      ...URL,
      createObjectURL: vi.fn().mockReturnValue('blob:mock'),
      revokeObjectURL: vi.fn(),
    });
    dialogOpen = vi.fn().mockName('dialog.open');

    await TestBed.configureTestingModule({
      imports: [MiAlbumComponent],
      providers: [
        {
          provide: FamiliaGaleriaService,
          useValue: {
            listar: vi.fn().mockReturnValue(of(fotos)),
            derivado: vi.fn().mockReturnValue(of(new Blob(['x']))),
          },
        },
        { provide: MatDialog, useValue: { open: dialogOpen } },
      ],
    }).compileComponents();

    TestBed.inject(FamiliaSessionStore).setSession({
      token: 't',
      validTo: new Date(Date.now() + 60000).toISOString(),
      eventoId: 1,
      nombreEvento: 'Egresados 2026',
      participantes,
    });

    fixture = TestBed.createComponent(MiAlbumComponent);
    fixture.detectChanges();
    fixture.detectChanges(); // ciclo extra: el rxResource resuelve tras el primer detectChanges
    return { cmp: fixture.componentInstance, el: fixture.nativeElement as HTMLElement };
  }

  beforeEach(() => sessionStorage.clear());
  afterEach(() => {
    sessionStorage.clear();
    vi.unstubAllGlobals();
  });

  it('saluda con el/los nombre(s) de la sesión', async () => {
    const { cmp, el } = await create();
    expect(cmp.saludo()).toBe('Hola, familia de Ana Pérez');
    expect(el.textContent).toContain('Egresados 2026');
  });

  it('con más de un participante los une con "y"', async () => {
    const { cmp } = await create([], [
      { id: 100, nombre: 'Ana Pérez' },
      { id: 101, nombre: 'José López' },
    ]);

    expect(cmp.saludo()).toBe('Hola, familia de Ana Pérez y José López');
  });

  it('sin fotos muestra el mensaje vacío', async () => {
    const { el } = await create([]);
    expect(el.textContent).toContain('Todavía no hay fotos disponibles');
  });

  it('renderiza la grilla con las fotos de la sesión', async () => {
    const { el } = await create([foto({ id: 1, participanteId: 100 }), foto({ id: 2, participanteId: null })]);

    expect(el.querySelectorAll('.grilla__item').length).toBe(2);
  });

  it('marca las grupales con el chip "Grupal"', async () => {
    const { el } = await create([foto({ id: 1, participanteId: null })]);

    expect(el.textContent).toContain('Grupal');
  });

  it('al tocar una foto abre el diálogo de preview con la lista completa y el índice tocado', async () => {
    const fotos = [foto({ id: 1, participanteId: 100 }), foto({ id: 2, participanteId: null })];
    const { el } = await create(fotos);

    const botones = el.querySelectorAll('.grilla__item');
    (botones[1] as HTMLButtonElement).click();

    expect(dialogOpen).toHaveBeenCalledTimes(1);
    const data = dialogOpen.mock.calls[0][1].data;
    expect(data.fotos).toEqual(fotos);
    expect(data.index).toBe(1);
  });

  it('densidad por default es 4 por fila; el botón de lista cambia a vista en lista', async () => {
    const { el, cmp } = await create([foto({ id: 1, participanteId: 100 })]);

    expect(cmp['densidad']()).toBe(4);
    expect(el.querySelector('.grilla')?.classList.contains('grilla--lista')).toBe(false);

    const botonLista = el.querySelector('[aria-label="Ver en lista"]') as HTMLButtonElement;
    botonLista.click();
    fixture.detectChanges();

    expect(cmp['densidad']()).toBe('lista');
    expect(el.querySelector('.grilla')?.classList.contains('grilla--lista')).toBe(true);
    expect(el.textContent).toContain('Ana Pérez'); // en lista se ve el nombre del participante
  });

  it('el botón "Grande" pone 2 columnas en la grilla', async () => {
    const { el, cmp } = await create([foto({ id: 1, participanteId: 100 })]);

    (el.querySelector('[aria-label="Fotos grandes, 2 por fila"]') as HTMLButtonElement).click();
    fixture.detectChanges();

    expect(cmp['densidad']()).toBe(2);
    expect(cmp['gridTemplateColumns']()).toBe('repeat(2, 1fr)');
  });
});
