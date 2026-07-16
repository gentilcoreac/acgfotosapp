import type { Mock } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpHeaders, HttpResponse } from '@angular/common/http';
import { MatDialog } from '@angular/material/dialog';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { of } from 'rxjs';
import { ConfirmService } from '../../../../shared/feedback/confirm.service';
import { NotificationService } from '../../../../shared/feedback/notification.service';
import { GruposService } from '../../grupos/data/grupos.service';
import { EventosService } from '../../eventos/data/eventos.service';
import { FotosService } from '../data/fotos.service';
import { EstadoProcesamientoFoto, Foto } from '../domain/foto.model';
import { GaleriaComponent } from './galeria.component';

function archivo(nombre: string, tamano = 1000): File {
  return new File([new Uint8Array(tamano)], nombre, { type: 'image/jpeg' });
}

function foto(parcial: Partial<Foto>): Foto {
  return {
    id: 1,
    eventoId: 7,
    grupoId: 3,
    participanteId: null,
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
  let subir: Mock;
  let borrar: Mock;
  let descargarOriginal: Mock;
  let confirm: Mock;
  let dialogOpen: Mock;
  let notify: { success: Mock; error: Mock };

  const fotos = [
    foto({ id: 1, participanteId: null, nombreArchivoOriginal: 'grupal.jpg' }),
    foto({ id: 2, participanteId: 21, nombreArchivoOriginal: 'ana.jpg' }),
    foto({
      id: 3,
      participanteId: 21,
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
    subir = vi
      .fn()
      .mockName('fotos.subir')
      .mockImplementation((_grupoId: number, _participanteId: number | null, archivos: File[]) =>
        of(archivos.map((a, i) => foto({ id: 100 + i, nombreArchivoOriginal: a.name }))),
      );
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
          provide: GruposService,
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
                  participantes: [{ id: 21, nombre: 'Ana Pérez' }],
                }),
              ),
            },
          },
        },
        {
          provide: FotosService,
          useValue: {
            listar,
            subir,
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

  function seleccionarGrupo(): void {
    fixture.componentInstance['eventoId'].set(7);
    fixture.detectChanges();
    fixture.componentInstance['grupoId'].set(3);
    fixture.detectChanges();
    fixture.detectChanges(); // ciclo extra: resources disparados por señal seteada directo del test
  }

  it('sin grupo elegido no pide fotos', async () => {
    await setup();
    expect(listar).not.toHaveBeenCalled();
  });

  it('elegir grupo pide sus fotos; el filtro por participante se aplica en memoria', async () => {
    await setup();
    seleccionarGrupo();

    expect(listar).toHaveBeenCalledWith(3);
    const c = fixture.componentInstance;
    expect(c['fotos']().length).toBe(3); // Todas

    c['participanteFiltro'].set(0); // Solo grupales
    expect(c['fotos']().map((f) => f.id)).toEqual([1]);

    c['participanteFiltro'].set(21); // Participante de Ana
    expect(c['fotos']().map((f) => f.id)).toEqual([2, 3]);
    expect(c['nombreParticipante'](c['fotos']()[0])).toBe('Ana Pérez');
  });

  it('borrar con confirmación llama al servicio, notifica y recarga', async () => {
    await setup();
    seleccionarGrupo();
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
    seleccionarGrupo();
    confirm.mockReturnValue(of(false));

    fixture.componentInstance['borrar'](fotos[0]);

    expect(borrar).not.toHaveBeenCalled();
  });

  it('el preview solo se abre para fotos Lista', async () => {
    await setup();
    seleccionarGrupo();

    fixture.componentInstance['verPreview'](fotos[2]); // Error
    expect(dialogOpen).not.toHaveBeenCalled();

    fixture.componentInstance['verPreview'](fotos[0]); // Lista
    expect(dialogOpen).toHaveBeenCalledTimes(1);
  });

  it('con "Todas las fotos" no hay destino: no se puede subir', async () => {
    await setup();
    seleccionarGrupo();

    fixture.componentInstance['archivos'].set([archivo('a.jpg')]);
    expect(fixture.componentInstance['destinoElegido']()).toBe(false);
    expect(fixture.componentInstance['puedeSubir']()).toBe(false);

    fixture.componentInstance['subir']();
    expect(subir).not.toHaveBeenCalled();
  });

  it('subir a grupales manda participanteId null, notifica, limpia y recarga', async () => {
    await setup();
    seleccionarGrupo();
    fixture.componentInstance['participanteFiltro'].set(0); // Grupales del grupo
    const llamadasPrevias = listar.mock.calls.length;

    fixture.componentInstance['archivos'].set([archivo('a.jpg'), archivo('b.jpg')]);
    fixture.componentInstance['subir']();
    fixture.detectChanges(); // el reload() del resource corre en el próximo flush de efectos

    expect(subir).toHaveBeenCalledTimes(1);
    const [grupoId, participanteId, archivos] = subir.mock.calls[0] as [number, number | null, File[]];
    expect(grupoId).toBe(3);
    expect(participanteId).toBeNull();
    expect(archivos.map((a) => a.name)).toEqual(['a.jpg', 'b.jpg']);
    expect(notify.success).toHaveBeenCalledWith(expect.stringContaining('2 foto(s) subidas'));
    expect(fixture.componentInstance['archivos']()).toEqual([]);
    expect(listar.mock.calls.length).toBeGreaterThan(llamadasPrevias); // reload
  });

  it('con un participante elegido, su id viaja en la subida; más de 10 archivos van en tandas', async () => {
    await setup();
    seleccionarGrupo();
    fixture.componentInstance['participanteFiltro'].set(21); // Participante de Ana

    const muchos = Array.from({ length: 23 }, (_, i) => archivo(`f${i}.jpg`));
    fixture.componentInstance['archivos'].set(muchos);
    fixture.componentInstance['subir']();

    expect(subir).toHaveBeenCalledTimes(3); // 10 + 10 + 3
    expect(subir.mock.calls.map((c) => (c[2] as File[]).length)).toEqual([10, 10, 3]);
    expect(subir.mock.calls.every((c) => c[1] === 21)).toBe(true);
  });

  it('los archivos repetidos (mismo nombre y tamaño) no se duplican en la selección', async () => {
    await setup();
    seleccionarGrupo();

    fixture.componentInstance['archivos'].set([archivo('a.jpg')]);
    // jsdom no permite setear input.files: se simula el shape mínimo que lee el handler.
    const evento = { target: { files: [archivo('a.jpg'), archivo('b.jpg')], value: '' } };
    fixture.componentInstance['agregarArchivos'](evento as unknown as Event);

    expect(fixture.componentInstance['archivos']().map((f) => f.name)).toEqual(['a.jpg', 'b.jpg']);
  });

  it('descargar original delega en el servicio', async () => {
    await setup();
    seleccionarGrupo();

    // el stub de object URLs del setup cubre el saveBlobResponse
    fixture.componentInstance['descargarOriginal'](fotos[0]);
    expect(descargarOriginal).toHaveBeenCalledWith(1);
  });
});
