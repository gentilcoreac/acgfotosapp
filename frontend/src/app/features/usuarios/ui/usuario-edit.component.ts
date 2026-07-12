import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
  signal,
} from '@angular/core';
import { rxResource, takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
  FormField,
  disabled,
  email,
  form,
  maxLength,
  pattern,
  required,
} from '@angular/forms/signals';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatDialogModule } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatTabsModule } from '@angular/material/tabs';
import { MatTooltipModule } from '@angular/material/tooltip';
import { Observable, catchError, of } from 'rxjs';
import { AuthStore } from '../../../core/auth';
import { LicenciasService } from '../../licencias/data/licencias.service';
import { LicenciaResumen } from '../../licencias/domain/licencia-resumen.model';
import { estadoVigencia } from '../../licencias/domain/licencia-metrics';
import { LicenciasWarningBannerComponent } from '../../licencias/ui/licencias-warning-banner.component';
import { NotificationService } from '../../../shared/feedback/notification.service';
import { EditComponentBase } from '../../../shared/forms/edit-component-base';
import { TbiButtonComponent } from '../../../shared/ui/tbi-button/tbi-button.component';
import {
  TbiChipTone,
  TbiStatusChipComponent,
} from '../../../shared/ui/tbi-status-chip/tbi-status-chip.component';
import {
  TbiSelectComponent,
  TbiSelectOption,
} from '../../../shared/ui/tbi-select/tbi-select.component';
import { TbiTextFieldComponent } from '../../../shared/ui/tbi-text-field/tbi-text-field.component';
import { toggleInSet } from '../../../shared/util/collections';
import { UsuariosService } from '../data/usuarios.service';
import {
  AplicacionOption,
  RolEfectivo,
  ROLES_USUARIO_DESCRIPCION,
  RolOption,
  TipoLicenciaOption,
  Usuario,
} from '../domain/usuario.model';

interface UsuarioFormModel {
  userName: string;
  nombre: string;
  apellido: string;
  email: string;
  /** Se edita como texto (input numérico con pattern 10-13 dígitos); se convierte en `toEntity`. */
  telefono: string;
  /** Reservado a root; se persiste por endpoint dedicado (ver afterSave), no en el update. */
  administrador: boolean;
  tipoLicenciaId: number | null;
  aplicacionDefaultId: number | null;
}

// TODO (Fase 4 - i18n): textos en español por ahora.
@Component({
  selector: 'tbi-usuario-edit',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FormField,
    MatDialogModule,
    MatButtonModule,
    MatIconModule,
    MatProgressBarModule,
    MatProgressSpinnerModule,
    MatCheckboxModule,
    MatSlideToggleModule,
    MatTabsModule,
    MatTooltipModule,
    TbiTextFieldComponent,
    TbiSelectComponent,
    TbiButtonComponent,
    TbiStatusChipComponent,
    LicenciasWarningBannerComponent,
  ],
  templateUrl: './usuario-edit.component.html',
  styleUrl: './usuario-edit.component.scss',
})
export class UsuarioEditComponent extends EditComponentBase<Usuario, UsuarioFormModel> {
  private readonly service = inject(UsuariosService);
  private readonly licenciasService = inject(LicenciasService);
  private readonly notify = inject(NotificationService);
  protected readonly crud = this.service.crud;
  private loaded?: Usuario;

  /** Solo root puede promover/demotar administrador (la API lo valida; acá es UX). */
  protected readonly isRoot = inject(AuthStore).isRoot;
  private originalAdministrador = false;

  protected readonly tiposLicencia = signal<TipoLicenciaOption[]>([]);
  /** Roles que habilita la licencia elegida (la lista a asignar depende de la licencia). */
  protected readonly licenseRoles = signal<RolOption[]>([]);

  /** Resumen de licencias del tenant (estado de vencimiento por tipo) para avisar en el ABM. */
  private readonly resumenLicencias = signal<LicenciaResumen[]>([]);

  protected readonly model = signal<UsuarioFormModel>({
    userName: '',
    nombre: '',
    apellido: '',
    email: '',
    telefono: '',
    administrador: false,
    tipoLicenciaId: null,
    aplicacionDefaultId: null,
  });

