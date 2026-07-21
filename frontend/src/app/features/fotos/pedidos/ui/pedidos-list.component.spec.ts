import type { Mock } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MatDialog } from '@angular/material/dialog';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { of } from 'rxjs';
import { QueryParams } from '../../../../core/models/query-params.model';
import { NotificationService } from '../../../../shared/feedback/notification.service';
import { EventosService } from '../../eventos/data/eventos.service';
import { PedidosService } from '../data/pedidos.service';
import { ESTADO_PEDIDO, Pedido } from '../domain/pedido.model';
import { PedidosListComponent } from './pedidos-list.component';

describe('PedidosListComponent', () => {
  let fixture: ComponentFixture<PedidosListComponent>;
  let getAllByCriteria: Mock;
  let cambiarEstado: Mock;
  let getAllEventos: Mock;
  let notify: { success: Mock; error: Mock };

  const pedidoBase: Pedido = {
    id: 1,
    participanteId: 5,
    participanteNombre: 'Ana Pérez',
    nombreContacto: 'Familia Pérez',
    telefonoContacto: '+54 9 11 5555-5555',
    estado: ESTADO_PEDIDO.Pendiente,
    total: 1000,
    creadoEn: '2026-07-19T12:00:00Z',
    cantidadItems: 2,
  };

  async function setup(): Promise<void> {
    getAllByCriteria = vi
      .fn()
      .mockName('getAllByCriteria')
      .mockReturnValue(of({ items: [pedidoBase], totalCount: 1 }));
    cambiarEstado = vi.fn().mockName('cambiarEstado').mockReturnValue(of({ ...pedidoBase }));
    getAllEventos = vi
      .fn()
      .mockName('eventos.getAll')
      .mockReturnValue(of({ items: [{ id: 7, nombre: 'Graduación 2026', estado: 1 }], totalCount: 1 }));
    notify = { success: vi.fn(), error: vi.fn() };

    await TestBed.configureTestingModule({
      imports: [PedidosListComponent],
      providers: [
        provideNoopAnimations(),
        { provide: MatDialog, useValue: { open: () => ({ afterClosed: () => of(null) }) } },
        { provide: NotificationService, useValue: notify },
        { provide: PedidosService, useValue: { crud: { getAllByCriteria }, cambiarEstado } },
        { provide: EventosService, useValue: { crud: { getAll: getAllEventos } } },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(PedidosListComponent);
    fixture.detectChanges();
  }

  it('se crea y la tabla pide datos', async () => {
    await setup();
    expect(fixture.componentInstance).toBeTruthy();
    expect(getAllByCriteria).toHaveBeenCalled();
  });

  it('el filtro por evento viaja en la query como eventoId', async () => {
    await setup();
    fixture.componentInstance['eventoFiltro'].set(7);
    fixture.detectChanges();

    const lastQuery = vi.mocked(getAllByCriteria).mock.lastCall![0] as QueryParams;
    expect(lastQuery['eventoId']).toBe(7);
  });

  it('el filtro por estado viaja en la query, y no viaja cuando es "todos"', async () => {
    await setup();
    fixture.componentInstance['estadoFiltro'].set(ESTADO_PEDIDO.Impreso);
    fixture.detectChanges();
    let lastQuery = vi.mocked(getAllByCriteria).mock.lastCall![0] as QueryParams;
    expect(lastQuery['estado']).toBe(ESTADO_PEDIDO.Impreso);

    fixture.componentInstance['estadoFiltro'].set(null);
    fixture.detectChanges();
    lastQuery = vi.mocked(getAllByCriteria).mock.lastCall![0] as QueryParams;
    expect(lastQuery['estado']).toBeUndefined();
  });

  it('"Marcar impreso" solo se ve para Pendiente/Pagado; "Marcar entregado" solo para Impreso', async () => {
    await setup();
    const rowActions = fixture.componentInstance['rowActions'];
    const marcarImpreso = rowActions.find((a) => a.label === 'Marcar impreso')!;
    const marcarEntregado = rowActions.find((a) => a.label === 'Marcar entregado')!;

    expect(marcarImpreso.hidden!({ ...pedidoBase, estado: ESTADO_PEDIDO.Pendiente })).toBe(false);
    expect(marcarImpreso.hidden!({ ...pedidoBase, estado: ESTADO_PEDIDO.Pagado })).toBe(false);
    expect(marcarImpreso.hidden!({ ...pedidoBase, estado: ESTADO_PEDIDO.Impreso })).toBe(true);

    expect(marcarEntregado.hidden!({ ...pedidoBase, estado: ESTADO_PEDIDO.Impreso })).toBe(false);
    expect(marcarEntregado.hidden!({ ...pedidoBase, estado: ESTADO_PEDIDO.Pendiente })).toBe(true);
  });

  it('"Marcar impreso" llama a cambiarEstado y notifica éxito', async () => {
    await setup();
    const marcarImpreso = fixture.componentInstance['rowActions'].find(
      (a) => a.label === 'Marcar impreso',
    )!;

    marcarImpreso.run!(pedidoBase).subscribe();

    expect(cambiarEstado).toHaveBeenCalledWith(pedidoBase.id, ESTADO_PEDIDO.Impreso);
    expect(notify.success).toHaveBeenCalledWith('Pedido marcado como impreso.');
  });
});
