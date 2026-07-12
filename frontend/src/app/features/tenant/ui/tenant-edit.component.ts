import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
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
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSelectModule } from '@angular/material/select';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatTabsModule } from '@angular/material/tabs';
import { Router } from '@angular/router';
import { EditComponentBase } from '../../../shared/forms/edit-component-base';
import { NotificationService } from '../../../shared/feedback/notification.service';
import { TbiButtonComponent } from '../../../shared/ui/tbi-button/tbi-button.component';
import {
  TbiFileResource,
  TbiFileUploadComponent,
} from '../../../shared/ui/tbi-file-upload/tbi-file-upload.component';
import {
  TbiSelectComponent,
  TbiSelectOption,
} from '../../../shared/ui/tbi-select/tbi-select.component';
import { TbiTextFieldComponent } from '../../../shared/ui/tbi-text-field/tbi-text-field.component';
import { plusOneYear, toDateInput, todayLocalIso } from '../../../shared/util/dates';
import { TenantService } from '../data/tenant.service';
import {
  AplicacionOption,
  Tenant,
  TenantAdminInfo,
  TenantLicencia,
  TenantResource,
  TipoLicenciaOption,
} from '../domain/tenant.model';

/** Slots de imagen del tenant (cada uno mapea a `${slot}Url` + `${slot}File` del modelo). */
type ImageSlot =
  | 'logoLoginLight'
  | 'logoLoginDark'
  | 'logoHeaderLight'
  | 'logoHeaderDark'
  | 'favicon'
  | 'imagenFondoLoginLight'
  | 'imagenFondoLoginDark';

const IMAGE_SLOTS: ImageSlot[] = [
  'logoLoginLight',
  'logoLoginDark',
  'logoHeaderLight',
  'logoHeaderDark',
  'favicon',
  'imagenFondoLoginLight',
  'imagenFondoLoginDark',
];

const emptySlots = <T>(value: T): Record<ImageSlot, T> =>
  IMAGE_SLOTS.reduce((acc, s) => ({ ...acc, [s]: value }), {} as Record<ImageSlot, T>);

interface TenantFormModel {
  codigo: string;
  nombre: string;
  tituloWeb: string | null;
  hostName: string | null;
  activo: boolean;
  darkModeByDefault: boolean;
  tipoLayoutLogin: number;
  colorPrimarioLight: string;
  colorPrimarioDark: string;
  /** UI-only: true = usa CSS custom en vez del theme por colores. */
  customizaTheme: boolean;
  usuarioNombre: string;
  usuarioApellido: string;
  usuarioEmail: string;
  usuarioUserName: string;
  usuarioTelefono: string;
}

// TODO (Fase 4 - i18n): textos en español por ahora.
@Component({
  selector: 'tbi-tenant-edit',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FormField,
    MatDialogModule,
    MatButtonModule,
    MatIconModule,
    MatProgressBarModule,
    MatCheckboxModule,
    MatSlideToggleModule,
    MatTabsModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    TbiTextFieldComponent,
    TbiSelectComponent,
    TbiButtonComponent,
    TbiFileUploadComponent,
  ],
  templateUrl: './tenant-edit.component.html',
  styleUrl: './tenant-edit.component.scss',
})
export class TenantEditComponent extends EditComponentBase<Tenant, TenantFormModel> {
  private readonly service = inject(TenantService);
  private readonly notify = inject(NotificationService);
  private readonly router = inject(Router);
  protected readonly crud = this.service.crud;
  private loaded?: Tenant;

  protected readonly aplicaciones = signal<AplicacionOption[]>([]);
  protected readonly tiposLicencia = signal<TipoLicenciaOption[]>([]);
  protected readonly selectedAppIds = signal<ReadonlySet<number>>(new Set());
  protected readonly licenses = signal<TenantLicencia[]>([]);
  /** Admin(es) del tenant en edición (solo lectura; se traen del endpoint dedicado). */
  protected readonly adminUsuarios = signal<TenantAdminInfo[]>([]);

  protected readonly model = signal<TenantFormModel>({
    codigo: '',
    nombre: '',
    tituloWeb: null,
    hostName: null,
    activo: true,
    darkModeByDefault: false,
    tipoLayoutLogin: 0,
    colorPrimarioLight: '#009688',
    colorPrimarioDark: '#23C5BE',
    customizaTheme: false,
    usuarioNombre: '',
    usuarioApellido: '',
    usuarioEmail: '',
    usuarioUserName: '',
    usuarioTelefono: '',
  });