  // Teléfono opcional; si se carga, 10-13 dígitos (mismo rango que valida la API en
  // UsuarioDtoValidator; el pattern no dispara con el campo vacío). No admite 0 inicial: la API lo
  // modela como número y '011…' se corrompería en silencio a 11…. userName y email son
  // inmutables en edición (la API preserva el normalizado) → disabled declarativo.
  protected readonly form = form(this.model, (path) => {
    required(path.userName, { message: 'Requerido' });
    maxLength(path.userName, 50, { message: 'Máximo 50 caracteres' });
    required(path.nombre, { message: 'Requerido' });
    required(path.apellido, { message: 'Requerido' });
    required(path.email, { message: 'Requerido' });
    email(path.email, { message: 'Email inválido' });
    pattern(path.telefono, /^[1-9]\d{9,12}$/, {
      message: 'El teléfono debe tener entre 10 y 13 dígitos y no empezar con 0',
    });
    disabled(path.userName, { when: () => this.isEdit });
    disabled(path.email, { when: () => this.isEdit });
  });

  /**
   * Licencia elegida envuelta para el banner de aviso: lista con la licencia si está vencida o por
   * vencer, vacía si no (el banner ya filtra, pero así evitamos renderizarlo de más). No aplica a
   * root (sus licencias son del tenant de sistema; ver el listado de usuarios). Lee la licencia
   * elegida directo del modelo del form (reactivo).
   */
  protected readonly licenciaSeleccionadaAlerta = computed<LicenciaResumen[]>(() => {
    if (this.isRoot()) {
      return [];
    }
    const id = this.form.tipoLicenciaId().value();
    if (id == null) {
      return [];
    }
    const lic = this.resumenLicencias().find((l) => l.tipoLicenciaId === id);
    return lic ? [lic] : [];
  });

  /**
   * Estado de **acceso** del usuario según su licencia, para el panel de estado. Resuelve la
   * aparente contradicción "cuenta Activa pero sin licencia": el lockout (Activo/Bloqueado) y la
   * licencia son ejes distintos, y la API **bloquea el login** si la licencia falta o venció
   * (AuthController: NoActiveLicense / ErrorLicenseExpired). Reusa el resumen ya cargado (sin
   * llamadas nuevas) y la licencia elegida en el form (reactivo). No aplica a root (exento de
   * licencia).
   */
  protected readonly acceso = computed<{
    label: string;
    tone: TbiChipTone;
    icon: string;
    bloquea: boolean;
  }>(() => {
    const id = this.form.tipoLicenciaId().value();
    if (id == null) {
      return { label: 'Sin licencia', tone: 'error', icon: 'block', bloquea: true };
    }
    const lic = this.resumenLicencias().find((l) => l.tipoLicenciaId === id);
    switch (lic ? estadoVigencia(lic) : 'vigente') {
      case 'vencida':
        return { label: 'Licencia vencida', tone: 'error', icon: 'error', bloquea: true };
      case 'por-vencer':
        return { label: 'Licencia por vencer', tone: 'warning', icon: 'schedule', bloquea: false };
      default:
        return {
          label: 'Licencia vigente',
          tone: 'success',
          icon: 'verified_user',
          bloquea: false,
        };
    }
  });

  /**
   * Severidad del aviso de la licencia elegida, para señalizar el **tab** "Licencia y Roles" (que
   * el usuario vea que hay algo aunque no esté en esa pestaña). Reusa el mismo computed que el
   * banner (no agrega ninguna llamada). `none` = sin aviso.
   */
  protected readonly licenciaTabSeveridad = computed<'none' | 'warning' | 'error'>(() => {
    const [lic] = this.licenciaSeleccionadaAlerta();
    if (!lic) {
      return 'none';
    }
    switch (estadoVigencia(lic)) {
      case 'vencida':
        return 'error';
      case 'por-vencer':
        return 'warning';
      default:
        return 'none';
    }
  });
  protected readonly aplicaciones = signal<AplicacionOption[]>([]);
  protected readonly selectedRoleIds = signal<ReadonlySet<number>>(new Set());
  protected readonly selectedAppIds = signal<ReadonlySet<number>>(new Set());

