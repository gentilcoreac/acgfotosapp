import type { Mock } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { of } from 'rxjs';
import { NotificationService } from '../../../../shared/feedback/notification.service';
import { CursosService } from '../../cursos/data/cursos.service';
import { EventosService } from '../../eventos/data/eventos.service';
import { FotosService } from '../data/fotos.service';
import { EstadoProcesamientoFoto, Foto } from '../domain/foto.model';
import { SubirFotosComponent } from './subir-fotos.component';

function archivo(nombre: string, tamano = 1000): File {
  return new File([new Uint8Array(tamano)], nombre, { type: 'image/jpeg' });
}

function foto(parcial: Partial<Foto>): Foto {
  return {
    id: 1,
    eventoId: 7,
    cursoId: 3,
    albumId: null,
    nombreArchivoOriginal: 'a.jpg',
    ancho: 0,
    alto: 0,
    tamanoBytes: 1000,
    estadoProcesamiento: EstadoProcesamientoFoto.Pendiente,
    errorProcesamiento: null,
    creadoEn: '2026-07-13T00:00:00Z',
    ...parcial,
  };
}

describe('SubirFotosComponent', () => {
  let fixture: ComponentFixture<SubirFotosComponent>;
  let getAllEventos: Mock;
  let getAllCursos: Mock;
  let getCursoById: Mock;
  let listar: Mock;
  let subir: Mock;
  let notify: { success: Mock; error: Mock };

  async function setup(fotos: Foto[] = []): Promise<void> {
    getAllEventos = vi
      .fn()
      .mockName('eventos.getAll')
      .mockReturnValue(of({ items: [{ id: 7, nombre: 'Graduación 2026', estado: 1 }], totalCount: 1 }));
    getAllCursos = vi
      .fn()
      .mockName('cursos.getAllByCriteria')
      .mockReturnValue(of({ items: [{ id: 3, eventoId: 7, nombre: '7ºA' }], totalCount: 1 }));
    getCursoById = vi
      .fn()
      .mockName('cursos.getById')
      .mockReturnValue(
        of({
          id: 3,
          eventoId: 7,
          nombre: '7ºA',
          albumes: [{ id: 21, nombreAlumno: 'Ana Pérez', codigoAcceso: 'AAAA-1111' }],
        }),
      );
    listar = vi.fn().mockName('fotos.listar').mockReturnValue(of(fotos));
    subir = vi
      .fn()
      .mockName('fotos.subir')
      .mockImplementation((_cursoId: number, _albumId: number | null, archivos: File[]) =>
        of(archivos.map((a, i) => foto({ id: i + 1, nombreArchivoOriginal: a.name }))),
      );
    notify = { success: vi.fn(), error: vi.fn() };

    await TestBed.configureTestingModule({
      imports: [SubirFotosComponent],
      providers: [
        provideNoopAnimations(),
        { provide: NotificationService, useValue: notify },
        { provide: EventosService, useValue: { crud: { getAll: getAllEventos } } },
        {
          provide: CursosService,
          useValue: { crud: { getAllByCriteria: getAllCursos, getById: getCursoById } },
        },
        { provide: FotosService, useValue: { listar, subir } },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(SubirFotosComponent);
    fixture.detectChanges();
  }

  /** Deja seleccionados evento 7 y curso 3 (los resources dependientes resuelven). */
  function seleccionarCurso(): void {
    fixture.componentInstance['eventoId'].set(7);
    fixture.detectChanges();
    fixture.componentInstance['cursoId'].set(3);
    fixture.detectChanges();
    fixture.detectChanges(); // ciclo extra: resources disparados por señal seteada directo del test
  }

  it('se crea y pide el lookup de eventos una sola vez', async () => {
    await setup();
    expect(fixture.componentInstance).toBeTruthy();
    expect(getAllEventos).toHaveBeenCalledTimes(1);
    // Sin evento/curso elegidos no se piden cursos ni fotos (params undefined = no fetch).
    expect(getAllCursos).not.toHaveBeenCalled();
    expect(listar).not.toHaveBeenCalled();
  });

  it('elegir evento pide sus cursos; elegir curso pide detalle (álbumes) y fotos', async () => {
    await setup();
    seleccionarCurso();

    expect(getAllCursos).toHaveBeenCalledWith(expect.objectContaining({ eventoId: 7 }));
    expect(getCursoById).toHaveBeenCalledWith(3);
    expect(listar).toHaveBeenCalledWith(3);

    // El destino ofrece grupales + los álbumes del curso.
    const opciones = fixture.componentInstance['destinoOptions']();
    expect(opciones.map((o) => o.label)).toEqual([
      'Fotos grupales del curso',
      'Álbum: Ana Pérez',
    ]);
  });

  it('cambiar el evento resetea el curso elegido', async () => {
    await setup();
    seleccionarCurso();

    fixture.componentInstance['eventoId'].set(99);
    fixture.detectChanges();

    expect(fixture.componentInstance['cursoId']()).toBeNull();
  });

  it('subir manda una tanda con curso y sin álbum (grupales) y notifica', async () => {
    await setup();
    seleccionarCurso();

    fixture.componentInstance['archivos'].set([archivo('a.jpg'), archivo('b.jpg')]);
    fixture.componentInstance['subir']();

    expect(subir).toHaveBeenCalledTimes(1);
    const [cursoId, albumId, archivos] = subir.mock.calls[0] as [number, number | null, File[]];
    expect(cursoId).toBe(3);
    expect(albumId).toBeNull();
    expect(archivos.map((a) => a.name)).toEqual(['a.jpg', 'b.jpg']);
    expect(notify.success).toHaveBeenCalledWith(expect.stringContaining('2 foto(s) subidas'));
    // Tras subir, la selección se limpia y se recarga el listado.
    expect(fixture.componentInstance['archivos']()).toEqual([]);
  });

  it('con destino álbum, el albumId viaja en la subida', async () => {
    await setup();
    seleccionarCurso();

    fixture.componentInstance['destino'].set(21);
    fixture.componentInstance['archivos'].set([archivo('ana.jpg')]);
    fixture.componentInstance['subir']();

    const [, albumId] = subir.mock.calls[0] as [number, number | null, File[]];
    expect(albumId).toBe(21);
  });

  it('más de 10 archivos se suben en tandas secuenciales', async () => {
    await setup();
    seleccionarCurso();

    const muchos = Array.from({ length: 23 }, (_, i) => archivo(`f${i}.jpg`));
    fixture.componentInstance['archivos'].set(muchos);
    fixture.componentInstance['subir']();

    expect(subir).toHaveBeenCalledTimes(3); // 10 + 10 + 3
    const tamanos = subir.mock.calls.map((c) => (c[2] as File[]).length);
    expect(tamanos).toEqual([10, 10, 3]);
    expect(notify.success).toHaveBeenCalledWith(expect.stringContaining('23 foto(s) subidas'));
  });

  it('los archivos repetidos (mismo nombre y tamaño) no se duplican en la selección', async () => {
    await setup();
    seleccionarCurso();

    fixture.componentInstance['archivos'].set([archivo('a.jpg')]);
    // jsdom no permite setear input.files: se simula el shape mínimo que lee el handler.
    const evento = { target: { files: [archivo('a.jpg'), archivo('b.jpg')], value: '' } };
    fixture.componentInstance['agregarArchivos'](evento as unknown as Event);

    expect(fixture.componentInstance['archivos']().map((f) => f.name)).toEqual(['a.jpg', 'b.jpg']);
  });

  it('el estado de cada foto se presenta con su etiqueta', async () => {
    await setup([
      foto({ id: 1, estadoProcesamiento: EstadoProcesamientoFoto.Pendiente }),
      foto({ id: 2, estadoProcesamiento: EstadoProcesamientoFoto.Lista }),
      foto({ id: 3, estadoProcesamiento: EstadoProcesamientoFoto.Error, errorProcesamiento: 'x' }),
    ]);
    seleccionarCurso();

    const c = fixture.componentInstance;
    expect(c['estadoLabel'](c['fotos']()[0])).toBe('Procesando…');
    expect(c['estadoLabel'](c['fotos']()[1])).toBe('Lista');
    expect(c['estadoLabel'](c['fotos']()[2])).toBe('Error');
    expect(c['cantidadPendientes']()).toBe(1);
  });
});
