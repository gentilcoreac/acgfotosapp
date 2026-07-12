import type { Mock, MockedObject } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { Router } from '@angular/router';
import { of } from 'rxjs';
import { NotificationService } from '../../../shared/feedback/notification.service';
import { TenantService } from '../data/tenant.service';
import { AplicacionOption, Tenant, TipoLicenciaOption } from '../domain/tenant.model';
import { TenantEditComponent } from './tenant-edit.component';

describe('TenantEditComponent', () => {
  let fixture: ComponentFixture<TenantEditComponent>;
  let save: Mock;
  let getById: Mock;
  let getAdministradores: Mock;
  let dialogRef: MockedObject<MatDialogRef<TenantEditComponent, Tenant>>;
  let router: MockedObject<Router>;

  const aplicaciones: AplicacionOption[] = [{ id: 10, nombre: 'App' }];
  const tipos: TipoLicenciaOption[] = [
    { id: 1, descripcion: 'Visualizador', esDefaultParaNuevoTenant: true },
    { id: 2, descripcion: 'Editor', esDefaultParaNuevoTenant: false },
  ];

  async function setup(
    data: {
      id?: number;
    } = {},
    loaded?: Tenant,
  ): Promise<void> {
    save = vi
      .fn()
      .mockName('save')
      .mockImplementation((t: Tenant) => of({ ...t, id: 7 }));
    getById = vi
      .fn()
      .mockName('getById')
      .mockReturnValue(of(loaded ?? null));
    getAdministradores = vi.fn().mockName('getAdministradores').mockReturnValue(of([]));
    dialogRef = {
      close: vi.fn().mockName('MatDialogRef.close'),
    } as unknown as MockedObject<MatDialogRef<TenantEditComponent, Tenant>>;
    router = { navigate: vi.fn().mockName('Router.navigate') } as unknown as MockedObject<Router>;

    await TestBed.configureTestingModule({
      imports: [TenantEditComponent],
      providers: [
        provideNoopAnimations(),
        { provide: MatDialogRef, useValue: dialogRef },
        { provide: Router, useValue: router },
        { provide: MAT_DIALOG_DATA, useValue: data },
        {
          provide: TenantService,
          useValue: {
            crud: { getById, save },
            getAplicaciones: () => of(aplicaciones),
            getTiposLicencia: () => of(tipos),
            getAdministradores,
          },
        },
        {
          provide: NotificationService,
          useValue: { success: () => undefined, error: () => undefined },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(TenantEditComponent);
    fixture.detectChanges();
  }

  const submit = (): void => {
    const form: HTMLFormElement = fixture.nativeElement.querySelector('form');
    form.dispatchEvent(new Event('submit'));
    fixture.detectChanges();
  };

  // Accede a los miembros protegidos (model / toggleApp) para no depender del render de cada tab.
  const comp = () =>
    fixture.componentInstance as unknown as {
      model: {
        (): Record<string, unknown>;
        update(fn: (m: Record<string, unknown>) => Record<string, unknown>): void;
      };
      toggleApp(id: number, checked: boolean): void;
      licenses(): {
        tipoLicenciaId: number;
      }[];
    };

  const patchModel = (patch: Record<string, unknown>): void => {
    comp().model.update((m) => ({ ...m, ...patch }));
    fixture.detectChanges();
  };

  it('alta: precarga la licencia default y guarda con usuario admin + apps + licencias', async () => {
    await setup();

    // La licencia default (Visualizador) se precargó al llegar los tipos.
    expect(comp().licenses().length).toBe(1);
    expect(comp().licenses()[0].tipoLicenciaId).toBe(1);

    patchModel({
      codigo: 'T1',
      nombre: 'Tenant 1',
      usuarioNombre: 'Ada',
      usuarioApellido: 'Lovelace',
      usuarioEmail: 'ada@tenant.com',
      usuarioUserName: 'ada',
      usuarioTelefono: '1234567890',
    });
    comp().toggleApp(10, true);
    fixture.detectChanges();
    submit();

    expect(save).toHaveBeenCalled();
    const payload = vi.mocked(save).mock.lastCall![0] as Tenant;
    expect(payload.codigo).toBe('T1');
    expect(payload.nombre).toBe('Tenant 1');
    expect(payload.usuario?.userName).toBe('ada');
    expect(payload.tenantAplicaciones.map((a) => a.aplicacionId)).toEqual([10]);
    expect(payload.tenantLicenses.map((l) => l.tipoLicenciaId)).toEqual([1]);
    // Sin custom theme → styleSheetFile/Url van en null (la API regenera por colores).
    expect(payload.styleSheetFile).toBeNull();
    expect(payload.styleSheetCssUrl).toBeNull();
    expect(dialogRef.close).toHaveBeenCalled();
  });

  it('edición: deshabilita código y admin, y no manda el usuario', async () => {
    const loaded: Tenant = {
      id: 7,
      codigo: 'ACME',
      nombre: 'Acme',
      tituloWeb: 'Acme BI',
      activo: true,
      hostName: 'acme.local',
      colorPrimarioDark: '#111111',
      colorPrimarioLight: '#222222',
      darkModeByDefault: false,
      logoLoginLightUrl: null,
      logoLoginDarkUrl: null,
      logoHeaderLightUrl: null,
      logoHeaderDarkUrl: null,
      faviconUrl: null,
      imagenFondoLoginLightUrl: null,
      imagenFondoLoginDarkUrl: null,
      styleSheetCssUrl: null,
      tipoLayoutLogin: 0,
      logoLoginLightFile: null,
      logoLoginDarkFile: null,
      logoHeaderLightFile: null,
      logoHeaderDarkFile: null,
      faviconFile: null,
      imagenFondoLoginLightFile: null,
      imagenFondoLoginDarkFile: null,
      styleSheetFile: null,
      tenantAplicaciones: [{ aplicacionId: 10, tenantId: 7 }],
      tenantLicenses: [
        {
          id: 3,
          tipoLicenciaId: 2,
          cantidad: 5,
          startDateTime: '2026-01-01',
          expireDateTime: '2027-01-01',
        },
      ],
    };
    await setup({ id: 7 }, loaded);

    expect(getById).toHaveBeenCalledWith(7);
    // Los admin se traen por el endpoint dedicado (solo lectura), no por el detalle.
    expect(getAdministradores).toHaveBeenCalledWith(7);
    expect(comp().model()['codigo']).toBe('ACME');

    submit();
    expect(save).toHaveBeenCalled();
    const payload = vi.mocked(save).mock.lastCall![0] as Tenant;
    expect(payload.id).toBe(7);
    expect(payload.usuario).toBeUndefined();
    // El listado de admin NUNCA se reenvía en el edit.
    expect((payload as unknown as Record<string, unknown>)['usuariosAdmin']).toBeUndefined();
    expect(payload.tenantAplicaciones.map((a) => a.aplicacionId)).toEqual([10]);
  });

});
