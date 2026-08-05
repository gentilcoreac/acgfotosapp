import type { Mock } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MatDialog } from '@angular/material/dialog';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { of } from 'rxjs';
import { NotificationService } from '../../../../shared/feedback/notification.service';
import { OpcionesPublicacionService } from '../data/opciones-publicacion.service';
import { OpcionesPublicacion } from '../domain/opciones-publicacion.model';
import { PublicacionComponent } from './publicacion.component';

describe('PublicacionComponent', () => {
  let fixture: ComponentFixture<PublicacionComponent>;
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
              nombre: 'Estándar',
              esDefault: true,
              ladoMayorPreview: 1600,
              ladoMayorThumb: 600,
              calidad: 80,
            },
          ],
          totalCount: 1,
        }),
      );
    deleteFn = vi.fn().mockName('delete').mockReturnValue(of(undefined));
    notify = { success: vi.fn(), error: vi.fn() };

    await TestBed.configureTestingModule({
      imports: [PublicacionComponent],
      providers: [
        provideNoopAnimations(),
        { provide: MatDialog, useValue: { open: () => ({ afterClosed: () => of(null) }) } },
        { provide: NotificationService, useValue: notify },
        {
          provide: OpcionesPublicacionService,
          useValue: { crud: { getAllByCriteria, delete: deleteFn } },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(PublicacionComponent);
    fixture.detectChanges();
  }

  it('se crea y la tabla pide datos al iniciar', async () => {
    await setup();
    expect(fixture.componentInstance).toBeTruthy();
    expect(getAllByCriteria).toHaveBeenCalled();
  });

  it('muestra el chip Default de la fila marcada', async () => {
    await setup();
    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('Default');
    expect(text).toContain('Estándar');
  });

  it('el borrado invoca delete y notifica éxito', async () => {
    await setup();
    const row: OpcionesPublicacion = {
      id: 1,
      nombre: 'Estándar',
      esDefault: true,
      ladoMayorPreview: 1600,
      ladoMayorThumb: 600,
      calidad: 80,
    };
    fixture.componentInstance['removeFn'](row)().subscribe();

    expect(deleteFn).toHaveBeenCalledWith(1);
    expect(notify.success).toHaveBeenCalledWith('Opciones eliminadas.');
  });
});
