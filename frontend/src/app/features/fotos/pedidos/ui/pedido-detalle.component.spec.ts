import type { Mock } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MAT_DIALOG_DATA, MatDialog, MatDialogRef } from '@angular/material/dialog';
import { of } from 'rxjs';
import { ConfirmService } from '../../../../shared/feedback/confirm.service';
import { NotificationService } from '../../../../shared/feedback/notification.service';
import { PedidosService } from '../data/pedidos.service';
import { ESTADO_PEDIDO, Pedido } from '../domain/pedido.model';
import { PedidoDetalleComponent } from './pedido-detalle.component';

describe('PedidoDetalleComponent', () => {
  let fixture: ComponentFixture<PedidoDetalleComponent>;
  let getById: Mock;
  let cambiarEstado: Mock;
  let dialogRef: { close: Mock };
  let dialogOpenSpy: ReturnType<typeof vi.spyOn>;
  let confirm: Mock;
  let notify: { success: Mock; error: Mock };

  afterEach(() => {
    vi.restoreAllMocks();
  });

  const pedidoPendiente: Pedido = {
    id: 1,
    participanteId: 5,
    participanteNombre: 'Ana Pérez',
    nombreContacto: 'Familia Pérez',
    telefonoContacto: '+54 9 11 5555-5555',
    estado: ESTADO_PEDIDO.Pendiente,
    total: 1000,
    creadoEn: '2026-07-19T12:00:00Z',
    cantidadItems: 1,
    items: [
      {
        fotoId: 9,
        nombreArchivoOriginal: 'DSC001.jpg',
        tamanoPrecioNombre: '10x15',
        cantidad: 2,
        precioUnitarioSnapshot: 500,
      },
    ],
  };

  async function setup(pedido: Pedido = pedidoPendiente, confirmResult = true): Promise<void> {
    getById = vi.fn().mockName('getById').mockReturnValue(of(pedido));
    cambiarEstado = vi
      .fn()
      .mockName('cambiarEstado')
      .mockReturnValue(of({ ...pedido, estado: ESTADO_PEDIDO.Impreso }));
    dialogRef = { close: vi.fn() };
    // `PedidoDetalleComponent` importa `MatDialogModule` para su propio template
    // (mat-dialog-content, etc.), así que un `{ provide: MatDialog, useValue }` en TestBed queda
    // sombreado por el provider del módulo en el injector del propio componente — se intercepta
    // en el prototipo en vez de por DI (mismo motivo por el que no hay precedente de esto en el
    // resto de la app: ningún otro diálogo abre OTRO diálogo desde adentro).
    dialogOpenSpy = vi
      .spyOn(MatDialog.prototype, 'open')
      .mockReturnValue({ afterClosed: () => of(null) } as ReturnType<MatDialog['open']>);
    confirm = vi.fn().mockName('confirm').mockReturnValue(of(confirmResult));
    notify = { success: vi.fn(), error: vi.fn() };

    await TestBed.configureTestingModule({
      imports: [PedidoDetalleComponent],
      providers: [
        { provide: MAT_DIALOG_DATA, useValue: { id: pedido.id } },
        { provide: MatDialogRef, useValue: dialogRef },
        { provide: PedidosService, useValue: { crud: { getById }, cambiarEstado } },
        { provide: NotificationService, useValue: notify },
        { provide: ConfirmService, useValue: { confirm } },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(PedidoDetalleComponent);
    fixture.detectChanges();
  }

  it('carga el pedido por id y muestra sus líneas agrupadas por foto', async () => {
    await setup();
    expect(getById).toHaveBeenCalledWith(1);
    const el: HTMLElement = fixture.nativeElement;
    expect(el.textContent).toContain('Ana Pérez');
    expect(el.textContent).toContain('DSC001.jpg');
    expect(el.textContent).toContain('10x15');
  });

  it('agrupa varias líneas de la misma foto en un solo grupo', async () => {
    await setup({
      ...pedidoPendiente,
      items: [
        { fotoId: 9, nombreArchivoOriginal: 'DSC001.jpg', tamanoPrecioNombre: '10x15', cantidad: 2, precioUnitarioSnapshot: 500 },
        { fotoId: 9, nombreArchivoOriginal: 'DSC001.jpg', tamanoPrecioNombre: '20x30', cantidad: 1, precioUnitarioSnapshot: 1200 },
      ],
    });
    expect(fixture.componentInstance['grupos']().length).toBe(1);
    expect(fixture.componentInstance['grupos']()[0].lineas.length).toBe(2);
  });

  it('un pedido Pendiente solo puede marcarse Impreso', async () => {
    await setup();
    expect(fixture.componentInstance['puedeMarcarImpreso']()).toBe(true);
    expect(fixture.componentInstance['puedeMarcarEntregado']()).toBe(false);
  });

  it('un pedido Impreso solo puede marcarse Entregado', async () => {
    await setup({ ...pedidoPendiente, estado: ESTADO_PEDIDO.Impreso });
    expect(fixture.componentInstance['puedeMarcarImpreso']()).toBe(false);
    expect(fixture.componentInstance['puedeMarcarEntregado']()).toBe(true);
  });

  it('marcar impreso llama a cambiarEstado, notifica y marca cambios para el padre', async () => {
    await setup();
    fixture.componentInstance['marcarImpreso']();

    expect(cambiarEstado).toHaveBeenCalledWith(1, ESTADO_PEDIDO.Impreso);
    expect(notify.success).toHaveBeenCalledWith('Pedido marcado como impreso.');

    fixture.componentInstance['cerrar']();
    expect(dialogRef.close).toHaveBeenCalledWith(true);
  });

  it('cerrar sin cambios no marca la tabla del padre para refrescar', async () => {
    await setup();
    fixture.componentInstance['cerrar']();
    expect(dialogRef.close).toHaveBeenCalledWith(false);
  });

  it('abrirPreview abre el diálogo compartido de foto pidiendo la ORIGINAL primero', async () => {
    await setup();
    fixture.componentInstance['abrirPreview'](fixture.componentInstance['grupos']()[0]);

    expect(dialogOpenSpy).toHaveBeenCalled();
    const [, config] = dialogOpenSpy.mock.calls[0];
    expect((config as { data: unknown }).data).toEqual({
      id: 9,
      nombreArchivoOriginal: 'DSC001.jpg',
      varianteInicial: 'original',
    });
  });

  it('la corrección manual de estado NO ofrece el estado actual como opción', async () => {
    await setup();
    const opciones = fixture.componentInstance['estadoManualOptions']();
    expect(opciones.some((o: { value: number }) => o.value === ESTADO_PEDIDO.Pendiente)).toBe(false);
    expect(opciones.length).toBe(4);
  });

  it('la corrección manual está siempre disponible, incluso con el pedido recién Pendiente', async () => {
    await setup(pedidoPendiente);
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Corregir estado');
  });

  it('la corrección manual también está disponible en el estado final (Entregado)', async () => {
    await setup({ ...pedidoPendiente, estado: ESTADO_PEDIDO.Entregado });
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Corregir estado');
  });

  it('corregirEstado pide confirmación y solo cambia el estado si se confirma', async () => {
    await setup(pedidoPendiente, true);

    fixture.componentInstance['corregirEstado'](ESTADO_PEDIDO.Entregado);

    expect(confirm).toHaveBeenCalled();
    expect(cambiarEstado).toHaveBeenCalledWith(1, ESTADO_PEDIDO.Entregado);
  });

  it('corregirEstado no cambia nada si el usuario cancela la confirmación', async () => {
    await setup(pedidoPendiente, false);

    fixture.componentInstance['corregirEstado'](ESTADO_PEDIDO.Entregado);

    expect(confirm).toHaveBeenCalled();
    expect(cambiarEstado).not.toHaveBeenCalled();
  });
});
