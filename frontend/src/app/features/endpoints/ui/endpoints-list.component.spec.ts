import type { Mock } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MatDialog } from '@angular/material/dialog';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { of, throwError } from 'rxjs';
import { NotificationService } from '../../../shared/feedback/notification.service';
import { EndpointsService } from '../data/endpoints.service';
import { Endpoint } from '../domain/endpoint.model';
import { EndpointsListComponent } from './endpoints-list.component';

describe('EndpointsListComponent', () => {
  let fixture: ComponentFixture<EndpointsListComponent>;
  let getAllByCriteria: Mock;
  let deleteFn: Mock;
  let descubrir: Mock;
  let notify: { success: Mock; error: Mock };

  async function setup(): Promise<void> {
    getAllByCriteria = vi
      .fn()
      .mockName('getAllByCriteria')
      .mockReturnValue(
        of({ items: [{ id: 1, httpMethod: 'GET', route: 'api/x' }], totalCount: 1 }),
      );
    deleteFn = vi.fn().mockName('delete').mockReturnValue(of(undefined));
    descubrir = vi
      .fn()
      .mockName('descubrir')
      .mockReturnValue(of([{ id: 1 }, { id: 2 }]));
    notify = { success: vi.fn(), error: vi.fn() };

    await TestBed.configureTestingModule({
      imports: [EndpointsListComponent],
      providers: [
        provideNoopAnimations(),
        { provide: MatDialog, useValue: { open: () => ({ afterClosed: () => of(null) }) } },
        { provide: NotificationService, useValue: notify },
        {
          provide: EndpointsService,
          useValue: { crud: { getAllByCriteria, delete: deleteFn }, descubrir },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(EndpointsListComponent);
    fixture.detectChanges();
  }

  it('se crea y la tabla pide datos al iniciar', async () => {
    await setup();
    expect(fixture.componentInstance).toBeTruthy();
    expect(getAllByCriteria).toHaveBeenCalled();
  });

  it('descubrir(): muestra spinner, notifica la cantidad y recarga la tabla', async () => {
    await setup();
    const instance = fixture.componentInstance;
    expect(instance['discovering']()).toBe(false);

    instance['descubrir']();
    fixture.detectChanges();

    expect(descubrir).toHaveBeenCalled();
    expect(notify.success).toHaveBeenCalledWith('2 endpoints registrados.');
    expect(instance['discovering']()).toBe(false);
  });

  it('descubrir(): si falla, apaga el spinner sin notificar éxito', async () => {
    await setup();
    descubrir.mockReturnValue(throwError(() => new Error('boom')));
    const instance = fixture.componentInstance;

    instance['descubrir']();
    fixture.detectChanges();

    expect(instance['discovering']()).toBe(false);
    expect(notify.success).not.toHaveBeenCalled();
  });

  it('el borrado invoca delete y notifica éxito', async () => {
    await setup();
    const row = { id: 1, httpMethod: 'GET', route: 'api/x' } as Endpoint;
    fixture.componentInstance['removeFn'](row)().subscribe();
    expect(deleteFn).toHaveBeenCalledWith(1);
    expect(notify.success).toHaveBeenCalledWith('Endpoint eliminado.');
  });
});
