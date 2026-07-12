import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormField, form, maxLength, required } from '@angular/forms/signals';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatDialogModule } from '@angular/material/dialog';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { Observable, of } from 'rxjs';
import { AuthStore } from '../../../core/auth';
import { EditComponentBase } from '../../../shared/forms/edit-component-base';
import { TbiButtonComponent } from '../../../shared/ui/tbi-button/tbi-button.component';
import { TbiTextFieldComponent } from '../../../shared/ui/tbi-text-field/tbi-text-field.component';
import {
  TbiTreeNode,
  TbiTreeSelectComponent,
} from '../../../shared/ui/tbi-tree-select/tbi-tree-select.component';
import { RolesService } from '../data/roles.service';
import { Rol } from '../domain/rol.model';

interface RolFormModel {
  descripcion: string;
  /** Ids de los permisos asignados (los gestiona el tbi-tree-select; no es requerido). */
  permisos: number[];
  /** Flag reservado a root; se persiste por endpoint aparte (ver afterSave), no en el update. */
  esDefaultParaNuevoTenant: boolean;
}

// TODO (Fase 4 - i18n): textos en español por ahora.
@Component({
  selector: 'tbi-rol-edit',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FormField,
    MatDialogModule,
    MatButtonModule,
    MatCheckboxModule,
    MatProgressBarModule,
    TbiTextFieldComponent,
    TbiTreeSelectComponent,
    TbiButtonComponent,
  ],
  templateUrl: './rol-edit.component.html',
  styleUrl: './rol-edit.component.scss',
})
export class RolEditComponent extends EditComponentBase<Rol, RolFormModel> {
  private readonly service = inject(RolesService);
  protected readonly crud = this.service.crud;
  private loaded?: Rol;

  /** Solo root puede editar `esDefaultParaNuevoTenant` (la API lo valida; acá es UX). */
  protected readonly isRoot = inject(AuthStore).isRoot;
  protected readonly permisosTree = signal<TbiTreeNode[]>([]);
  /** Valor original del flag, para detectar si root lo cambió y disparar el endpoint. */
  private originalEsDefault = false;

  protected readonly model = signal<RolFormModel>({
    descripcion: '',
    permisos: [],
    esDefaultParaNuevoTenant: false,
  });

  protected readonly form = form(this.model, (path) => {
    required(path.descripcion, { message: 'Requerido' });
    maxLength(path.descripcion, 100, { message: 'Máximo 100 caracteres' });
  });

  override ngOnInit(): void {
    super.ngOnInit();
    this.service.getPermisosTree().subscribe((tree) => this.permisosTree.set(tree));
  }

  protected toEntity(): Rol {
    // El request manda los permisos como ids (`permisoIds`); la API sincroniza la colección.
    // `rolPermisos` (sólo respuesta) se omite del payload; `esDefaultParaNuevoTenant` no viaja
    // en el update (lo persiste el endpoint dedicado vía afterSave).
    const value = this.model();
    return {
      ...this.loaded,
      descripcion: value.descripcion,
      permisoIds: value.permisos,
      rolPermisos: undefined,
      // Explícito: que el spread de `loaded` no mande un valor stale si el input DTO lo incorporase.
      esDefaultParaNuevoTenant: undefined,
    };
  }

  protected patchForm(entity: Rol): void {
    this.loaded = entity;
    this.originalEsDefault = entity.esDefaultParaNuevoTenant ?? false;
    this.model.set({
      descripcion: entity.descripcion,
      permisos: (entity.rolPermisos ?? []).map((rp) => rp.permisoId),
      esDefaultParaNuevoTenant: this.originalEsDefault,
    });
  }

  /**
   * Tras el update normal, si el usuario es root y cambió el flag, lo persiste por el endpoint
   * dedicado `roles/{id}/set-default-tenant` (usa el id del rol ya guardado: sirve para alta y
   * edición). Si no es root o no cambió, no hace nada.
   */
  protected override afterSave(saved: Rol): Observable<unknown> {
    const value = this.model().esDefaultParaNuevoTenant;
    if (!this.isRoot() || saved.id == null || value === this.originalEsDefault) {
      return of(saved);
    }
    return this.service.setDefaultTenant(saved.id, value);
  }
}