  /**
   * Roles efectivos por **grupo** (read-only) tal como los devuelve el back. Los roles **directos** NO
   * se guardan acá: se derivan del form (`selectedRoleIds`) para que la tab sea reactiva a lo que se
   * edita sin guardar. Solo en edición.
   */
  private readonly grupoRolesEfectivos = signal<RolEfectivo[]>([]);
  /** Descripciones de rol cacheadas del fetch (cubre roles que no estén en la licencia actual). */
  private readonly rolDescripciones = signal<ReadonlyMap<number, string>>(new Map());
  protected readonly loadingEfectivos = signal(false);

  /**
   * Roles efectivos a mostrar = **directos** (del form, reactivos) ∪ **de grupo** (del back), cada uno
   * con su marca `permitidoPorLicencia` recalculada **en vivo** contra la licencia elegida en el form
   * (`licenseRoles`). Así cambiar la licencia o tildar/destildar un rol en las otras pestañas se ve al
   * instante, sin guardar. Read-only.
   */
  protected readonly rolesEfectivos = computed<RolEfectivo[]>(() => {
    const licencia = this.licenseRoles();
    const licenciaIds = new Set(licencia.map((r) => r.id));
    const descripciones = this.rolDescripciones();
    const describe = (id: number): string =>
      licencia.find((r) => r.id === id)?.descripcion ?? descripciones.get(id) ?? `Rol ${id}`;
    const directos: RolEfectivo[] = [...this.selectedRoleIds()].map((rolId) => ({
      rolId,
      rolDescripcion: describe(rolId),
      origen: 'Directo',
      grupoId: null,
      grupoNombre: null,
      permitidoPorLicencia: licenciaIds.has(rolId),
    }));
    const grupos: RolEfectivo[] = this.grupoRolesEfectivos().map((r) => ({
      ...r,
      permitidoPorLicencia: licenciaIds.has(r.rolId),
    }));
    return [...directos, ...grupos];
  });
  /** Texto compartido que describe la composición de roles del usuario (tab "Roles efectivos"). */
  protected readonly rolesUsuarioDesc = ROLES_USUARIO_DESCRIPCION;

  /** Estado de bloqueo: lo cambia el botón dedicado (endpoint propio), no el form. */
  protected readonly bloqueado = signal(false);
  /** Se muestra el botón "reconfirmar" sólo si la cuenta no está confirmada. */
  protected readonly emailConfirmed = signal(true);
  // Estado por acción dedicada: cada botón muestra su propio spinner; `busyAction` deshabilita
  // ambos mientras cualquiera esté en curso.
  protected readonly bloqueando = signal(false);
  protected readonly reenviando = signal(false);
  protected readonly busyAction = computed(() => this.bloqueando() || this.reenviando());

  protected readonly tipoLicenciaOptions = computed<TbiSelectOption<number>[]>(() =>
    this.tiposLicencia().map((t) => ({ value: t.id, label: t.descripcion })),
  );

  /** Opciones del select de "aplicación por defecto": sólo las aplicaciones seleccionadas. */
  protected readonly defaultAppOptions = computed<TbiSelectOption<number>[]>(() => {
    const selected = this.selectedAppIds();
    return this.aplicaciones()
      .filter((a) => selected.has(a.id))
      .map((a) => ({ value: a.id, label: a.nombre }));
  });

  /** Refetchea sola cuando cambia la licencia elegida (cancela el pedido en vuelo anterior). */
  private readonly licenseRolesResource = rxResource({
    params: () => this.form.tipoLicenciaId().value(),
    stream: ({ params: id }) =>
      id == null
        ? of<RolOption[]>([])
        : this.service
            .getRolesDeLicencia(id)
            // el toast de error lo emite el errorInterceptor (global)
            .pipe(catchError(() => of<RolOption[] | null>(null))),
  });

