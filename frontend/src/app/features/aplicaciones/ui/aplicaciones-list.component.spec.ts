import type { Mock } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MatDialog } from '@angular/material/dialog';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { of } from 'rxjs';
import { NotificationService } from '../../../shared/feedback/notification.service';
import { AplicacionesService } from '../data/aplicaciones.service';
import { Aplicacion } from '../domain/aplicacion.model';
import { AplicacionesListComponent } from './aplicaciones-list.component';

describe('AplicacionesListComponent', () => {
  let fixture: ComponentFixture<AplicacionesListComponent>;
  let getAllByCriteria: Mock;
  let deleteFn: Mock;
  let notify: { success: Mock; error: Mock };

  beforeEach(async () => {
    getAllByCriteria = vi
      .fn()
      .mockName('getAllByCriteria')
      .mockReturnValue(of({ items: [{ id: 1, nombre: 'Budgeting' }], totalCount: 1 }));
    deleteFn = vi.fn().mockName('delete').mockReturnValue(of(undefined));
    notify = { success: vi.fn(), error: vi.fn() };

    await TestBed.configureTestingModule({
      imports: [AplicacionesListComponent],
      providers: [
        provideNoopAnimations(),
        { provide: MatDialog, useValue: { open: () => ({ afterClosed: () => of(null) }) } },
        { provide: NotificationService, useValue: notify },
        {
          provide: AplicacionesService,
          useValue: { crud: { getAllByCriteria, delete: deleteFn } },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(AplicacionesListComponent);
    fixture.detectChanges();
  });

  it('se crea y la tabla pide datos al iniciar', () => {
    expect(fixture.componentInstance).toBeTruthy();
    expect(getAllByCriteria).toHaveBeenCalled();
  });

  it('confirmDelete arma el mensaje con el nombre de la aplicación', () => {
    const row = { id: 1, nombre: 'Budgeting' } as Aplicacion;
    expect(fixture.componentInstance['confirmDelete'](row)).toEqual({
      title: 'Eliminar',
      message: '¿Eliminar la aplicación "Budgeting"?',
    });
  });

  it('el borrado invoca delete y notifica éxito', () => {
    const row = { id: 1, nombre: 'Budgeting' } as Aplicacion;
    fixture.componentInstance['removeFn'](row)().subscribe();
    expect(deleteFn).toHaveBeenCalledWith(1);
    expect(notify.success).toHaveBeenCalledWith('Aplicación eliminada.');
  });
});
