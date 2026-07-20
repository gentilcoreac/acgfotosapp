import type { Mock } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MatDialogRef } from '@angular/material/dialog';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { Observable, of, throwError } from 'rxjs';
import { ImpersonatableUser, ImpersonationService } from '../../core/auth';
import { TenantService } from '../../features/tenant/data/tenant.service';
import { ImpersonationDialogComponent } from './impersonation-dialog.component';

/** Superficie protegida del componente que inspeccionamos desde los tests. */
interface Testable {
  onTenantChange(tenantId: number): void;
  tenants(): {
    id: number;
    nombre: string;
  }[];
  users(): ImpersonatableUser[];
  error(): string | null;
  loadingUsers(): boolean;
}

const sampleUser: ImpersonatableUser = {
  id: 10009,
  userName: 'qa.admin',
  nombre: 'QA',
  apellido: 'Admin',
  email: 'q@t',
};

describe('ImpersonationDialogComponent', () => {
  let impersonation: {
    getImpersonatableUsers: Mock;
    impersonate: Mock;
  };

  async function setup(
    tenants: Observable<{
      items: {
        id: number | null;
        nombre: string;
      }[];
    }> = of({ items: [] }),
  ): Promise<{
    fixture: ComponentFixture<ImpersonationDialogComponent>;
    cmp: Testable;
  }> {
    impersonation = {
      getImpersonatableUsers: vi.fn().mockName('getImpersonatableUsers').mockReturnValue(of([])),
      impersonate: vi.fn().mockName('impersonate').mockReturnValue(of({})),
    };

    await TestBed.configureTestingModule({
      imports: [ImpersonationDialogComponent],
      providers: [
        provideNoopAnimations(),
        {
          provide: MatDialogRef,
          useValue: {
            close: vi.fn().mockName('MatDialogRef.close'),
          },
        },
        { provide: TenantService, useValue: { crud: { getAll: () => tenants } } },
        { provide: ImpersonationService, useValue: impersonation },
      ],
    }).compileComponents();

    const fixture = TestBed.createComponent(ImpersonationDialogComponent);
    fixture.detectChanges();
    return { fixture, cmp: fixture.componentInstance as unknown as Testable };
  }

  it('carga los tenants al iniciar', async () => {
    const { cmp } = await setup(of({ items: [{ id: 4, nombre: 'QA Tenant' }] }));
    expect(cmp.tenants()).toEqual([{ id: 4, nombre: 'QA Tenant' }]);
    expect(cmp.error()).toBeNull();
  });

  it('si falla la carga de tenants muestra un error en vez de quedar vacío', async () => {
    const { fixture, cmp } = await setup(throwError(() => new Error('boom')));
    expect(cmp.tenants()).toEqual([]);
    expect(cmp.error()).toBe('No se pudieron cargar los tenants. Intentá de nuevo más tarde.');
    expect(fixture.nativeElement.textContent as string).toContain(
      'No se pudieron cargar los tenants',
    );
  });

  it('onTenantChange carga los usuarios del tenant elegido', async () => {
    const { fixture, cmp } = await setup();
    impersonation.getImpersonatableUsers.mockReturnValue(of([sampleUser]));

    cmp.onTenantChange(4);
    fixture.detectChanges();

    expect(impersonation.getImpersonatableUsers).toHaveBeenCalledWith(4);
    expect(cmp.users()).toEqual([sampleUser]);
    expect(cmp.loadingUsers()).toBe(false);
    expect(cmp.error()).toBeNull();
  });

  it('si falla la carga de usuarios muestra un error y corta el loading', async () => {
    const { fixture, cmp } = await setup();
    impersonation.getImpersonatableUsers.mockReturnValue(throwError(() => new Error('boom')));

    cmp.onTenantChange(4);
    fixture.detectChanges();

    expect(cmp.loadingUsers()).toBe(false);
    expect(cmp.error()).toBe('No se pudieron cargar los usuarios del tenant seleccionado.');
  });

  it('al cambiar de tenant limpia el error previo', async () => {
    const { fixture, cmp } = await setup();
    impersonation.getImpersonatableUsers
      .mockReturnValueOnce(throwError(() => new Error('boom')))
      .mockReturnValueOnce(of([sampleUser]));

    cmp.onTenantChange(4);
    fixture.detectChanges();
    expect(cmp.error()).not.toBeNull();

    cmp.onTenantChange(7);
    fixture.detectChanges();
    expect(cmp.error()).toBeNull();
    expect(cmp.users()).toEqual([sampleUser]);
  });
});
