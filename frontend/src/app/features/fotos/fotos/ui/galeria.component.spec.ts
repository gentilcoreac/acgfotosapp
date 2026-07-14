import type { Mock } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpHeaders, HttpResponse } from '@angular/common/http';
import { MatDialog } from '@angular/material/dialog';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { of } from 'rxjs';
import { ConfirmService } from '../../../../shared/feedback/confirm.service';
import { NotificationService } from '../../../../shared/feedback/notification.service';
import { CursosService } from '../../cursos/data/cursos.service';
import { EventosService } from '../../eventos/data/eventos.service';
import { FotosService } from '../data/fotos.service';
import { EstadoProcesamientoFoto, Foto } from '../domain/foto.model';
import { GaleriaComponent } from './galeria.component';

function foto(parcial: Partial<Foto>): Foto {
  return {
    id: 1,
    eventoId: 7,
    cursoId: 3,
    albumId: null,
    nombreArchivoOriginal: 'a.jpg',
    ancho: 800,
    alto: 600,
    tamanoBytes: 1000,
    estadoProcesamiento: EstadoProcesamientoFoto.Lista,
    errorProcesamiento: null,
    creadoEn: '2026-07-13T00:00:00Z',
    ...parcial,
  };
}

describe('GaleriaComponent', () => {
  let fixture: ComponentFixture<GaleriaComponent>;
  let listar: Mock;
  let borrar: Mock;
  let descargarOriginal: Mock;
  let confirm: Mock;
  let dialogOpen: Mock;
  let notify: { success: Mock; error: Mock };

  const fotos = [
    foto({ id: 1, albumId: null, nombreArchivoOriginal: 'grupal.jpg' }),
    foto({ id: 2, albumId: 21, nombreArchivoOriginal: 'ana.jpg' }),
    foto({
      id: 3,
      albumId: 21,
      nombreArchivoOriginal: 'rota.jpg',
      estadoProcesamiento: EstadoProcesamientoFoto.Error,
      errorProcesamiento: 'no es imagen',
    }),
  ];

  async function setup(): Promise<void> {
    // La grilla renderiza `tbi-foto-img`, que baja blobs y arma object URLs (jsdom no los trae).
    vi.stubGlobal('URL', {
      ...URL,
      createObjectURL: vi.fn().mockReturnValue('blob:mock'),
      revokeObjectURL: vi.fn(),
    });
    listar = vi.fn().mockName('fotos.listar').mockReturnValue(of(fotos));
    borrar = vi.fn().mockName('fotos.borrar').mockReturnValue(of(undefined));
    descargarOriginal = vi
      .fn()
      .mockName('fotos.descargarOriginal')
      .mockReturnValue(
        of(new HttpResponse({ body: new Blob(['x']), headers: new HttpHeaders() })),
      );
    confirm = vi.fn().mockName('confirm').mockReturnValue(of(true));
    dialogOpen = vi.fn().mockName('dialog.open');
    notify = { success: vi.fn(), error: vi.fn() };

    await TestBed.configureTestingModule({
      imports: [GaleriaComponent],
      providers: [
        provideNoopAnimations(),
        { provide: MatDialog, useValue: { open: dialogOpen } },
        { provide: NotificationService, useValue: notify },
        { provide: ConfirmService, useValue: { confirm } },
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
              getById: vi.fn().mockReturnValue(
                of({
                  id: 3,
                  eventoId: 7,
                  nombre: '7ºA',
                  albumes: [{ id: 21, nombreAlumno: 'Ana Pérez' }],
                }),
              ),
            },
          },
        },
        {
          provide: FotosService,
          useValue: {
            listar,
            borrar,
            descargarOriginal,
            derivado: vi.fn().mockReturnValue(of(new Blob(['jpg']))),
          },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(GaleriaComponent);
    fixture.detectChanges();
  }

  afterEach(() => vi.unstubAllGlobals());

  function seleccionarCurso(): void {
    fixture.componentInstance['eventoId'].set(7);
    fixture.detectChanges();
    fixture.componentInstance['cursoId'].set(3);
    fixture.detectChanges();
    fixture.detectChanges(); // ciclo extra: resources disparados por señal seteada directo del test
  }

  it('sin curso elegido no pide fotos', async () => {
    await setup();
    expect(listar).not.toHaveBeenCalled();
  });

  it('elegir curso pide sus fotos; el filtro por álbum se aplica en memoria', async () => {
    await setup();
    seleccionarCurso();

    expect(listar).toHaveBeenCalledWith(3);
    const c = fixture.componentInstance;
    expect(c['fotos']().length).toBe(3); // Todas

    c['albumFiltro'].set(0); // Solo grupales
    expect(c['fotos']().map((f) => f.id)).toEqual([1]);

    c['albumFiltro'].set(21); // Álbum de Ana
    expect(c['fotos']().map((f) => f.id)).toEqual([2, 3]);
    expect(c['nombreAlbum'](c['fotos']()[0])).toBe('Ana Pérez');
  });

  it('borrar con confirmación llama al servicio, notifica y recarga', async () => {
    await setup();
    seleccionarCurso();
    const llamadasPrevias = listar.mock.calls.length;

    fixture.componentInstance['borrar'](fotos[0]);
    fixture.detectChanges(); // el reload() del resource corre en el próximo flush de efectos

    expect(confirm).toHaveBeenCalled();
    expect(borrar).toHaveBeenCalledWith(1);
    expect(notify.success).toHaveBeenCalledWith('Foto eliminada.');
    expect(listar.mock.calls.length).toBeGreaterThan(llamadasPrevias); // reload
  });

  it('si el usuario cancela la confirmación no se borra nada', async () => {
    await setup();
    seleccionarCurso();
    confirm.mockReturnValue(of(false));

    fixture.componentInstance['borrar'](fotos[0]);

    expect(borrar).not.toHaveBeenCalled();
  });

  it('el preview solo se abre para fotos Lista', async () => {
    await setup();
    seleccionarCurso();

    fixture.componentInstance['verPreview'](fotos[2]); // Error
    expect(dialogOpen).not.toHaveBeenCalled();

    fixture.componentInstance['verPreview'](fotos[0]); // Lista
    expect(dialogOpen).toHaveBeenCalledTimes(1);
  });

  it('descargar original delega en el servicio', async () => {
    await setup();
    seleccionarCurso();

    // el stub de object URLs del setup cubre el saveBlobResponse
    fixture.componentInstance['descargarOriginal'](fotos[0]);
    expect(descargarOriginal).toHaveBeenCalledWith(1);
  });
});