  // Admin (usuarioXxx) sólo aplica en alta: en edición se deshabilita (excluye sus validaciones del
  // submit). El código es la clave del tenant (nombra los archivos del storage) → inmutable en edición.
  // Longitudes espejan la DB (gen_Tenants: codigo/nombre 100). El teléfono no admite 0 inicial: la
  // API lo modela como número y un '011…' se corrompería en silencio ('011…' → 11…).
  protected readonly form = form(this.model, (path) => {
    required(path.codigo, { message: 'Requerido' });
    maxLength(path.codigo, 100, { message: 'Máximo 100 caracteres' });
    required(path.nombre, { message: 'Requerido' });
    maxLength(path.nombre, 100, { message: 'Máximo 100 caracteres' });
    disabled(path.codigo, { when: () => this.isEdit });
    required(path.usuarioNombre, { message: 'Requerido' });
    required(path.usuarioApellido, { message: 'Requerido' });
    required(path.usuarioEmail, { message: 'Requerido' });
    email(path.usuarioEmail, { message: 'Email inválido' });
    required(path.usuarioUserName, { message: 'Requerido' });
    maxLength(path.usuarioUserName, 50, { message: 'Máximo 50 caracteres' });
    pattern(path.usuarioTelefono, /^[1-9]\d{9,12}$/, {
      message: 'El teléfono debe tener entre 10 y 13 dígitos y no empezar con 0',
    });
    disabled(path.usuarioNombre, { when: () => this.isEdit });
    disabled(path.usuarioApellido, { when: () => this.isEdit });
    disabled(path.usuarioEmail, { when: () => this.isEdit });
    disabled(path.usuarioUserName, { when: () => this.isEdit });
    disabled(path.usuarioTelefono, { when: () => this.isEdit });
  });

  protected override invalidSubmitMessage =
    'Completá los campos obligatorios resaltados (revisá todas las pestañas).';

  /** No se puede agregar más licencias que tipos existentes (cada tipo va una sola vez). */
  protected readonly canAddLicense = computed(
    () => this.tiposLicencia().length > 0 && this.licenses().length < this.tiposLicencia().length,
  );

  /** Estado de archivos: recurso recién elegido (a subir) y URL actual (modo edición). */
  protected readonly fileResources = signal<Record<ImageSlot, TenantResource | null>>(
    emptySlots(null),
  );
  protected readonly fileUrls = signal<Record<ImageSlot, string | null>>(emptySlots(null));
  protected readonly cssResource = signal<TenantResource | null>(null);
  protected readonly cssUrl = signal<string | null>(null);

  protected readonly imageSlots = IMAGE_SLOTS;
  /** Slots agrupados para la pestaña Estilos (logos vs fondos del login). */
  protected readonly logoSlots: ImageSlot[] = [
    'logoLoginLight',
    'logoLoginDark',
    'logoHeaderLight',
    'logoHeaderDark',
    'favicon',
  ];
  protected readonly fondoSlots: ImageSlot[] = ['imagenFondoLoginLight', 'imagenFondoLoginDark'];
  protected readonly slotLabels: Record<ImageSlot, string> = {
    logoLoginLight: 'Logo login (claro)',
    logoLoginDark: 'Logo login (oscuro)',
    logoHeaderLight: 'Logo header (claro)',
    logoHeaderDark: 'Logo header (oscuro)',
    favicon: 'Favicon',
    imagenFondoLoginLight: 'Fondo login (claro)',
    imagenFondoLoginDark: 'Fondo login (oscuro)',
  };

  protected readonly layoutOptions: TbiSelectOption<number>[] = [
    { value: 0, label: 'Centrado' },
    { value: 1, label: 'Alineado a la derecha' },
    { value: 2, label: 'Alineado a la izquierda' },
  ];

  /**
   * Tipos disponibles por fila: excluye los ya elegidos en **otras** filas (no se puede agregar dos
   * veces el mismo tipo) y mantiene el tipo de la fila actual para que su selección siga visible.
   * Computed (no un método llamado por fila desde el template) para no recalcular en cada CD.
   */
  protected readonly availableTipoOptionsByRow = computed<TipoLicenciaOption[][]>(() => {
    const licenses = this.licenses();
    const tipos = this.tiposLicencia();
    return licenses.map((_, index) => {
      const usedByOthers = new Set(
        licenses.filter((_, i) => i !== index).map((l) => l.tipoLicenciaId),
      );
      return tipos.filter((t) => !usedByOthers.has(t.id));
    });
  });

  private readonly defaultTipoIds = computed(
    () =>
      new Set(
        this.tiposLicencia()
          .filter((t) => t.esDefaultParaNuevoTenant)
          .map((t) => t.id),
      ),
  );

