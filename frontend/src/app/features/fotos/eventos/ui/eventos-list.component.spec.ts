import type { Mock } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MatDialog } from '@angular/material/dialog';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { of } from 'rxjs';
import { NotificationService } from '../../../../shared/feedback/notification.service';
import { EventosService } from '../data/eventos.service';
import { Evento } from '../domain/evento.model';
import { EventosListComponent } from './eventos-list.component';

describe('EventosListComponent', () => {
  let fixture: ComponentFixture<EventosListComponent>;
  let getAllByCriteria: Mock;
  let deleteFn: Mock;
  let notify: { success: Mock; error: Mock };

  async function setup(): Promise<void> {
    getAllByCriteria = vi
      .fn()
      .mockName('getAllByCriteria')
      .mockReturnValue(
        of({
          items: [
            {
              id: 1,
              nombre: 'Graduación 2026',
              colegio: 'San Martín',
              fecha: '2026-11-20T00:00:00',
              estado: 1,
            },
          ],
          totalCount: 1,
        }),
      );
    deleteFn = vi.fn().mockName('delete').mockReturnValue(of(undefined));
    notify = { success: vi.fn(), error: vi.fn() };

    await TestBed.configureTestingModule({
      imports: [EventosListComponent],
      providers: [
        provideNoopAnimations(),
        { provide: MatDialog, useValue: { open: () => ({ afterClosed: () => of(null) }) } },
        { provide: NotificationService, useValue: notify },
        {
          provide: EventosService,
          useValue: { crud: { getAllByCriteria, delete: deleteFn } },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(EventosListComponent);
    fixture.detectChanges();
  }

  it('se crea y la tabla pide datos al iniciar', async () => {
    await setup();
    expect(fixture.componentInstance).toBeTruthy();
    expect(getAllByCriteria).toHaveBeenCalled();
  });

  it('muestra el estado como chip y la fecha localizada', async () => {
    await setup();
    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('Publicado');
    expect(text).toContain(new Date(2026, 10, 20).toLocaleDateString());
  });

  it('el borrado invoca delete y notifica éxito', async () => {
    await setup();
    const row = { id: 1, nombre: 'Graduación 2026', estado: 0 } as Evento;
    fixture.componentInstance['removeFn'](row)().subscribe();

    expect(deleteFn).toHaveBeenCalledWith(1);
    expect(notify.success).toHaveBeenCalledWith('Evento eliminado.');
  });
});
