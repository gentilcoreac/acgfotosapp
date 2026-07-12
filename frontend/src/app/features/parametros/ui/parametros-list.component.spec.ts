import type { Mock } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MatDialog } from '@angular/material/dialog';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { of } from 'rxjs';
import { NotificationService } from '../../../shared/feedback/notification.service';
import { ParametrosService } from '../data/parametros.service';
import { Parametro } from '../domain/parametro.model';
import { ParametrosListComponent } from './parametros-list.component';

describe('ParametrosListComponent', () => {
  let fixture: ComponentFixture<ParametrosListComponent>;
  let getAllByCriteria: Mock;
  let deleteFn: Mock;
  let getAplicaciones: Mock;
  let notify: { success: Mock; error: Mock };

  beforeEach(async () => {
    getAllByCriteria = vi
      .fn()
      .mockName('getAllByCriteria')
      .mockReturnValue(of({ items: [{ id: 1, nombre: 'MaxIntentos' }], totalCount: 1 }));
    deleteFn = vi.fn().mockName('delete').mockReturnValue(of(undefined));
    getAplicaciones = vi
      .fn()
      .mockName('getAplicaciones')
      .mockReturnValue(of([{ id: 1, nombre: 'App 1' }]));
    notify = { success: vi.fn(), error: vi.fn() };

    await TestBed.configureTestingModule({
      imports: [ParametrosListComponent],
      providers: [
        provideNoopAnimations(),
        { provide: MatDialog, useValue: { open: () => ({ afterClosed: () => of(null) }) } },
        { provide: NotificationService, useValue: notify },
        {
          provide: ParametrosService,
          useValue: { crud: { getAllByCriteria, delete: deleteFn }, getAplicaciones },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(ParametrosListComponent);
    fixture.detectChanges();
  });

  it('se crea y la tabla pide datos al iniciar', () => {
    expect(fixture.componentInstance).toBeTruthy();
    expect(getAllByCriteria).toHaveBeenCalled();
  });

  it('cambiar el filtro de aplicación actualiza `filters` sin recargar dos veces', () => {
    const instance = fixture.componentInstance;
    const callsBefore = getAllByCriteria.mock.calls.length;

    instance['aplicacionFilter'].setValue(1);
    fixture.detectChanges();

    expect(instance['filters']()).toEqual({ aplicacionId: 1 });
    expect(getAllByCriteria.mock.calls.length).toBe(callsBefore + 1);
  });

  it('confirmDelete arma el mensaje con el nombre del parámetro', () => {
    const row = { id: 1, nombre: 'MaxIntentos' } as Parametro;
    expect(fixture.componentInstance['confirmDelete'](row)).toEqual({
      title: 'Eliminar',
      message: '¿Eliminar el parámetro "MaxIntentos"?',
    });
  });

  it('el borrado invoca delete y notifica éxito', () => {
    const row = { id: 1, nombre: 'MaxIntentos' } as Parametro;
    fixture.componentInstance['removeFn'](row)().subscribe();
    expect(deleteFn).toHaveBeenCalledWith(1);
    expect(notify.success).toHaveBeenCalledWith('Parámetro eliminado.');
  });
});