  override ngOnInit(): void {
    super.ngOnInit();

    this.service.getAplicaciones().subscribe({
      next: (apps) => this.aplicaciones.set(apps),
      // el toast de error lo emite el errorInterceptor (global)
      error: () => undefined,
    });

    this.service.getTiposLicencia().subscribe({
      next: (tipos) => {
        this.tiposLicencia.set(tipos);
        // En alta, precargar las licencias default del nuevo tenant (una vez tenemos los tipos).
        if (!this.isEdit && this.licenses().length === 0) {
          this.licenses.set(
            tipos.filter((t) => t.esDefaultParaNuevoTenant).map((t) => this.newLicenseRow(t.id)),
          );
        }
      },
      // el toast de error lo emite el errorInterceptor (global)
      error: () => undefined,
    });
  }

  // ---- Aplicaciones -----------------------------------------------------------------------------
  protected toggleApp(aplicacionId: number, checked: boolean): void {
    this.selectedAppIds.update((current) => {
      const next = new Set(current);
      if (checked) {
        next.add(aplicacionId);
      } else {
        next.delete(aplicacionId);
      }
      return next;
    });
    this.dirtyExtra.set(true);
  }

  protected isAppChecked(aplicacionId: number): boolean {
    return this.selectedAppIds().has(aplicacionId);
  }

  // ---- Licencias --------------------------------------------------------------------------------
  protected addLicense(): void {
    if (!this.canAddLicense()) {
      return;
    }
    this.licenses.update((list) => [...list, this.newLicenseRow(0)]);
    this.dirtyExtra.set(true);
  }

  protected removeLicense(index: number): void {
    this.licenses.update((list) => list.filter((_, i) => i !== index));
    this.dirtyExtra.set(true);
  }

  /** Las licencias default del nuevo tenant no se pueden quitar ni cambiar de tipo. */
  protected isDefaultLicense(row: TenantLicencia): boolean {
    return this.defaultTipoIds().has(row.tipoLicenciaId);
  }

  protected onTipoChange(index: number, tipoLicenciaId: number): void {
    const dup = this.licenses().some((l, i) => i !== index && l.tipoLicenciaId === tipoLicenciaId);
    if (dup) {
      this.notify.error('Ese tipo de licencia ya está agregado.');
      this.patchLicense(index, { tipoLicenciaId: 0 });
      return;
    }
    this.patchLicense(index, { tipoLicenciaId });
  }

  protected onCantidadChange(index: number, value: string): void {
    this.patchLicense(index, { cantidad: Math.max(1, Number(value) || 1) });
  }

  protected onStartChange(index: number, value: string): void {
    // Misma lógica que el ABM original: al cambiar el inicio, el vencimiento pasa a inicio + 1 año
    // y se marca la fecha de modificación = hoy.
    this.patchLicense(index, {
      startDateTime: value,
      expireDateTime: plusOneYear(value),
      modifiedDateTime: todayLocalIso(),
    });
  }

  protected onExpireChange(index: number, value: string): void {
    this.patchLicense(index, { expireDateTime: value });
  }

  private patchLicense(index: number, patch: Partial<TenantLicencia>): void {
    this.licenses.update((list) => list.map((l, i) => (i === index ? { ...l, ...patch } : l)));
    this.dirtyExtra.set(true);
  }

  private newLicenseRow(tipoLicenciaId: number): TenantLicencia {
    const start = todayLocalIso();
    return {
      tipoLicenciaId,
      cantidad: 1,
      startDateTime: start,
      expireDateTime: plusOneYear(start),
      modifiedDateTime: start,
    };
  }

  // ---- Archivos (imágenes + CSS) ----------------------------------------------------------------
  protected onFile(slot: ImageSlot, resource: TbiFileResource): void {
    this.fileResources.update((r) => ({ ...r, [slot]: resource }));
    this.dirtyExtra.set(true);
  }

  protected onFileRemove(slot: ImageSlot): void {
    // Sin file y con URL en null → la API borra la imagen del storage.
    this.fileResources.update((r) => ({ ...r, [slot]: null }));
    this.fileUrls.update((u) => ({ ...u, [slot]: null }));
    this.dirtyExtra.set(true);
  }

  protected onCssFile(resource: TbiFileResource): void {
    this.cssResource.set(resource);
    this.cssUrl.set(resource.name);
    this.dirtyExtra.set(true);
  }

  protected onCssRemove(): void {
    this.cssResource.set(null);
    this.cssUrl.set(null);
    this.dirtyExtra.set(true);
  }

