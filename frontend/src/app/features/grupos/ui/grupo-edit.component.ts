import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import {
  FormField,
  form,
  maxLength,
  required,
  requiredError,
  validate,
} from '@angular/forms/signals';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatChipsModule } from '@angular/material/chips';
import { MatDialogModule } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { Observable, tap } from 'rxjs';
import { EditComponentBase } from '../../../shared/forms/edit-component-base';
import { TbiButtonComponent } from '../../../shared/ui/tbi-button/tbi-button.component';
import {
  TbiSearchSelectComponent,
  TbiSearchSelectItem,
} from '../../../shared/ui/tbi-search-select/tbi-search-select.component';
import { TbiTextFieldComponent } from '../../../shared/ui/tbi-text-field/tbi-text-field.component';
import { toggleInSet } from '../../../shared/util/collections';
import { GruposService, UsuarioBusqueda } from '../data/grupos.service';
import { Grupo, RolOption, UsuarioGrupo } from '../domain/grupo.model';

interface GrupoFormModel {
  nombre: string;
  /** Miembros como items {id,label} (los gestiona el tbi-search-select). */
  usuarios: TbiSearchSelectItem[];
}

// TODO (Fase 4 - i18n): textos en español por ahora.
@Component({
  selector: 'tbi-grupo-edit',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FormField,
    MatDialogModule,
    MatButtonModule,
    MatCheckboxModule,
    MatChipsModule,
    MatIconModule,
    MatProgressBarModule,
    TbiTextFieldComponent,
    TbiSearchSelectComponent,
    TbiButtonComponent,
  ],
  templateUrl: './grupo-edit.component.html',
  styleUrl: './grupo-edit.component.scss',
})
export class GrupoEditComponent extends EditComponentBase<Grupo, GrupoFormModel> {
  private readonly service = inject(GruposService);
  protected readonly crud = this.service.crud;
  private loaded?: Grupo;

  /** Roles disponibles (lookup) y los que otorga el grupo (checkboxes). El grupo los hereda a sus miembros. */
  protected readonly roles = signal<RolOption[]>([]);
  protected readonly selectedRolIds = signal<ReadonlySet<number>>(new Set());

  protected readonly model = signal<GrupoFormModel>({
    nombre: '',
    usuarios: [],
  });

  protected readonly form = form(this.model, (path) => {
    required(path.nombre, { message: 'Requerido' });
    maxLength(path.nombre, 100, { message: 'Máximo 100 caracteres' });
    // Al menos un miembro (como el ABM original). `required()` de Signal Forms NO trata `[]` como
    // vacío (solo ''/false/null/NaN), a diferencia de Validators.required → validación explícita.
    validate(path.usuarios, ({ value }) =>
      value().length === 0 ? requiredError({ message: 'Agregá al menos un miembro' }) : undefined,
    );
  });

  /**
   * Licencia activa conocida por usuario (`usuarioId → tipoLicenciaId | null`). Se va llenando con lo
   * que devuelve el buscador y con los miembros cargados en edición; alimenta `mezclaLicencias`.
   */
  private readonly licenciaPorUsuario = signal<ReadonlyMap<number, number | null>>(new Map());

  /**
   * `true` si los miembros seleccionados tienen **≥2 licencias activas distintas** (§11.2). Cada
   * usuario solo recibe los roles del grupo que su licencia incluye, así que mezclar es inofensivo a
   * nivel seguridad (el tope de licencia capa) pero confuso → alerta blanda. Ignora miembros sin
   * licencia conocida/activa. Lee los miembros directo del modelo (reactivo, sin toSignal).
   */
  protected readonly mezclaLicencias = computed<boolean>(() => {
    const mapa = this.licenciaPorUsuario();
    const licencias = new Set<number>();
    for (const m of this.model().usuarios) {
      const lic = mapa.get(m.id);
      if (lic != null) {
        licencias.add(lic);
      }
    }
    return licencias.size >= 2;
  });

  /** Búsqueda server-side de usuarios (typeahead); de paso registra la licencia de cada resultado. */
  protected readonly searchUsuarios = (term: string): Observable<UsuarioBusqueda[]> =>
    this.service.searchUsuarios(term).pipe(tap((usuarios) => this.registrarLicencias(usuarios)));

  override ngOnInit(): void {
    super.ngOnInit();
    this.service.getRoles().subscribe((roles) => this.roles.set(roles));
  }

  /** Mergea en el mapa la licencia de cada usuario (de búsqueda o de los miembros cargados). */
  private registrarLicencias(
    usuarios: { id: number; tipoLicenciaActivaId: number | null }[],
  ): void {
    if (usuarios.length === 0) {
      return;
    }
    this.licenciaPorUsuario.update((current) => {
      const next = new Map(current);
      for (const u of usuarios) {
        next.set(u.id, u.tipoLicenciaActivaId);
      }
      return next;
    });
  }

  protected toggleRol(rolId: number, checked: boolean): void {
    this.selectedRolIds.update((current) => toggleInSet(current, rolId, checked));
  }

  protected toEntity(): Grupo {
    // Miembros como `usuarioIds` y roles como `rolIds` (la API sincroniza ambas colecciones).
    // `usuarioGrupos` / `grupoRoles` (sólo respuesta) se omiten del payload.
    const value = this.model();
    return {
      ...this.loaded,
      nombre: value.nombre,
      usuarioIds: value.usuarios.map((u) => u.id),
      rolIds: [...this.selectedRolIds()],
      usuarioGrupos: undefined,
      grupoRoles: undefined,
    };
  }

  protected patchForm(entity: Grupo): void {
    this.loaded = entity;
    // Registrar la licencia de los miembros cargados antes de setear el form (así la alerta de mezcla
    // ya está bien calculada al abrir un grupo existente, sin tener que re-buscar a cada miembro).
    this.registrarLicencias(
      (entity.usuarioGrupos ?? []).map((ug) => ({
        id: ug.usuarioId,
        tipoLicenciaActivaId: ug.usuarioTipoLicenciaActivaId ?? null,
      })),
    );
    this.model.set({
      nombre: entity.nombre,
      usuarios: (entity.usuarioGrupos ?? []).map((ug) => ({
        id: ug.usuarioId,
        label: miembroLabel(ug),
      })),
    });
    this.selectedRolIds.set(new Set((entity.grupoRoles ?? []).map((gr) => gr.rolId)));
  }
}

/** Etiqueta visible de un miembro a partir de los datos de display que trae el detalle (getById). */
function miembroLabel(ug: UsuarioGrupo): string {
  const nombre = `${ug.usuarioNombre ?? ''} ${ug.usuarioApellido ?? ''}`.trim();
  if (nombre && ug.usuarioUserName) {
    return `${nombre} (${ug.usuarioUserName})`;
  }
  return nombre || ug.usuarioUserName || `#${ug.usuarioId}`;
}
