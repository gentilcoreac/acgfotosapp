import type { Mock } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MatDialog } from '@angular/material/dialog';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { of } from 'rxjs';
import { NotificationService } from '../../../../shared/feedback/notification.service';
import { ImagenAmpliadaDialogComponent } from '../../../../shared/ui/imagen-ampliada-dialog/imagen-ampliada-dialog.component';
import { GruposService } from '../../grupos/data/grupos.service';
import { TarjetasGrupo } from '../../grupos/domain/grupo.model';
import { EventosService } from '../../eventos/data/eventos.service';
import { TarjetasComponent } from './tarjetas.component';

const TARJETAS: TarjetasGrupo = {
  grupoId: 3,
  nombreGrupo: '7ºA',
  nombreEvento: 'Graduación 2026',
  tarjetas: [
    {
      participanteId: 21,
      nombre: 'Ana Pérez',
      codigo: 'K7F3-9QMD',
      urlCanje: 'http://localhost:4200/canje/K7F3-9QMD',
      qrPngBase64: 'aWNvbm8=',
    },
    {
      participanteId: 22,
      nombre: 'Bruno Díaz',
      codigo: null,
      urlCanje: null,
      qrPngBase64: null,
    },
  ],
};

describe('TarjetasComponent', () => {
  let fixture: ComponentFixture<TarjetasComponent>;
  let getTarjetas: Mock;
  let notify: { success: Mock; error: Mock };
  let dialogOpen: Mock;

  async function setup(): Promise<void> {
    getTarjetas = vi.fn().mockName('getTarjetas').mockReturnValue(of(TARJETAS));
    notify = { success: vi.fn(), error: vi.fn() };
    dialogOpen = vi.fn().mockName('dialogOpen');

    await TestBed.configureTestingModule({
      imports: [TarjetasComponent],
      providers: [
        provideNoopAnimations(),
        { provide: NotificationService, useValue: notify },
        { provide: MatDialog, useValue: { open: dialogOpen } },
        {
          provide: EventosService,
          useValue: {
            crud: {
              getAll: vi
                .fn()
                .mockReturnValue(of({ items: [{ id: 7, nombre: 'Graduación 2026', estado: 1 }], totalCount: 1 })),
            },
          },
        },
        {
          provide: GruposService,
          useValue: {
            crud: {
              getAllByCriteria: vi
                .fn()
                .mockReturnValue(of({ items: [{ id: 3, eventoId: 7, nombre: '7ºA' }], totalCount: 1 })),
            },
            getTarjetas,
          },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(TarjetasComponent);
    fixture.detectChanges();
  }

  function seleccionarGrupo(): void {
    fixture.componentInstance['eventoId'].set(7);
    fixture.detectChanges();
    fixture.componentInstance['grupoId'].set(3);
    fixture.detectChanges();
    fixture.detectChanges(); // ciclo extra: resources disparados por señal seteada directo del test
  }

  it('sin grupo no pide tarjetas; al elegirlo las trae y renderiza código y QR', async () => {
    await setup();
    expect(getTarjetas).not.toHaveBeenCalled();

    seleccionarGrupo();

    expect(getTarjetas).toHaveBeenCalledWith(3);
    const html = fixture.nativeElement as HTMLElement;
    expect(html.textContent).toContain('Ana Pérez');
    expect(html.textContent).toContain('K7F3-9QMD');
    const qr = html.querySelector('img.tarjeta__qr') as HTMLImageElement;
    expect(qr.src).toContain('data:image/png;base64,aWNvbm8=');
  });

  it('avisa cuántos participantes quedan fuera por no tener código activo', async () => {
    await setup();
    seleccionarGrupo();

    expect(fixture.componentInstance['sinCodigo']().map((t) => t.nombre)).toEqual([
      'Bruno Díaz',
    ]);
    expect((fixture.nativeElement as HTMLElement).textContent).toContain(
      '1 participante(s) sin código activo',
    );
  });

  it('imprimir abre una ventana con las tarjetas (solo las que tienen código) y llama print', async () => {
    await setup();
    seleccionarGrupo();

    const write = vi.fn();
    const ventana = { document: { write, close: vi.fn() }, focus: vi.fn(), print: vi.fn() };
    const open = vi.spyOn(window, 'open').mockReturnValue(ventana as unknown as Window);
    try {
      fixture.componentInstance['imprimir']();

      expect(open).toHaveBeenCalled();
      const html = write.mock.calls[0][0] as string;
      expect(html).toContain('K7F3-9QMD');
      expect(html).toContain('Ana Pérez');
      expect(html).not.toContain('Bruno Díaz'); // sin código: no se imprime
      expect(html).toContain('data:image/png;base64,aWNvbm8=');
      expect(ventana.print).toHaveBeenCalled();
    } finally {
      open.mockRestore();
    }
  });

  it('al hacer click en el QR lo abre ampliado, sin recorrido (imagen aislada)', async () => {
    await setup();
    seleccionarGrupo();

    (fixture.nativeElement.querySelector('.tarjeta__qr-btn') as HTMLButtonElement).click();

    expect(dialogOpen).toHaveBeenCalledWith(
      ImagenAmpliadaDialogComponent,
      expect.objectContaining({
        data: { src: 'data:image/png;base64,aWNvbm8=', alt: 'QR de acceso de Ana Pérez' },
      }),
    );
  });

  it('sin código no hay QR que ampliar (no se ofrece el botón)', async () => {
    await setup();
    seleccionarGrupo();

    const tarjetas = fixture.nativeElement.querySelectorAll('.tarjeta');
    const sinCodigo = tarjetas[1] as HTMLElement;
    expect(sinCodigo.querySelector('.tarjeta__qr-btn')).toBeNull();
  });

  it('si el popup está bloqueado avisa con un error', async () => {
    await setup();
    seleccionarGrupo();

    const open = vi.spyOn(window, 'open').mockReturnValue(null);
    try {
      fixture.componentInstance['imprimir']();
      expect(notify.error).toHaveBeenCalled();
    } finally {
      open.mockRestore();
    }
  });
});