  /** El patch inicial (edición) arma esta licencia ANTES de volcar el modelo: cuando el effect
   * corra y la licencia ACTUAL siga siendo esta, no poda la selección — se compara contra el valor
   * vigente en vez de un booleano "consumido" (un booleano es frágil si dos cambios de licencia
   * ocurren antes de que el effect llegue a correr para el primero: se coalescen en una sola
   * ejecución y el booleano se consumiría para el cambio equivocado). `undefined` = sin supresión
   * armada (distinto de `null`, que es "sin licencia elegida", un id válido). */
  private suppressPruneForTipoLicenciaId: number | null | undefined = undefined;

  constructor() {
    super();
    // La licencia define los roles asignables: al cambiarla se recargan sus roles y se podan los
    // seleccionados que ya no aplican (salvo el patch inicial, ver `suppressPruneForTipoLicenciaId`).
    effect(() => {
      const currentTipoLicenciaId = this.form.tipoLicenciaId().value();
      const roles = this.licenseRolesResource.value();
      if (roles == null) {
        return;
      }
      const suppress = this.suppressPruneForTipoLicenciaId === currentTipoLicenciaId;
      this.suppressPruneForTipoLicenciaId = undefined;
      this.licenseRoles.set(roles);
      if (suppress) {
        return;
      }
      const allowed = new Set(roles.map((r) => r.id));
      this.selectedRoleIds.update((cur) => new Set([...cur].filter((id) => allowed.has(id))));
    });
  }

