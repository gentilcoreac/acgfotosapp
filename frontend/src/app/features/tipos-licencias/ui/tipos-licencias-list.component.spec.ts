import type { MockedObject } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MatDialog } from '@angular/material/dialog';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { of } from 'rxjs';
import { NotificationService } from '../../../shared/feedback/notification.service';
import { TiposLicenciaService } from '../data/tipos-licencia.service';
import { TiposLicenciasListComponent } from './tipos-licencias-list.component';

describe('TiposLicenciasListComponent', () => {
  let fixture: ComponentFixture<TiposLicenciasListComponent>;
  let dialog: MockedObject<MatDialog>;

  beforeEach(async () => {
    dialog = {
      open: vi.fn().mockName('MatDialog.open'),
    } as unknown as MockedObject<MatDialog>;
    dialog.open.mockReturnValue({ afterClosed: () => of(undefined) } as ReturnType<
      MatDialog['open']
    >);

    await TestBed.configureTestingModule({
      imports: [TiposLicenciasListComponent],
      providers: [
        provideNoopAnimations(),
        { provide: MatDialog, useValue: dialog },
        {
          provide: TiposLicenciaService,
          useValue: { crud: { getAllByCriteria: () => of({ items: [], totalCount: 0 }) } },
        },
        {
          provide: NotificationService,
          useValue: { success: () => undefined, error: () => undefined },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(TiposLicenciasListComponent);
    fixture.detectChanges();
  });

  it('renderiza la lista y el botón Nuevo', () => {
    const newButton = fixture.nativeElement.querySelector('button');
    expect(newButton.textContent).toContain('Nuevo');
  });

  it('"Nuevo" abre el diálogo de edición', () => {
    const newButton: HTMLButtonElement = fixture.nativeElement.querySelector('button');
    newButton.click();
    expect(dialog.open).toHaveBeenCalled();
  });
});
