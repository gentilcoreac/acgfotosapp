import type { Mock } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MatDialog } from '@angular/material/dialog';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { of } from 'rxjs';
import { QueryParams } from '../../../../core/models/query-params.model';
import { NotificationService } from '../../../../shared/feedback/notification.service';
import { EventosService } from '../../eventos/data/eventos.service';
import { CursosService } from '../data/cursos.service';
import { Curso } from '../domain/curso.model';
import { CursosListComponent } from './cursos-list.component';

describe('CursosListComponent', () => {
  let fixture: ComponentFixture<CursosListComponent>;
  let getAllByCriteria: Mock;
  let deleteFn: Mock;
  let getAllEventos: Mock;
  let notify: { success: Mock; error: Mock };

  async function setup(): Promise<void> {
    getAllByCriteria = vi
      .fn()
      .mockName('getAllByCriteria')
      .mockReturnValue(
        of({
          items: [{ id: 1, eventoId: 7, nombre: '6to A', cantidadAlbumes: 25 }],
          totalCount: 1,
        }),
      );
    deleteFn = vi.fn().mockName('delete').mockReturnValue(of(undefined));
    getAllEventos = vi
      .fn()
      .mockName('eventos.getAll')
      .mockReturnValue(of({ items: [{ id: 7, nombre: 'Graduación 2026', estado: 1 }], totalCount: 1 }));
    notify = { success: vi.fn(), error: vi.fn() };

    await TestBed.configureTestingModule({
      imports: [CursosListComponent],
      providers: [
        provideNoopAnimations(),
        { provide: MatDialog, useValue: { open: () => ({ afterClosed: () => of(null) }) } },
        { provide: NotificationService, useValue: notify },
        { provide: CursosService, useValue: { crud: { getAllByCriteria, delete: deleteFn } } },
        { provide: EventosService, useValue: { crud: { getAll: getAllEventos } } },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(CursosListComponent);
    fixture.detectChanges();
  }

  it('se crea, la tabla pide datos y el lookup de eventos se pide una vez', async () => {
    await setup();
    expect(fixture.componentInstance).toBeTruthy();
    expect(getAllByCriteria).toHaveBeenCalled();
    expect(getAllEventos).toHaveBeenCalledTimes(1);
  });

  it('resuelve la columna Evento con el lookup', async () => {
    await setup();
    // El resource del lookup resuelve async: un ciclo extra para que la columna lo tome.
    fixture.detectChanges();
    const row = { id: 1, eventoId: 7, nombre: '6to A' } as Curso;
    const columnaEvento = fixture.componentInstance['columns']().find((c) => c.key === 'evento');
    expect(columnaEvento?.cell?.(row)).toBe('Graduación 2026');
  });

  it('el filtro por evento viaja en la query como eventoId', async () => {
    await setup();
    fixture.componentInstance['eventoFiltro'].set(7);
    fixture.detectChanges();

    const lastQuery = vi.mocked(getAllByCriteria).mock.lastCall![0] as QueryParams;
    expect(lastQuery['eventoId']).toBe(7);
  });

  it('el borrado invoca delete y notifica éxito', async () => {
    await setup();
    const row = { id: 1, eventoId: 7, nombre: '6to A' } as Curso;
    fixture.componentInstance['removeFn'](row)().subscribe();

    expect(deleteFn).toHaveBeenCalledWith(1);
    expect(notify.success).toHaveBeenCalledWith('Curso eliminado.');
  });
});