  override ngOnInit(): void {
    super.ngOnInit();
    this.service.getTiposLicencia().subscribe({
      next: (tipos) => this.tiposLicencia.set(tipos),
      // el toast de error lo emite el errorInterceptor (global)
      error: () => undefined,
    });
    this.service.getAplicaciones().subscribe({
      next: (apps) => this.aplicaciones.set(apps),
      // el toast de error lo emite el errorInterceptor (global)
      error: () => undefined,
    });

    // Resumen de licencias del tenant para el aviso de vencimiento. Root no tiene licencias de
    // negocio (ver UsuariosListComponent), así que no lo pedimos.
    if (!this.isRoot()) {
      this.licenciasService
        .getResumen()
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: (resumen) => this.resumenLicencias.set(resumen),
          error: () => this.resumenLicencias.set([]),
        });
    }
  }

  protected toggleRole(rolId: number, checked: boolean): void {
    this.selectedRoleIds.update((current) => toggleInSet(current, rolId, checked));
  }

  protected toggleApp(aplicacionId: number, checked: boolean): void {
    this.selectedAppIds.update((current) => toggleInSet(current, aplicacionId, checked));
    if (!checked && this.model().aplicacionDefaultId === aplicacionId) {
      this.model.update((m) => ({ ...m, aplicacionDefaultId: null }));
    }
  }

  protected isRoleChecked(rolId: number): boolean {
    return this.selectedRoleIds().has(rolId);
  }

  protected isAppChecked(aplicacionId: number): boolean {
    return this.selectedAppIds().has(aplicacionId);
  }

  protected toEntity(): Usuario {
    const value = this.model();
    const usuarioId = this.loaded?.id ?? 0;
    const defaultId = value.aplicacionDefaultId;
    const tipoLicenciaId = value.tipoLicenciaId;
    return {
      ...this.loaded,
      userName: value.userName,
      nombre: value.nombre,
      apellido: value.apellido,
      email: value.email,
      telefono: value.telefono ? Number(value.telefono) : null,
      bloqueado: this.bloqueado(),
      roles: [...this.selectedRoleIds()].map((rolId) => ({ rolId, usuarioId })),
      usuarioAplicaciones: [...this.selectedAppIds()].map((aplicacionId) => ({
        aplicacionId,
        usuarioId,
        default: aplicacionId === defaultId,
      })),
      // Licencia activa: la procesa la API (cupos/activación). null si no se eligió ninguna.
      tipoLicenciaActiva: tipoLicenciaId
        ? {
            tipoLicenciaId,
            isActive: true,
            usuarioId,
            id: this.loaded?.tipoLicenciaActiva?.id ?? 0,
          }
        : null,
      // Gestionados por endpoints dedicados: no viajan en el update.
      administrador: undefined,
      emailConfirmed: undefined,
      ultimoLogin: undefined,
    };
  }

  protected patchForm(entity: Usuario): void {
    this.loaded = entity;
    this.originalAdministrador = entity.administrador ?? false;
    this.bloqueado.set(entity.bloqueado ?? false);
    this.emailConfirmed.set(entity.emailConfirmed ?? true);
    const tipoLicenciaId = entity.tipoLicenciaActiva?.tipoLicenciaId ?? null;
    // Arma la supresión para esta licencia ANTES de volcar el modelo: el effect no trata este
    // patch como cambio del usuario (no poda la selección) — `licenseRolesResource` refetchea sola.
    this.suppressPruneForTipoLicenciaId = tipoLicenciaId;
    this.model.set({
      userName: entity.userName ?? '',
      nombre: entity.nombre,
      apellido: entity.apellido,
      email: entity.email,
      telefono: entity.telefono != null ? String(entity.telefono) : '',
      administrador: this.originalAdministrador,
      tipoLicenciaId,
      aplicacionDefaultId:
        (entity.usuarioAplicaciones ?? []).find((a) => a.default)?.aplicacionId ?? null,
    });
    this.selectedAppIds.set(new Set((entity.usuarioAplicaciones ?? []).map((a) => a.aplicacionId)));
    this.selectedRoleIds.set(new Set((entity.roles ?? []).map((r) => r.rolId)));
    this.loadRolesEfectivos(entity.id);
  }

  /**
   * Trae del back los roles efectivos del usuario. Solo conserva los de **origen grupo** (los directos
   * los aporta el form, reactivos) y cachea las descripciones. La tab (`rolesEfectivos`, computed) los
   * combina con los directos del form.
   */
  private loadRolesEfectivos(usuarioId?: number): void {
    if (usuarioId == null) {
      this.grupoRolesEfectivos.set([]);
      return;
    }
    this.loadingEfectivos.set(true);
    this.service.getRolesEfectivos(usuarioId).subscribe({
      next: (roles) => {
        this.grupoRolesEfectivos.set(roles.filter((r) => r.origen === 'Grupo'));
        this.rolDescripciones.set(new Map(roles.map((r) => [r.rolId, r.rolDescripcion])));
        this.loadingEfectivos.set(false);
      },
      error: () => this.loadingEfectivos.set(false),
    });
  }

  /** Bloquea/desbloquea por el endpoint dedicado (sólo en edición; necesita el userName). */
  protected toggleBloqueo(): void {
    if (!this.loaded?.userName) {
      return;
    }
    const next = !this.bloqueado();
    this.bloqueando.set(true);
    this.service.bloquearDesbloquear(this.loaded.userName, next).subscribe({
      next: () => {
        this.bloqueado.set(next);
        this.bloqueando.set(false);
        this.notify.success(next ? 'Usuario bloqueado.' : 'Usuario desbloqueado.');
      },
      error: () => this.bloqueando.set(false),
    });
  }

  /** Reenvía el mail de confirmación (sólo en edición y si la cuenta no está confirmada). */
  protected reConfirmar(): void {
    if (this.loaded?.id == null) {
      return;
    }
    this.reenviando.set(true);
    this.service.reConfirmarCuenta(this.loaded.id).subscribe({
      next: () => {
        this.reenviando.set(false);
        this.notify.success('Mail de confirmación reenviado.');
      },
      error: () => this.reenviando.set(false),
    });
  }

  /**
   * Tras el update normal, si es root y cambió el flag administrador, lo persiste por el endpoint
   * dedicado `usuarios/{id}/set-administrador` (sirve para alta y edición). Si no, no hace nada.
   */
  protected override afterSave(saved: Usuario): Observable<unknown> {
    const value = this.model().administrador;
    if (!this.isRoot() || saved.id == null || value === this.originalAdministrador) {
      return of(saved);
    }
    return this.service.setAdministrador(saved.id, value);
  }
}
