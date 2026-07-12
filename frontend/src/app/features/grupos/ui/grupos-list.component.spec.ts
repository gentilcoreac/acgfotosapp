import type { Mock } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MatDialog } from '@angular/material/dialog';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { of } from 'rxjs';
import { NotificationService } from '../../../shared/feedback/notification.service';
import { GruposService } from '../data/grupos.service';
import { GruposListComponent } from './grupos-list.component';

describe('GruposListComponent', () => {
  let fixture: ComponentFixture<GruposListComponent>;
  let getAllByCriteria: Mock;

  beforeEach(async () => {
    getAllByCriteria = vi
      .fn()
      .mockName('getAllByCriteria')
      .mockReturnValue(of({ items: [], totalCount: 0 }));

    await TestBed.configureTestingModule({
      imports: [GruposListComponent],
      providers: [
        provideNoopAnimations(),
        { provide: MatDialog, useValue: { open: () => ({ afterClosed: () => of(null) }) } },
        {
          provide: NotificationService,
          useValue: { success: () => undefined, error: () => undefined },
        },
        {
          provide: GruposService,
          useValue: { crud: { getAllByCriteria, delete: () => of(undefined) } },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(GruposListComponent);
    fixture.detectChanges();
  });

  it('se crea y la tabla pide datos al iniciar', () => {
    expect(fixture.componentInstance).toBeTruthy();
    expect(getAllByCriteria).toHaveBeenCalled();
  });
});
