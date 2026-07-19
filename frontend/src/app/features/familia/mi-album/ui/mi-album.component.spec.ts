import type { Mock } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MatBottomSheet } from '@angular/material/bottom-sheet';
import { MatDialog } from '@angular/material/dialog';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { provideRouter, Router } from '@angular/router';
import { of } from 'rxjs';
import {
  CarritoStore,
  FamiliaCatalogoService,
  FamiliaGaleriaService,
  FamiliaSessionStore,
  FotoFamilia,
  ParticipanteSesion,
  TamanoPrecio,
} from '../../../../core/familia';
import { NotificationService } from '../../../../shared/feedback/notification.service';
import { MiAlbumComponent } from './mi-album.component';

function foto(parcial: Partial<FotoFamilia>): FotoFamilia {
  return {
    id: 1,
    grupoId: 3,
    participanteId: null,
    nombreArchivoOriginal: 'IMG_001.jpg',
    ancho: 800,
    alto: 600,
    ...parcial,
  };
}

const UNA_FAMILIA: ParticipanteSesion[] = [{ id: 100, nombre: 'Ana Pérez' }];
const TAMANOS: TamanoPrecio[] = [
  { id: 1, nombre: '10x15', precioUnitario: 500, orden: 0, activo: true },
  { id: 2, nombre: '20x30', precioUnitario: 1200, orden: 1, activo: true },
];

