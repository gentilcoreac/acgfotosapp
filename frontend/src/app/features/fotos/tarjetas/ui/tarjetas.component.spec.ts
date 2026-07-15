import type { Mock } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { of } from 'rxjs';
import { NotificationService } from '../../../../shared/feedback/notification.service';
import { CursosService } from '../../cursos/data/cursos.service';
import { TarjetasCurso } from '../../cursos/domain/curso.model';
import { EventosService } from '../../eventos/data/eventos.service';
import { TarjetasComponent } from './tarjetas.component';

const TARJETAS: TarjetasCurso = {
  cursoId: 3,
  nombreCurso: '7ºA',
  nombreEvento: 'Graduación 2026',
  tarjetas: [
    {
      albumId: 21,
      nombreAlumno: 'Ana Pérez',
      codigo: 'K7F3-9QMD',
      urlCanje: 'http://localhost:4200/canje/K7F3-9QMD',
      qrPngBase64: 'aWNvbm8=',
    },
    {
      albumId: 22,
      nombreAlumno: 'Bruno Díaz',
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

  async function setup(): Promise<void> {
    getTarjetas = vi.fn().mockName('getTarjetas').mockReturnValue(of(TARJETAS));
    notify = { success: vi.fn(), error: vi.fn() };

    await TestBed.configureTestingModule({
      imports: [TarjetasComponent],
      providers: [
        provideNoopAnimations(),
        { provide: NotificationService, useValue: notify },
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
          provide: CursosService,
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

  function seleccionarCurso(): void {
    fixture.componentInstance['eventoId'].set(7);
    fixture.detectChanges();
    fixture.componentInstance['cursoId'].set(3);
    fixture.detectChanges();
    fixture.detectChanges(); // ciclo extra: resources disparados por señal seteada directo del test
  }

  it('sin curso no pide tarjetas; al elegirlo las trae y renderiza código y QR', async () => {
    await setup();
    expect(getTarjetas).not.toHaveBeenCalled();

    seleccionarCurso();

    expect(getTarjetas).toHaveBeenCalledWith(3);
    const html = fixture.nativeElement as HTMLElement;
    expect(html.textContent).toContain('Ana Pérez');
    expect(html.textContent).toContain('K7F3-9QMD');
    const qr = html.querySelector('img.tarjeta__qr') as HTMLImageElement;
    expect(qr.src).toContain('data:image/png;base64,aWNvbm8=');
  });

  it('avisa cuántos álbumes quedan fuera por no tener código activo', async () => {
    await setup();
    seleccionarCurso();

    expect(fixture.componentInstance['sinCodigo']().map((t) => t.nombreAlumno)).toEqual([
      'Bruno Díaz',
    ]);
    expect((fixture.nativeElement as HTMLElement).textContent).toContain(
      '1 álbum(es) sin código activo',
    );
  });

  it('imprimir abre una ventana con las tarjetas (solo las que tienen código) y llama print', async () => {
    await setup();
    seleccionarCurso();

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

  it('si el popup está bloqueado avisa con un error', async () => {
    await setup();
    seleccionarCurso();

    const open = vi.spyOn(window, 'open').mockReturnValue(null);
    try {
      fixture.componentInstance['imprimir']();
      expect(notify.error).toHaveBeenCalled();
    } finally {
      open.mockRestore();
    }
  });
});
