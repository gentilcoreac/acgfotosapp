import type { Mock } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MatDialog } from '@angular/material/dialog';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { of } from 'rxjs';
import { NotificationService } from '../../../shared/feedback/notification.service';
import { PermisosService } from '../data/permisos.service';
import { Permiso } from '../domain/permiso.model';
import { PermisosListComponent } from './permisos-list.component';

describe('PermisosListComponent', () => {
  let fixture: ComponentFixture<PermisosListComponent>;
  let getAllByCriteria: Mock;
  let deleteFn: Mock;
  let notify: { success: Mock; error: Mock };

  beforeEach(async () => {
    getAllByCriteria = vi
      .fn()
      .mockName('getAllByCriteria')
      .mockReturnValue(of({ items: [{ id: 1, nombre: 'Ver reportes' }], totalCount: 1 }));
    deleteFn = vi.fn().mockName('delete').mockReturnValue(of(undefined));
    notify = { success: vi.fn(), error: vi.fn() };

    await TestBed.configureTestingModule({
      imports: [PermisosListComponent],
      providers: [
        provideNoopAnimations(),
        { provide: MatDialog, useValue: { open: () => ({ afterClosed: () => of(null) }) } },
        { provide: NotificationService, useValue: notify },
        { provide: PermisosService, useValue: { crud: { getAllByCriteria, delete: deleteFn } } },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(PermisosListComponent);
    fixture.detectChanges();
  });

  it('se crea y la tabla pide datos al iniciar', () => {
    expect(fixture.componentInstance).toBeTruthy();
    expect(getAllByCriteria).toHaveBeenCalled();
  });

  it('confirmDelete arma el mensaje con el nombre del permiso', () => {
    const row = { id: 1, nombre: 'Ver reportes' } as Permiso;
    expect(fixture.componentInstance['confirmDelete'](row)).toEqual({
      title: 'Eliminar',
      message: '¿Eliminar el permiso "Ver reportes"?',
    });
  });

  it('el borrado invoca delete y notifica éxito', () => {
    const row = { id: 1, nombre: 'Ver reportes' } as Permiso;
    fixture.componentInstance['removeFn'](row)().subscribe();
    expect(deleteFn).toHaveBeenCalledWith(1);
    expect(notify.success).toHaveBeenCalledWith('Permiso eliminado.');
  });
});
