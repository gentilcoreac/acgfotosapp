import type { MockedObject } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { provideRouter, Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { ApiError } from '../../../../core/models';
import {
  CarritoStore,
  FamiliaCatalogoService,
  FamiliaGaleriaService,
  FotoFamilia,
  PedidoConfirmado,
  PedidoService,
  TamanoPrecio,
} from '../../../../core/familia';
import { CarritoComponent } from './carrito.component';

function foto(parcial: Partial<FotoFamilia>): FotoFamilia {
  return {
    id: 1,
    grupoId: 1,
    participanteId: 100,
    nombreArchivoOriginal: 'IMG_001.jpg',
    ancho: 800,
    alto: 600,
    ...parcial,
  };
}

const FOTOS: FotoFamilia[] = [foto({ id: 1 }), foto({ id: 2 })];
const TAMANOS: TamanoPrecio[] = [
  { id: 10, nombre: '10x15', precioUnitario: 500, orden: 0, activo: true },
  { id: 20, nombre: '20x30', precioUnitario: 1200, orden: 1, activo: true },
];

describe('CarritoComponent', () => {
  let fixture: ComponentFixture<CarritoComponent>;
  let pedidoSpy: MockedObject<PedidoService>;

  /** `sembrar` corre DESPUÉS de `configureTestingModule` (ya se puede injectar) y ANTES de crear el
   * componente, así el carrito ya tiene líneas cuando el componente arranca a leerlo. */
  function create(sembrar?: (carrito: CarritoStore) => void): { cmp: CarritoComponent; el: HTMLElement } {
    // No reemplazar el global `URL` entero (rompería `new URL()`, que `provideRouter` necesita para
    // la navegación fake): solo agregar los estáticos que jsdom no implementa.
    URL.createObjectURL = vi.fn().mockReturnValue('blob:mock');
    URL.revokeObjectURL = vi.fn();

    pedidoSpy = { confirmar: vi.fn() } as unknown as MockedObject<PedidoService>;

    TestBed.configureTestingModule({
      imports: [CarritoComponent],
      providers: [
        provideRouter([]),
        provideNoopAnimations(),
        {
          provide: FamiliaGaleriaService,
          useValue: { listar: vi.fn().mockReturnValue(of(FOTOS)), derivado: vi.fn().mockReturnValue(of(new Blob(['x']))) },
        },
        { provide: FamiliaCatalogoService, useValue: { listarTamanosPrecios: vi.fn().mockReturnValue(of(TAMANOS)) } },
        { provide: PedidoService, useValue: pedidoSpy },
      ],
    });

    sembrar?.(TestBed.inject(CarritoStore));

    fixture = TestBed.createComponent(CarritoComponent);
    fixture.detectChanges();
    fixture.detectChanges(); // ciclo extra: los rxResource resuelven tras el primer detectChanges
    return { cmp: fixture.componentInstance, el: fixture.nativeElement as HTMLElement };
  }

  beforeEach(() => sessionStorage.clear());
  afterEach(() => sessionStorage.clear());

  it('carrito vacío muestra el mensaje y no pide el form', () => {
    const { el } = create();
    expect(el.textContent).toContain('Todavía no agregaste fotos');
  });

  it('lista las líneas cruzadas con foto+tamaño y calcula el total en vivo', () => {
    const { cmp, el } = create((carrito) => {
      carrito.agregar(1, 10, 2); // 2 * 500 = 1000
      carrito.agregar(2, 20, 1); // 1 * 1200 = 1200
    });

    expect(cmp['lineas']().length).toBe(2);
    expect(cmp['total']()).toBe(2200);
    expect(el.textContent).toContain('2200,00');
  });

  it('muestra el nombre de archivo de la foto en cada línea', () => {
    const { el } = create((carrito) => carrito.agregar(1, 10, 1));

    expect(el.querySelector('.carrito__archivo')?.textContent).toBe('IMG_001.jpg');
  });

  it('sumar/restar/quitar delegan al CarritoStore', () => {
    const { cmp } = create((carrito) => carrito.agregar(1, 10, 2));
    const carrito = TestBed.inject(CarritoStore);

    // `lineas()` recalcula en cada mutación del store: hay que releerla, no reusar un snapshot viejo.
    cmp['sumar'](cmp['lineas']()[0]);
    expect(carrito.cantidadDe(1, 10)).toBe(3);

    cmp['restar'](cmp['lineas']()[0]);
    expect(carrito.cantidadDe(1, 10)).toBe(2);

    cmp['quitar'](cmp['lineas']()[0]);
    expect(carrito.cantidadDe(1, 10)).toBe(0);
  });

  it('confirmar() con el form inválido no llama al servicio', () => {
    const { cmp } = create((carrito) => carrito.agregar(1, 10, 1));

    cmp['confirmar']();

    expect(pedidoSpy.confirmar).not.toHaveBeenCalled();
  });

  it('confirmar() OK manda los items del carrito, lo vacía y navega a /pedido-confirmado con el pedido', () => {
    const { cmp } = create((carrito) => carrito.agregar(1, 10, 2));

    const confirmado: PedidoConfirmado = {
      id: 55,
      estado: 0,
      total: 1000,
      creadoEn: new Date().toISOString(),
      items: [{ fotoId: 1, tamanoPrecioId: 10, cantidad: 2, precioUnitarioSnapshot: 500 }],
    };
    pedidoSpy.confirmar.mockReturnValue(of(confirmado));
    const navSpy = vi
      .spyOn(TestBed.inject(Router), 'navigate')
      .mockReturnValue(undefined as unknown as Promise<boolean>);

    cmp['form'].setValue({ nombreContacto: 'Familia Pérez', telefonoContacto: '1155555555' });
    cmp['confirmar']();
    fixture.detectChanges();

    expect(pedidoSpy.confirmar).toHaveBeenCalledWith({
      nombreContacto: 'Familia Pérez',
      telefonoContacto: '1155555555',
      items: [{ fotoId: 1, tamanoPrecioId: 10, cantidad: 2 }],
    });
    expect(TestBed.inject(CarritoStore).lineas()).toEqual([]);
    expect(navSpy).toHaveBeenCalledWith(['/pedido-confirmado'], { state: { pedido: confirmado } });
  });

  it('ante error de confirmación muestra los mensajes del ApiError', () => {
    const { cmp } = create((carrito) => carrito.agregar(1, 10, 1));

    const apiError: ApiError = { status: 400, message: 'Una de las fotos ya no está disponible.', errors: [] };
    pedidoSpy.confirmar.mockReturnValue(throwError(() => apiError));

    cmp['form'].setValue({ nombreContacto: 'Familia Pérez', telefonoContacto: '1155555555' });
    cmp['confirmar']();

    expect(cmp['errors']()).toEqual(['Una de las fotos ya no está disponible.']);
    expect(cmp['confirmando']()).toBe(false);
  });

  it('el botón "volver" navega a /mi-album', () => {
    const { cmp } = create();
    const navSpy = vi
      .spyOn(TestBed.inject(Router), 'navigateByUrl')
      .mockReturnValue(undefined as unknown as Promise<boolean>);

    cmp['volver']();

    expect(navSpy).toHaveBeenCalledWith('/mi-album');
  });
});