  // ---- Form ↔ entidad ---------------------------------------------------------------------------
  protected toEntity(): Tenant {
    const v = this.model();
    const files = this.fileResources();
    const urls = this.fileUrls();
    const customiza = v.customizaTheme;
    const tenantId = this.loaded?.id;

    return {
      ...this.loaded,
      id: tenantId,
      codigo: v.codigo,
      nombre: v.nombre,
      tituloWeb: v.tituloWeb?.trim() || null,
      hostName: v.hostName?.trim() || null,
      activo: v.activo,

      colorPrimarioLight: v.colorPrimarioLight,
      colorPrimarioDark: v.colorPrimarioDark,
      darkModeByDefault: v.darkModeByDefault,
      tipoLayoutLogin: Number(v.tipoLayoutLogin),

      logoLoginLightUrl: urls.logoLoginLight,
      logoLoginDarkUrl: urls.logoLoginDark,
      logoHeaderLightUrl: urls.logoHeaderLight,
      logoHeaderDarkUrl: urls.logoHeaderDark,
      faviconUrl: urls.favicon,
      imagenFondoLoginLightUrl: urls.imagenFondoLoginLight,
      imagenFondoLoginDarkUrl: urls.imagenFondoLoginDark,

      logoLoginLightFile: files.logoLoginLight,
      logoLoginDarkFile: files.logoLoginDark,
      logoHeaderLightFile: files.logoHeaderLight,
      logoHeaderDarkFile: files.logoHeaderDark,
      faviconFile: files.favicon,
      imagenFondoLoginLightFile: files.imagenFondoLoginLight,
      imagenFondoLoginDarkFile: files.imagenFondoLoginDark,

      // CSS custom: si no se customiza, va en null → la API regenera el theme por colores.
      styleSheetCssUrl: customiza ? this.cssUrl() : null,
      styleSheetFile: customiza ? (this.cssResource()?.base64 ?? null) : null,

      tenantAplicaciones: [...this.selectedAppIds()].map((aplicacionId) => ({
        aplicacionId,
        tenantId,
      })),
      tenantLicenses: this.licenses(),

      // Sólo en alta: la API crea el usuario admin y le envía el mail de confirmación.
      usuario: this.isEdit
        ? undefined
        : {
            nombre: v.usuarioNombre,
            apellido: v.usuarioApellido,
            email: v.usuarioEmail,
            userName: v.usuarioUserName,
            telefono: v.usuarioTelefono ? Number(v.usuarioTelefono) : null,
          },

      // Sólo respuesta: no se reenvían (la API los preserva).
      hasError: undefined,
      errorDescription: undefined,
    };
  }

  protected patchForm(entity: Tenant): void {
    this.loaded = entity;

    // Admin(es) del tenant: endpoint dedicado (solo lectura). No viene en el detalle ni se reenvía.
    if (entity.id != null) {
      this.service.getAdministradores(entity.id).subscribe({
        next: (admins) => this.adminUsuarios.set(admins),
        // el toast de error lo emite el errorInterceptor (global)
        error: () => undefined,
      });
    }

    this.model.update((m) => ({
      ...m,
      codigo: entity.codigo,
      nombre: entity.nombre,
      tituloWeb: entity.tituloWeb ?? null,
      hostName: entity.hostName ?? null,
      activo: entity.activo ?? true,
      darkModeByDefault: entity.darkModeByDefault ?? false,
      tipoLayoutLogin: entity.tipoLayoutLogin ?? 0,
      colorPrimarioLight: entity.colorPrimarioLight ?? '#009688',
      colorPrimarioDark: entity.colorPrimarioDark ?? '#23C5BE',
      customizaTheme: !!entity.styleSheetCssUrl,
    }));

    this.selectedAppIds.set(new Set((entity.tenantAplicaciones ?? []).map((a) => a.aplicacionId)));
    this.licenses.set(
      (entity.tenantLicenses ?? []).map((l) => ({
        ...l,
        startDateTime: toDateInput(l.startDateTime),
        expireDateTime: toDateInput(l.expireDateTime),
        modifiedDateTime: toDateInput(l.modifiedDateTime),
      })),
    );

    this.fileUrls.set({
      logoLoginLight: entity.logoLoginLightUrl ?? null,
      logoLoginDark: entity.logoLoginDarkUrl ?? null,
      logoHeaderLight: entity.logoHeaderLightUrl ?? null,
      logoHeaderDark: entity.logoHeaderDarkUrl ?? null,
      favicon: entity.faviconUrl ?? null,
      imagenFondoLoginLight: entity.imagenFondoLoginLightUrl ?? null,
      imagenFondoLoginDark: entity.imagenFondoLoginDarkUrl ?? null,
    });
    this.fileResources.set(emptySlots(null));
    this.cssUrl.set(entity.styleSheetCssUrl ?? null);
    this.cssResource.set(null);
  }
}
