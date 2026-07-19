import type { Mock } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { PedidoConfirmado } from '../../../../core/familia';
import { PedidoConfirmadoComponent } from './pedido-confirmado.component';

const PEDIDO: PedidoConfirmado = {
  id: 55,
  estado: 0,
  total: 1000,
  creadoEn: new Date().toISOString(),
  items: [{ fotoId: 1, tamanoPrecioId: 10, cantidad: 2, precioUnitarioSnapshot: 500 }],
};

describe('PedidoConfirmadoComponent', () => {
  let fixture: ComponentFixture<PedidoConfirmadoComponent>;

  /** Sin una navegación real en curso, el componente lee el pedido del fallback `history.state`
   * (ver `leerPedidoDeNavegacion`) — mismo mecanismo que sobrevive a un F5 en el uso real. */
  function create(pedido: PedidoConfirmado | null): { cmp: PedidoConfirmadoComponent; el: HTMLElement; navSpy: Mock } {
    history.replaceState(pedido ? { pedido } : null, '');

    TestBed.configureTestingModule({
      imports: [PedidoConfirmadoComponent],
      providers: [provideRouter([])],
    });

    const navSpy = vi
      .spyOn(TestBed.inject(Router), 'navigateByUrl')
      .mockReturnValue(undefined as unknown as Promise<boolean>) as unknown as Mock;

    fixture = TestBed.createComponent(PedidoConfirmadoComponent);
    fixture.detectChanges();
    return { cmp: fixture.componentInstance, el: fixture.nativeElement as HTMLElement, navSpy };
  }

  afterEach(() => history.replaceState(null, ''));

  it('con pedido en el state muestra el número y el total, sin redirigir', () => {
    const { el, navSpy } = create(PEDIDO);

    expect(el.textContent).toContain('Pedido #55');
    expect(el.textContent).toContain('1000,00');
    expect(navSpy).not.toHaveBeenCalled();
  });

  it('el botón "Volver a mis fotos" navega a /mi-album', () => {
    const { cmp, navSpy } = create(PEDIDO);

    cmp['volver']();

    expect(navSpy).toHaveBeenCalledWith('/mi-album');
  });

  it('sin pedido en el state (entrada directa a la URL) redirige a /mi-album', () => {
    const { navSpy } = create(null);

    expect(navSpy).toHaveBeenCalledWith('/mi-album');
  });
});
