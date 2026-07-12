import type { Mock, MockedObject } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { of } from 'rxjs';
import { AuthStore } from '../../../core/auth';
import { LicenciasService } from '../../licencias/data/licencias.service';
import { NotificationService } from '../../../shared/feedback/notification.service';
import { UsuariosService } from '../data/usuarios.service';
import { RolEfectivo, Usuario } from '../domain/usuario.model';
import { UsuarioEditComponent } from './usuario-edit.component';

describe('UsuarioEditComponent', () => {
  let fixture: ComponentFixture<UsuarioEditComponent>;
  let save: Mock;
  let setAdministrador: Mock;
  let dialogRef: MockedObject<MatDialogRef<UsuarioEditComponent, Usuario>>;

  const roles = [
    { id: 1, descripcion: 'Administrador' },
    { id: 2, descripcion: 'Lector' },
  ];

  /**
   * Configura el TestBed. `isRoot` controla si el usuario es root; `data` con `id` activa el modo
   * edición (sin id = alta); `efectivos` son los roles efectivos que devuelve el endpoint read-only.
   */
  async function setup(
    isRoot: boolean,
    data: {
      id?: number;
    } = {},
    efectivos: RolEfectivo[] = [],
  ): Promise<void> {
    save = vi
      .fn()
      .mockName('save')
      .mockImplementation((u: Usuario) => of({ ...u, id: 7 }));
    setAdministrador = vi.fn().mockName('setAdministrador').mockReturnValue(of(undefined));
    dialogRef = {
      close: vi.fn().mockName('MatDialogRef.close'),
    } as unknown as MockedObject<MatDialogRef<UsuarioEditComponent, Usuario>>;

    const usuario: Usuario = {
      id: data.id,
      userName: 'aperez',
      nombre: 'Ana',
      apellido: 'Pérez',
      email: 'ana@test.com',
      telefono: null,
      bloqueado: false,
      emailConfirmed: true,
      administrador: false,
      // Edición: licencia activa 9 (incluye solo el rol 1) + rol directo 1. La licencia es tope duro.
      tipoLicenciaActiva: { tipoLicenciaId: 9, isActive: true, usuarioId: 7, id: 1 },
      roles: [{ rolId: 1, usuarioId: 7 }],
      usuarioAplicaciones: [],
    };

    await TestBed.configureTestingModule({
      imports: [UsuarioEditComponent],
      providers: [
        provideNoopAnimations(),
        { provide: MatDialogRef, useValue: dialogRef },
        { provide: MAT_DIALOG_DATA, useValue: data }, // sin id → alta; con id → edición
        { provide: AuthStore, useValue: { isRoot: () => isRoot } },
        { provide: LicenciasService, useValue: { getResumen: () => of([]) } },
        {
          provide: NotificationService,
          useValue: { success: () => undefined, error: () => undefined },
        },
        {
          provide: UsuariosService,
          useValue: {
            crud: { getById: () => of(data.id != null ? usuario : null), save },
            getTiposLicencia: () => of([{ id: 5, descripcion: 'Full' }]),
            // La licencia 9 habilita solo el rol 1; cualquier otra, ambos roles.
            getRolesDeLicencia: (id: number) =>
              of(id === 9 ? [{ id: 1, descripcion: 'Administrador' }] : roles),
            getAplicaciones: () => of([]),
            getRolesEfectivos: () => of(efectivos),
            setAdministrador,
            bloquearDesbloquear: () => of(true),
            reConfirmarCuenta: () => of(true),
          },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(UsuarioEditComponent);
    fixture.detectChanges();
  }

  // Setea un tbi-text-field por índice (0 = usuario, 1 = nombre, 2 = apellido, 3 = email, 4 = tel).
  const setTextField = (index: number, text: string): void => {
    const inputs = fixture.nativeElement.querySelectorAll(
      'tbi-text-field input',
    ) as NodeListOf<HTMLInputElement>;
    inputs[index].value = text;
    inputs[index].dispatchEvent(new Event('input'));
    fixture.detectChanges();
  };

  // Los tabs Roles/Aplicaciones se renderizan sólo al activarse; ejercemos la API del componente
  // (selección de roles y flag administrador) en vez de depender del DOM dentro del tab inactivo.
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  const component = (): any => fixture.componentInstance;

  const submit = (): void => {
    const form: HTMLFormElement = fixture.nativeElement.querySelector('form');
    form.dispatchEvent(new Event('submit'));
    fixture.detectChanges();
  };

  const fillRequired = (): void => {
    setTextField(0, 'aperez'); // usuario
    setTextField(1, 'Ana'); // nombre
    setTextField(2, 'Pérez'); // apellido
    setTextField(3, 'ana@test.com'); // email
  };

  it('no muestra el toggle de administrador si no es root', async () => {
    await setup(false);
    expect(fixture.nativeElement.querySelector('mat-slide-toggle')).toBeNull();
  });

  it('muestra el toggle de administrador si es root', async () => {
    await setup(true);
    expect(fixture.nativeElement.querySelector('mat-slide-toggle')).not.toBeNull();
  });

  it('guarda enviando los roles como objetos { rolId } (alta)', async () => {
    await setup(false);
    fillRequired();
    component().toggleRole(1, true);
    fixture.detectChanges();
    submit();
    expect(save).toHaveBeenCalled();
    const payload = vi.mocked(save).mock.lastCall![0] as Usuario;
    expect(payload.userName).toBe('aperez');
    expect(payload.nombre).toBe('Ana');
    expect(payload.email).toBe('ana@test.com');
    expect(payload.roles).toEqual([{ rolId: 1, usuarioId: 0 }]);
    expect(payload.administrador).toBeUndefined();
    expect(setAdministrador).not.toHaveBeenCalled();
  });

  it('al elegir una licencia carga los roles que habilita', async () => {
    await setup(false);
    expect(component().licenseRoles().length).toBe(0);
    component().model.update((m: Record<string, unknown>) => ({ ...m, tipoLicenciaId: 5 }));
    fixture.detectChanges();
    expect(component().licenseRoles().length).toBe(2);
  });

  it('en alta NO muestra la pestaña "Roles efectivos"', async () => {
    await setup(false);
    expect(fixture.nativeElement.textContent as string).not.toContain('Roles efectivos');
  });

  it('en edición compone roles efectivos = directos (del form) ∪ grupos (del back), con tope de licencia', async () => {
    // El back devuelve el rol de grupo; los directos los aporta el form (rol 1, de la licencia 9).
    await setup(false, { id: 7 }, [
      {
        rolId: 2,
        rolDescripcion: 'Lector',
        origen: 'Grupo',
        grupoId: 3,
        grupoNombre: 'Ventas',
        permitidoPorLicencia: false,
      },
    ]);
    expect(fixture.nativeElement.textContent as string).toContain('Ver Roles efectivos');
    const efectivos = component().rolesEfectivos() as RolEfectivo[];
    expect(efectivos.length).toBe(2);
    // Directo: viene del rol marcado en el form; permitido (la licencia 9 lo incluye).
    expect(efectivos[0].origen).toBe('Directo');
    expect(efectivos[0].rolId).toBe(1);
    expect(efectivos[0].permitidoPorLicencia).toBe(true);
    // Grupo: bloqueado por licencia (el rol 2 excede la licencia 9 del usuario).
    expect(efectivos[1].origen).toBe('Grupo');
    expect(efectivos[1].grupoNombre).toBe('Ventas');
    expect(efectivos[1].permitidoPorLicencia).toBe(false);
  });

  it('la tab "Ver Roles efectivos" reacciona al cambio de licencia (recalcula el tope sin guardar)', async () => {
    await setup(false, { id: 7 }, [
      {
        rolId: 2,
        rolDescripcion: 'Lector',
        origen: 'Grupo',
        grupoId: 3,
        grupoNombre: 'Ventas',
        permitidoPorLicencia: false,
      },
    ]);
    const grupoRow = (): RolEfectivo =>
      (component().rolesEfectivos() as RolEfectivo[]).find((r) => r.origen === 'Grupo')!;
    // Con la licencia 9 (solo rol 1) el rol 2 del grupo está bloqueado.
    expect(grupoRow().permitidoPorLicencia).toBe(false);
    // Cambiar a la licencia 5 (incluye rol 1 y 2) lo desbloquea al instante, sin guardar.
    component().model.update((m: Record<string, unknown>) => ({ ...m, tipoLicenciaId: 5 }));
    fixture.detectChanges();
    expect(grupoRow().permitidoPorLicencia).toBe(true);
  });

  // Activa la pestaña "Ver Roles efectivos" (su cuerpo se renderiza sólo al seleccionarla).
  const activarTabEfectivos = async (): Promise<void> => {
    const tabs = Array.from(
      fixture.nativeElement.querySelectorAll('.mat-mdc-tab'),
    ) as HTMLElement[];
    tabs.find((t) => (t.textContent ?? '').includes('Ver Roles efectivos'))!.click();
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
  };

  const rolBloqueadoGrupo: RolEfectivo = {
    rolId: 2,
    rolDescripcion: 'Lector',
    origen: 'Grupo',
    grupoId: 3,
    grupoNombre: 'Ventas',
    permitidoPorLicencia: false,
  };

  it('no-root: en la tab muestra la marca "Bloqueado por licencia" del rol que excede la licencia', async () => {
    await setup(false, { id: 7 }, [rolBloqueadoGrupo]);
    await activarTabEfectivos();
    expect(fixture.nativeElement.textContent as string).toContain('Bloqueado por licencia');
  });

  it('root: oculta las marcas de licencia y muestra el mensaje de root (§10.5)', async () => {
    await setup(true, { id: 7 }, [rolBloqueadoGrupo]);
    await activarTabEfectivos();
    const text = fixture.nativeElement.textContent as string;
    // root es exento: NO se muestra "Bloqueado por licencia" aunque el computed marque el rol bloqueado.
    expect(text).not.toContain('Bloqueado por licencia');
    expect(text).toContain('tope de licencia no aplica');
  });

  it('root: si activa administrador, lo persiste por el endpoint dedicado tras guardar', async () => {
    await setup(true);
    fillRequired();
    component().model.update((m: Record<string, unknown>) => ({ ...m, administrador: true }));
    fixture.detectChanges();
    submit();
    expect(save).toHaveBeenCalled();
    expect(setAdministrador).toHaveBeenCalledWith(7, true);
    expect(dialogRef.close).toHaveBeenCalled();
  });
});
