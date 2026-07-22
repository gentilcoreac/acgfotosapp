import type { Mock } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { of } from 'rxjs';
import { NotificationService } from '../../../../shared/feedback/notification.service';
import { PedidosService } from '../data/pedidos.service';
import { ESTADO_PEDIDO, ListaImpresion } from '../domain/pedido.model';
import { ListaImpresionDialogComponent, ListaImpresionDialogData } from './lista-impresion-dialog.component';

describe('ListaImpresionDialogComponent', () => {
  let fixture: ComponentFixture<ListaImpresionDialogComponent>;
  let getListaImpresion: Mock;
  let dialogRef: { close: Mock };
  let notify: { success: Mock; error: Mock };

  afterEach(() => {
    vi.restoreAllMocks();
  });

  const listaEjemplo: ListaImpresion = {
    agregado: [
      {
        fotoId: 1,
        nombreArchivoOriginal: 'DSC001.jpg',
        anchoFoto: 800,
        altoFoto: 1200,
        tamanoPrecioNombre: '10x15',
        cantidadTotal: 5,
      },
    ],
    detalle: [
      {
        participanteId: 9,
        participanteNombre: 'Ana Pérez',
        lineas: [{ fotoId: 1, nombreArchivoOriginal: 'DSC001.jpg', tamanoPrecioNombre: '10x15', cantidad: 5 }],
      },
    ],
  };

  const data: ListaImpresionDialogData = { eventoId: 3, eventoNombre: 'Egresados 2026' };

  async function setup(): Promise<void> {
    getListaImpresion = vi.fn().mockName('getListaImpresion').mockReturnValue(of(listaEjemplo));
    dialogRef = { close: vi.fn() };
    notify = { success: vi.fn(), error: vi.fn() };

    await TestBed.configureTestingModule({
      imports: [ListaImpresionDialogComponent],
      providers: [
        provideNoopAnimations(),
        { provide: MAT_DIALOG_DATA, useValue: data },
        { provide: MatDialogRef, useValue: dialogRef },
        { provide: PedidosService, useValue: { getListaImpresion } },
        { provide: NotificationService, useValue: notify },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(ListaImpresionDialogComponent);
    fixture.detectChanges();
  }

  it('arranca con Pagado preseleccionado y "Generar" habilitado', async () => {
    await setup();
    expect(fixture.componentInstance['estaSeleccionado'](ESTADO_PEDIDO.Pagado)).toBe(true);
    expect(fixture.componentInstance['puedeGenerar']()).toBe(true);
  });

  it('destildar el único estado tildado deshabilita "Generar"', async () => {
    await setup();
    fixture.componentInstance['toggleEstado'](ESTADO_PEDIDO.Pagado);
    expect(fixture.componentInstance['puedeGenerar']()).toBe(false);
  });

  it('generar llama al servicio con los estados tildados y guarda el resultado', async () => {
    await setup();
    fixture.componentInstance['toggleEstado'](ESTADO_PEDIDO.Pendiente); // Pagado + Pendiente

    fixture.componentInstance['generar']();

    expect(getListaImpresion).toHaveBeenCalledWith(3, expect.arrayContaining([ESTADO_PEDIDO.Pagado, ESTADO_PEDIDO.Pendiente]));
    expect(fixture.componentInstance['resultado']()).toEqual(listaEjemplo);
  });

  it('tildar/destildar un estado invalida el resultado ya generado', async () => {
    await setup();
    fixture.componentInstance['generar']();
    expect(fixture.componentInstance['resultado']()).not.toBeNull();

    fixture.componentInstance['toggleEstado'](ESTADO_PEDIDO.Impreso);

    expect(fixture.componentInstance['resultado']()).toBeNull();
  });

  it('el resumen muestra la cantidad de líneas y de participantes tras generar', async () => {
    await setup();
    fixture.componentInstance['generar']();
    fixture.detectChanges();

    const texto = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(texto).toContain('1 líneas para el laboratorio');
    expect(texto).toContain('1 participantes');
  });

  it('descargarCsv dispara la descarga del agregado como .csv', async () => {
    await setup();
    fixture.componentInstance['generar']();
    const clicks: string[] = [];
    vi.spyOn(URL, 'createObjectURL').mockReturnValue('blob:x');
    vi.spyOn(URL, 'revokeObjectURL').mockImplementation(() => undefined);
    vi.spyOn(HTMLAnchorElement.prototype, 'click').mockImplementation(function (this: HTMLAnchorElement) {
      clicks.push(this.download);
    });

    fixture.componentInstance['descargarCsv']();

    expect(clicks).toEqual(['lista-impresion-egresados-2026.csv']);
  });

  it('imprimir avisa si el navegador bloqueó el popup', async () => {
    await setup();
    fixture.componentInstance['generar']();
    vi.spyOn(window, 'open').mockReturnValue(null);

    fixture.componentInstance['imprimir']();

    expect(notify.error).toHaveBeenCalled();
  });

  it('imprimir escribe el documento y dispara print() en la ventana nueva', async () => {
    await setup();
    fixture.componentInstance['generar']();
    const write = vi.fn();
    const close = vi.fn();
    const focus = vi.fn();
    const print = vi.fn();
    vi.spyOn(window, 'open').mockReturnValue({
      document: { write, close },
      focus,
      print,
    } as unknown as Window);

    fixture.componentInstance['imprimir']();

    expect(write).toHaveBeenCalled();
    expect(write.mock.calls[0][0]).toContain('Egresados 2026');
    expect(print).toHaveBeenCalled();
  });

  it('cerrar cierra el diálogo', async () => {
    await setup();
    fixture.componentInstance['cerrar']();
    expect(dialogRef.close).toHaveBeenCalled();
  });
});