describe('MiAlbumComponent', () => {
  let fixture: ComponentFixture<MiAlbumComponent>;
  let dialogOpen: Mock;
  let bottomSheetOpen: Mock;
  let notify: { success: Mock; error: Mock };

  async function create(
    fotos: FotoFamilia[] = [],
    participantes: ParticipanteSesion[] = UNA_FAMILIA,
    tamanosPrecios: TamanoPrecio[] = TAMANOS,
  ): Promise<{ cmp: MiAlbumComponent; el: HTMLElement }> {
    // No reemplazar el global `URL` entero (rompería `new URL()`, que `provideRouter` necesita para
    // la navegación fake): solo agregar los estáticos que jsdom no implementa.
    URL.createObjectURL = vi.fn().mockReturnValue('blob:mock');
    URL.revokeObjectURL = vi.fn();
    dialogOpen = vi.fn().mockName('dialog.open');
    bottomSheetOpen = vi.fn().mockName('bottomSheet.open');
    notify = { success: vi.fn(), error: vi.fn() };

    await TestBed.configureTestingModule({
      imports: [MiAlbumComponent],
      providers: [
        provideRouter([]),
        provideNoopAnimations(),
        {
          provide: FamiliaGaleriaService,
          useValue: {
            listar: vi.fn().mockReturnValue(of(fotos)),
            derivado: vi.fn().mockReturnValue(of(new Blob(['x']))),
          },
        },
        { provide: FamiliaCatalogoService, useValue: { listarTamanosPrecios: vi.fn().mockReturnValue(of(tamanosPrecios)) } },
        { provide: MatDialog, useValue: { open: dialogOpen } },
        { provide: MatBottomSheet, useValue: { open: bottomSheetOpen } },
        { provide: NotificationService, useValue: notify },
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

    const botones = el.querySelectorAll('.grilla__tile');
    (botones[1] as HTMLButtonElement).click();

    expect(dialogOpen).toHaveBeenCalledTimes(1);
    const data = dialogOpen.mock.calls[0][1].data;
    expect(data.fotos).toEqual(fotos);
    expect(data.index).toBe(1);
    expect(data.tamanosPrecios).toEqual(TAMANOS);
  });

  it('el botón "agregar al carrito" de un tile abre el bottom sheet para esa foto, sin abrir el preview', async () => {
    const fotos = [foto({ id: 1, participanteId: 100 }), foto({ id: 2, participanteId: null })];
    const { el } = await create(fotos);

    const botonAgregar = el.querySelectorAll('.grilla__add')[1] as HTMLButtonElement;
    botonAgregar.click();

    expect(bottomSheetOpen).toHaveBeenCalledTimes(1);
    expect(dialogOpen).not.toHaveBeenCalled();
    const data = bottomSheetOpen.mock.calls[0][1].data;
    expect(data.fotoId).toBe(2);
    expect(data.tamanosPrecios).toEqual(TAMANOS);
  });

  it('una foto ya agregada al carrito muestra un tilde persistente en la grilla normal (no depende de "Seleccionar varias")', async () => {
    const fotos = [foto({ id: 1, participanteId: 100 }), foto({ id: 2, participanteId: null })];
    const { el } = await create(fotos);
    const carrito = TestBed.inject(CarritoStore);

    expect(el.querySelectorAll('.grilla__en-carrito').length).toBe(0);

    carrito.agregar(1, TAMANOS[0].id, 1);
    fixture.detectChanges();

    const tildes = el.querySelectorAll('.grilla__en-carrito');
    expect(tildes.length).toBe(1);
    expect(tildes[0].querySelector('.grilla__check-marca')?.textContent).toBe('✓');
  });

  it('sin ítems en el carrito no muestra el FAB', async () => {
    const { el } = await create([foto({ id: 1, participanteId: 100 })]);

    expect(el.querySelector('.mi-album__carrito-fab')).toBeNull();
  });

  it('el FAB de carrito muestra solo la cantidad (sin monto, estilo ML/Amazon) y navega a /carrito', async () => {
    const { el, cmp } = await create([foto({ id: 1, participanteId: 100 })]);
    const carrito = TestBed.inject(CarritoStore);
    carrito.agregar(1, 1, 3);
    fixture.detectChanges();

    expect(cmp['carrito'].totalItems()).toBe(3);
    const fab = el.querySelector('.mi-album__carrito-fab') as HTMLButtonElement;
    expect(fab.textContent).toContain('3');
    expect(fab.textContent).not.toContain('$');

    const navSpy = vi
      .spyOn(TestBed.inject(Router), 'navigateByUrl')
      .mockReturnValue(undefined as unknown as Promise<boolean>);
    fab.click();
    expect(navSpy).toHaveBeenCalledWith('/carrito');
  });

  it('"Seleccionar varias" tilda fotos sin abrir el preview y muestra el contador', async () => {
    const fotos = [foto({ id: 1, participanteId: 100 }), foto({ id: 2, participanteId: null })];
    const { el, cmp } = await create(fotos);

    (el.querySelector('.mi-album__seleccion-toggle') as HTMLButtonElement).click();
    fixture.detectChanges();
    expect(cmp['modoSeleccion']()).toBe(true);

    const tiles = el.querySelectorAll('.grilla__tile');
    (tiles[0] as HTMLButtonElement).click();
    (tiles[1] as HTMLButtonElement).click();
    fixture.detectChanges();

    expect(dialogOpen).not.toHaveBeenCalled();
    expect(cmp['seleccionadas']().size).toBe(2);
    // el contador vive solo en el botón, no repetido en un texto aparte
    expect(el.querySelector('.mi-album__seleccion-agregar')?.textContent?.trim()).toBe('Agregar 2 fotos');
    // en modo selección no se ve el botón individual "agregar al carrito"
    expect(el.querySelector('.grilla__add')).toBeNull();
    // el tile tocado muestra el tilde y el borde de "seleccionada"
    const primerCheck = el.querySelectorAll('.grilla__check')[0] as HTMLElement;
    expect(primerCheck.querySelector('.grilla__check-marca')?.textContent).toBe('✓');
    expect(el.querySelectorAll('.grilla__tile--seleccionada').length).toBe(2);
  });

  it('el selector de tamaño del lote arranca en el primero del catálogo y se puede cambiar antes de agregar', async () => {
    const fotos = [foto({ id: 1, participanteId: 100 })];
    const { el, cmp } = await create(fotos);
    const carrito = TestBed.inject(CarritoStore);

    expect(cmp['tamanoLoteId']()).toBe(TAMANOS[0].id);

    (el.querySelector('.mi-album__seleccion-toggle') as HTMLButtonElement).click();
    fixture.detectChanges();
    (el.querySelector('.grilla__tile') as HTMLButtonElement).click();
    fixture.detectChanges();

    // cambio de tamaño desde el selector (se ejercita el signal directo: `tbi-select` abre un
    // overlay de CDK que no vale la pena manejar acá — el binding `[(value)]` ya está cubierto).
    cmp['tamanoLoteId'].set(TAMANOS[1].id);
    (el.querySelector('.mi-album__seleccion-agregar') as HTMLButtonElement).click();
    fixture.detectChanges();

    expect(carrito.cantidadDe(1, TAMANOS[1].id)).toBe(1);
    expect(carrito.cantidadDe(1, TAMANOS[0].id)).toBe(0);
  });

  it('"Agregar seleccionadas" manda una línea por foto al tamaño elegido y sale del modo selección', async () => {
    const fotos = [foto({ id: 1, participanteId: 100 }), foto({ id: 2, participanteId: null })];
    const { el, cmp } = await create(fotos);
    const carrito = TestBed.inject(CarritoStore);

    (el.querySelector('.mi-album__seleccion-toggle') as HTMLButtonElement).click();
    fixture.detectChanges();
    const tiles = el.querySelectorAll('.grilla__tile');
    (tiles[0] as HTMLButtonElement).click();
    (tiles[1] as HTMLButtonElement).click();
    fixture.detectChanges();

    (el.querySelector('.mi-album__seleccion-agregar') as HTMLButtonElement).click();
    fixture.detectChanges();

    expect(carrito.cantidadDe(1, TAMANOS[0].id)).toBe(1);
    expect(carrito.cantidadDe(2, TAMANOS[0].id)).toBe(1);
    expect(cmp['modoSeleccion']()).toBe(false);
    expect(cmp['seleccionadas']().size).toBe(0);
    // el mensaje ya no dice "ajustar el tamaño después" (el tamaño ya se elige ANTES de agregar)
    expect(notify.success).toHaveBeenCalledWith(
      `2 fotos agregadas al carrito en ${TAMANOS[0].nombre} (1 copia c/u). Podés sumar más copias desde el carrito.`,
    );
  });

  it('muestra el nombre de archivo sobre la miniatura en la grilla', async () => {
    const { el } = await create([foto({ id: 1, participanteId: 100, nombreArchivoOriginal: 'CEREMONIA_007.jpg' })]);

    expect(el.querySelector('.grilla__filename')?.textContent).toBe('CEREMONIA_007.jpg');
  });

  it('en vista lista muestra nombre de participante y nombre de archivo', async () => {
    const { el } = await create([foto({ id: 1, participanteId: 100, nombreArchivoOriginal: 'CEREMONIA_007.jpg' })]);

    (el.querySelector('[aria-label="Ver en lista"]') as HTMLButtonElement).click();
    fixture.detectChanges();

    expect(el.querySelector('.grilla__etiqueta-nombre')?.textContent).toBe('Ana Pérez');
    expect(el.querySelector('.grilla__etiqueta-archivo')?.textContent).toBe('CEREMONIA_007.jpg');
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

  it('el botón "Muy chico" usa auto-fill (tantas columnas como entren) y oculta chip/nombre de archivo', async () => {
    const { el, cmp } = await create([
      foto({ id: 1, participanteId: null, nombreArchivoOriginal: 'CEREMONIA_007.jpg' }),
    ]);

    (el.querySelector('[aria-label="Fotos muy chicas, tantas como entren por fila"]') as HTMLButtonElement).click();
    fixture.detectChanges();

    expect(cmp['densidad']()).toBe('compacta');
    expect(cmp['gridTemplateColumns']()).toBe('repeat(auto-fill, minmax(72px, 1fr))');
    expect(el.querySelector('.grilla__chip')).toBeNull();
    expect(el.querySelector('.grilla__filename')).toBeNull();
  });

  it('el botón "Grande" pone 2 columnas en la grilla', async () => {
    const { el, cmp } = await create([foto({ id: 1, participanteId: 100 })]);

    (el.querySelector('[aria-label="Fotos grandes, 2 por fila"]') as HTMLButtonElement).click();
    fixture.detectChanges();

    expect(cmp['densidad']()).toBe(2);
    expect(cmp['gridTemplateColumns']()).toBe('repeat(2, 1fr)');
  });
});
