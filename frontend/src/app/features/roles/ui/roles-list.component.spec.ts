import type { Mock } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MatDialog } from '@angular/material/dialog';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { of } from 'rxjs';
import { NotificationService } from '../../../shared/feedback/notification.service';
import { RolesService } from '../data/roles.service';
import { RolesListComponent } from './roles-list.component';

describe('RolesListComponent', () => {
  let fixture: ComponentFixture<RolesListComponent>;
  let getAllByCriteria: Mock;
  let deleteFn: Mock;
  let notify: { success: Mock; error: Mock };

  beforeEach(async () => {
    getAllByCriteria = vi
      .fn()
      .mockName('getAllByCriteria')
      .mockReturnValue(of({ items: [{ id: 1, descripcion: 'Admin' }], totalCount: 1 }));
    deleteFn = vi.fn().mockName('delete').mockReturnValue(of(undefined));
    notify = { success: vi.fn(), error: vi.fn() };

    await TestBed.configureTestingModule({
      imports: [RolesListComponent],
      providers: [
        provideNoopAnimations(),
        { provide: MatDialog, useValue: { open: () => ({ afterClosed: () => of(null) }) } },
        { provide: NotificationService, useValue: notify },
        { provide: RolesService, useValue: { crud: { getAllByCriteria, delete: deleteFn } } },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(RolesListComponent);
    fixture.detectChanges();
  });

  it('se crea y la tabla pide datos al iniciar', () => {
    expect(fixture.componentInstance).toBeTruthy();
    expect(getAllByCriteria).toHaveBeenCalled();
  });

  it('confirmDelete arma el mensaje con la descripción del rol', () => {
    const row = { id: 1, descripcion: 'Admin' };
    expect(fixture.componentInstance['confirmDelete'](row)).toEqual({
      title: 'Eliminar',
      message: '¿Eliminar el rol "Admin"?',
    });
  });

  it('el borrado invoca delete y notifica éxito', () => {
    const row = { id: 1, descripcion: 'Admin' };
    fixture.componentInstance['removeFn'](row)().subscribe();
    expect(deleteFn).toHaveBeenCalledWith(1);
    expect(notify.success).toHaveBeenCalledWith('Rol eliminado.');
  });
});
