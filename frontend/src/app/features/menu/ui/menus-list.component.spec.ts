import type { Mock } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MatDialog } from '@angular/material/dialog';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { of } from 'rxjs';
import { NotificationService } from '../../../shared/feedback/notification.service';
import { MenusService } from '../data/menus.service';
import { Menu } from '../domain/menu.model';
import { MenusListComponent } from './menus-list.component';

describe('MenusListComponent', () => {
  let fixture: ComponentFixture<MenusListComponent>;
  let getAllByCriteria: Mock;
  let deleteFn: Mock;
  let getAplicaciones: Mock;
  let notify: { success: Mock; error: Mock };

  beforeEach(async () => {
    getAllByCriteria = vi
      .fn()
      .mockName('getAllByCriteria')
      .mockReturnValue(of({ items: [{ id: 1, nombre: 'Home', codigo: 'HOME' }], totalCount: 1 }));
    deleteFn = vi.fn().mockName('delete').mockReturnValue(of(undefined));
    getAplicaciones = vi
      .fn()
      .mockName('getAplicaciones')
      .mockReturnValue(of([{ id: 1, nombre: 'App 1' }]));
    notify = { success: vi.fn(), error: vi.fn() };

    await TestBed.configureTestingModule({
      imports: [MenusListComponent],
      providers: [
        provideNoopAnimations(),
        { provide: MatDialog, useValue: { open: () => ({ afterClosed: () => of(null) }) } },
        { provide: NotificationService, useValue: notify },
        {
          provide: MenusService,
          useValue: { crud: { getAllByCriteria, delete: deleteFn }, getAplicaciones },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(MenusListComponent);
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
    // Un solo fetch adicional (el `effect` interno de `tbi-table` reacciona al input `filters`).
    expect(getAllByCriteria.mock.calls.length).toBe(callsBefore + 1);
  });

  it('confirmDelete arma el mensaje con el nombre del menú', () => {
    const row = { id: 1, nombre: 'Home' } as Menu;
    expect(fixture.componentInstance['confirmDelete'](row)).toEqual({
      title: 'Eliminar',
      message: '¿Eliminar el menú "Home"?',
    });
  });

  it('el borrado invoca delete y notifica éxito', () => {
    const row = { id: 1, nombre: 'Home' } as Menu;
    fixture.componentInstance['removeFn'](row)().subscribe();
    expect(deleteFn).toHaveBeenCalledWith(1);
    expect(notify.success).toHaveBeenCalledWith('Menú eliminado.');
  });
});
