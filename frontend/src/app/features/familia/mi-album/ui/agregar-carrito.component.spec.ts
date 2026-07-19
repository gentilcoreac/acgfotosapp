import type { MockedObject } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { CarritoStore, TamanoPrecio } from '../../../../core/familia';
import { AgregarCarritoComponent } from './agregar-carrito.component';

const TAMANOS: TamanoPrecio[] = [
  { id: 10, nombre: '10x15', precioUnitario: 500, orden: 0, activo: true },
  { id: 20, nombre: '20x30', precioUnitario: 1200, orden: 1, activo: true },
];

describe('AgregarCarritoComponent', () => {
  let fixture: ComponentFixture<AgregarCarritoComponent>;
  let carritoSpy: MockedObject<CarritoStore>;

  /** `cantidadPorFoto` simula el store real: cada fotoId tiene su propia cantidad por tamaño. */
  function create(
    tamanosPrecios: TamanoPrecio[] = TAMANOS,
    fotoId = 1,
    cantidadPorFoto: Record<number, Record<number, number>> = {},
  ): AgregarCarritoComponent {
    carritoSpy = {
      agregar: vi.fn(),
      actualizarCantidad: vi.fn(),
      cantidadDe: vi.fn((fid: number, tid: number) => cantidadPorFoto[fid]?.[tid] ?? 0),
    } as unknown as MockedObject<CarritoStore>;

    TestBed.configureTestingModule({
      imports: [AgregarCarritoComponent],
      providers: [provideNoopAnimations(), { provide: CarritoStore, useValue: carritoSpy }],
    });

    fixture = TestBed.createComponent(AgregarCarritoComponent);
    fixture.componentRef.setInput('fotoId', fotoId);
    fixture.componentRef.setInput('tamanosPrecios', tamanosPrecios);
    fixture.detectChanges();
    return fixture.componentInstance;
  }

  it('sin catálogo muestra el mensaje de "no hay tamaños"', () => {
    create([]);
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Todavía no hay tamaños disponibles');
  });

  it('preselecciona el primer tamaño del catálogo (default)', () => {
    const cmp = create();
    expect(cmp['tamanoSeleccionadoId']()).toBe(10);
  });

  it('la cantidad ES la del carrito para fotoId+tamaño, no un contador local que arranca en 1', () => {
    const cmp = create(TAMANOS, 1, { 1: { 10: 4 } });
    expect(cmp['cantidad']()).toBe(4);
  });

  it('sin nada en el carrito la cantidad arranca en 0', () => {
    const cmp = create();
    expect(cmp['cantidad']()).toBe(0);
  });

  it('sumar() agrega 1 unidad del tamaño elegido al store', () => {
    const cmp = create(TAMANOS, 7);
    cmp['tamanoSeleccionadoId'].set(20);

    cmp['sumar']();

    expect(carritoSpy.agregar).toHaveBeenCalledWith(7, 20, 1);
  });

  it('restar() resta 1 unidad del tamaño elegido en el store', () => {
    const cmp = create(TAMANOS, 7, { 7: { 10: 3 } });

    cmp['restar']();

    expect(carritoSpy.actualizarCantidad).toHaveBeenCalledWith(7, 10, 2);
  });

  it('cambiar el tamaño elegido (desplegable) muestra la cantidad de ESE tamaño, no la del anterior', () => {
    const cmp = create(TAMANOS, 1, { 1: { 10: 4, 20: 1 } });
    expect(cmp['cantidad']()).toBe(4);

    cmp['tamanoSeleccionadoId'].set(20);
    expect(cmp['cantidad']()).toBe(1);
  });

  it('sin nada en el carrito para esta foto no muestra el resumen "Ya en tu carrito"', () => {
    create();
    expect((fixture.nativeElement as HTMLElement).textContent).not.toContain('Ya en tu carrito');
  });

  it('resumenEnCarrito lista TODOS los tamaños con copias de la foto, no solo el elegido, cada uno en su propio chip', () => {
    const cmp = create(TAMANOS, 1, { 1: { 10: 2, 20: 1 } });
    expect(cmp['resumenEnCarrito']()).toEqual([
      { nombre: '10x15', cantidad: 2 },
      { nombre: '20x30', cantidad: 1 },
    ]);

    const chips = (fixture.nativeElement as HTMLElement).querySelectorAll('.agregar-carrito__resumen-item');
    expect(chips.length).toBe(2);
    expect(chips[0].textContent).toBe('10x15 ×2');
    expect(chips[1].textContent).toBe('20x30 ×1');
  });

  it('BUG corregido: al cambiar de foto (carrusel) la cantidad no arrastra el valor de la foto anterior', () => {
    const cmp = create(TAMANOS, 1, { 1: { 10: 3 }, 2: {} });
    expect(cmp['cantidad']()).toBe(3);

    fixture.componentRef.setInput('fotoId', 2);
    fixture.detectChanges();

    expect(cmp['cantidad']()).toBe(0);
  });
});
