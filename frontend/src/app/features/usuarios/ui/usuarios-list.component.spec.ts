import type { Mock } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MatDialog } from '@angular/material/dialog';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { of } from 'rxjs';
import { AuthStore } from '../../../core/auth';
import { NotificationService } from '../../../shared/feedback/notification.service';
import { LicenciasService } from '../../licencias/data/licencias.service';
import { UsuariosService } from '../data/usuarios.service';
import { Usuario } from '../domain/usuario.model';
import { UsuariosListComponent } from './usuarios-list.component';

describe('UsuariosListComponent', () => {
  let fixture: ComponentFixture<UsuariosListComponent>;
  let getAllByCriteria: Mock;
  let deleteFn: Mock;
  let bloquearDesbloquear: Mock;
  let getResumen: Mock;
  let notify: { success: Mock; error: Mock };

  async function setup(isRoot = false): Promise<void> {
    getAllByCriteria = vi
      .fn()
      .mockName('getAllByCriteria')
      .mockReturnValue(
        of({
          items: [
            { id: 1, userName: 'jperez', nombre: 'Juan', apellido: 'Pérez', bloqueado: true },
          ],
          totalCount: 1,
        }),
      );
    deleteFn = vi.fn().mockName('delete').mockReturnValue(of(undefined));
    bloquearDesbloquear = vi.fn().mockName('bloquearDesbloquear').mockReturnValue(of(undefined));
    getResumen = vi.fn().mockName('getResumen').mockReturnValue(of([]));
    notify = { success: vi.fn(), error: vi.fn() };

    await TestBed.configureTestingModule({
      imports: [UsuariosListComponent],
      providers: [
        provideNoopAnimations(),
        { provide: MatDialog, useValue: { open: () => ({ afterClosed: () => of(null) }) } },
        { provide: NotificationService, useValue: notify },
        { provide: AuthStore, useValue: { isRoot: () => isRoot } },
        { provide: LicenciasService, useValue: { getResumen } },
        {
          provide: UsuariosService,
          useValue: { crud: { getAllByCriteria, delete: deleteFn }, bloquearDesbloquear },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(UsuariosListComponent);
    fixture.detectChanges();
  }

  it('se crea y la tabla pide datos al iniciar', async () => {
    await setup();
    expect(fixture.componentInstance).toBeTruthy();
    expect(getAllByCriteria).toHaveBeenCalled();
  });

  it('no root: pide el resumen de licencias y agrega la columna Licencia', async () => {
    await setup(false);
    expect(getResumen).toHaveBeenCalled();
    expect(fixture.componentInstance['columns']().some((c) => c.key === 'licencia')).toBe(true);
  });

  it('root: no pide el resumen de licencias ni agrega la columna Licencia', async () => {
    await setup(true);
    expect(getResumen).not.toHaveBeenCalled();
    expect(fixture.componentInstance['columns']().some((c) => c.key === 'licencia')).toBe(false);
  });

  it('desbloquear invoca el endpoint dedicado y notifica éxito', async () => {
    await setup();
    const row = { id: 1, userName: 'jperez', bloqueado: true } as Usuario;
    fixture.componentInstance['unlockFn'](row).subscribe();

    expect(bloquearDesbloquear).toHaveBeenCalledWith('jperez', false);
    expect(notify.success).toHaveBeenCalledWith('Usuario desbloqueado.');
  });

  it('el borrado invoca delete y notifica éxito', async () => {
    await setup();
    const row = { id: 1, userName: 'jperez' } as Usuario;
    fixture.componentInstance['removeFn'](row)().subscribe();

    expect(deleteFn).toHaveBeenCalledWith(1);
    expect(notify.success).toHaveBeenCalledWith('Usuario eliminado.');
  });
});
