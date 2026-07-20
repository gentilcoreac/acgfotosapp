import type { Mock } from 'vitest';
import { WritableSignal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormControl } from '@angular/forms';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { Observable, of } from 'rxjs';
import { ConfirmService } from '../../../shared/feedback/confirm.service';
import { NotificationService } from '../../../shared/feedback/notification.service';
import { ParametrosValorTenantService } from '../data/parametros-valor-tenant.service';
import { ParametroValorRow, TIPO_DATO } from '../domain/parametro-valor-tenant.model';
import { ParametrosValorTenantComponent } from './parametros-valor-tenant.component';

/** Superficie protegida del componente que inspeccionamos desde los tests. */
interface Testable {
  tenantControl: FormControl<number | null>;
  aplicacionControl: FormControl<number | null>;
  tenantOptions(): {
    value: number;
    label: string;
  }[];
  aplicaciones(): {
    id: number;
    nombre: string;
  }[];
  rows(): ParametroValorRow[];
  editingId(): number | null;
  searched(): boolean;
  boolEdit: WritableSignal<boolean>;
  valorControl: FormControl<string>;
  startEdit(row: ParametroValorRow): void;
  saveFn(row: ParametroValorRow): () => Observable<unknown>;
  restoreFn(row: ParametroValorRow): () => Observable<unknown>;
}

function textRow(): ParametroValorRow {
  return {
    id: 19,
    nombre: 'Paginacion',
    descripcion: 'Filas por grilla',
    valor: '10',
    tipoDato: TIPO_DATO.Entero,
    aplicacionId: 1,
    parametroValorId: null,
    parametroValorDefaultValue: '10',
  };
}

function overriddenBoolRow(): ParametroValorRow {
  return {
    id: 20,
    nombre: 'MostrarLogo',
    descripcion: 'Logo en header',
    valor: 'false',
    tipoDato: TIPO_DATO.Booleano,
    aplicacionId: 1,
    parametroValorId: 55,
    parametroValorDefaultValue: 'true',
  };
}

describe('ParametrosValorTenantComponent', () => {
  let service: {
    getTenants: Mock;
    getAplicacionesPorTenant: Mock;
    getParametros: Mock;
    crud: {
      save: Mock;
      delete: Mock;
    };
  };

  async function setup(): Promise<{
    fixture: ComponentFixture<ParametrosValorTenantComponent>;
    cmp: Testable;
  }> {
    service = {
      getTenants: vi
        .fn()
        .mockName('getTenants')
        .mockReturnValue(of([{ id: 4, codigo: 'QA', nombre: 'QA Tenant' }])),
      getAplicacionesPorTenant: vi
        .fn()
        .mockName('getAplicacionesPorTenant')
        .mockReturnValue(of([{ id: 1, nombre: 'General' }])),
      getParametros: vi
        .fn()
        .mockName('getParametros')
        .mockReturnValue(of([textRow()])),
      crud: {
        save: vi
          .fn()
          .mockName('save')
          .mockReturnValue(of({ id: 7 })),
        delete: vi.fn().mockName('delete').mockReturnValue(of(null)),
      },
    };

    await TestBed.configureTestingModule({
      imports: [ParametrosValorTenantComponent],
      providers: [
        provideNoopAnimations(),
        { provide: ParametrosValorTenantService, useValue: service },
        { provide: ConfirmService, useValue: { confirm: () => of(true) } },
        {
          provide: NotificationService,
          useValue: { success: () => undefined, error: () => undefined },
        },
      ],
    }).compileComponents();

    const fixture = TestBed.createComponent(ParametrosValorTenantComponent);
    fixture.detectChanges();
    return { fixture, cmp: fixture.componentInstance as unknown as Testable };
  }

  it('carga los tenants en el selector al iniciar', async () => {
    const { fixture, cmp } = await setup();
    expect(cmp.tenantOptions()).toEqual([{ value: 4, label: 'QA — QA Tenant' }]);
    expect(fixture.nativeElement.textContent as string).toContain('Parámetros por tenant');
  });

  it('al elegir tenant carga las aplicaciones del tenant', async () => {
    const { fixture, cmp } = await setup();
    service.getAplicacionesPorTenant.mockReturnValue(
      of([
        { id: 1, nombre: 'General' },
        { id: 2, nombre: 'Otra' },
      ]),
    );

    cmp.tenantControl.setValue(4);
    fixture.detectChanges();

    expect(service.getAplicacionesPorTenant).toHaveBeenCalledWith(4);
    expect(cmp.aplicaciones().length).toBe(2);
  });

  it('si el tenant tiene una sola aplicación la autoselecciona y carga los parámetros', async () => {
    const { fixture, cmp } = await setup();

    cmp.tenantControl.setValue(4);
    fixture.detectChanges();
    // La autoselección de la única aplicación dispara un segundo ciclo de reactividad (nuevo valor
    // de `aplicacionControl` → `parametrosResource` refetchea) — una sola pasada de detectChanges
    // no alcanza a propagar ambos.
    fixture.detectChanges();

    expect(cmp.aplicacionControl.value).toBe(1);
    expect(service.getParametros).toHaveBeenCalledWith(4, 1);
    expect(cmp.rows().length).toBe(1);
    expect(cmp.searched()).toBe(true);
  });

  it('guardar un valor hace upsert (id ausente = alta) y actualiza la fila con el id del override', async () => {
    const { fixture, cmp } = await setup();
    cmp.tenantControl.setValue(4);
    fixture.detectChanges();
    fixture.detectChanges();
    const row = cmp.rows()[0];

    cmp.startEdit(row);
    cmp.valorControl.setValue('25');
    cmp.saveFn(row)().subscribe();

    expect(service.crud.save).toHaveBeenCalledWith({
      id: undefined,
      tenantId: 4,
      parametroId: 19,
      valor: '25',
    });
    const updated = cmp.rows()[0];
    expect(updated.valor).toBe('25');
    expect(updated.parametroValorId).toBe(7);
    expect(cmp.editingId()).toBeNull();
  });

  it('guardar un booleano envía "true"/"false"', async () => {
    const { fixture, cmp } = await setup();
    service.getParametros.mockReturnValue(of([overriddenBoolRow()]));
    cmp.tenantControl.setValue(4);
    fixture.detectChanges();
    fixture.detectChanges();
    const row = cmp.rows()[0];

    cmp.startEdit(row);
    cmp.boolEdit.set(true);
    cmp.saveFn(row)().subscribe();

    expect(service.crud.save).toHaveBeenCalledWith({
      id: 55,
      tenantId: 4,
      parametroId: 20,
      valor: 'true',
    });
  });

  it('restaurar borra el override y deja el valor por defecto', async () => {
    const { fixture, cmp } = await setup();
    service.getParametros.mockReturnValue(of([overriddenBoolRow()]));
    cmp.tenantControl.setValue(4);
    fixture.detectChanges();
    fixture.detectChanges();
    const row = cmp.rows()[0];

    cmp.restoreFn(row)().subscribe();

    expect(service.crud.delete).toHaveBeenCalledWith(55);
    const updated = cmp.rows()[0];
    expect(updated.parametroValorId).toBeNull();
    expect(updated.valor).toBe('true'); // parametroValorDefaultValue
  });
});
