import type { MockedObject } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MatDialog } from '@angular/material/dialog';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { Router } from '@angular/router';
import { of } from 'rxjs';
import { NotificationService } from '../../../shared/feedback/notification.service';
import { TenantService } from '../data/tenant.service';
import { TenantsListComponent } from './tenants-list.component';

describe('TenantsListComponent', () => {
  let fixture: ComponentFixture<TenantsListComponent>;
  let dialog: MockedObject<MatDialog>;
  let router: MockedObject<Router>;

  beforeEach(async () => {
    dialog = {
      open: vi.fn().mockName('MatDialog.open'),
    } as unknown as MockedObject<MatDialog>;
    dialog.open.mockReturnValue({ afterClosed: () => of(undefined) } as ReturnType<
      MatDialog['open']
    >);
    router = { navigate: vi.fn().mockName('Router.navigate') } as unknown as MockedObject<Router>;

    await TestBed.configureTestingModule({
      imports: [TenantsListComponent],
      providers: [
        provideNoopAnimations(),
        { provide: MatDialog, useValue: dialog },
        { provide: Router, useValue: router },
        {
          provide: TenantService,
          useValue: {
            crud: { getAllByCriteria: () => of({ items: [], totalCount: 0 }) },
          },
        },
        {
          provide: NotificationService,
          useValue: { success: () => undefined, error: () => undefined },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(TenantsListComponent);
    fixture.detectChanges();
  });

  it('renderiza la lista y el botón Nuevo', () => {
    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('Nuevo');
  });

  it('"Nuevo" abre el diálogo de edición', () => {
    const buttons = Array.from(
      fixture.nativeElement.querySelectorAll('button'),
    ) as HTMLButtonElement[];
    const nuevo = buttons.find((b) => b.textContent?.includes('Nuevo'));
    nuevo?.click();
    expect(dialog.open).toHaveBeenCalled();
  });

});
