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
import { toggleInSet } from '../../../shared/util/collections';
import { TiposLicenciaService } from '../data/tipos-licencia.service';
import { RolOption, TipoLicencia } from '../domain/tipo-licencia.model';

interface TipoLicenciaFormModel {
  codigoTipoLicencia: string;
  descripcion: string;
  /** Flag reservado a root; se persiste por endpoint aparte (ver afterSave), no en el update. */
  esDefaultParaNuevoTenant: boolean;
}

// TODO (Fase 4 - i18n): textos en español por ahora.
@Component({
  selector: 'tbi-tipos-licencia-edit',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FormField,
    MatDialogModule,
    MatButtonModule,
    MatCheckboxModule,
    MatProgressBarModule,
    TbiTextFieldComponent,
    TbiButtonComponent,
  ],
  templateUrl: './tipos-licencia-edit.component.html',
  styleUrl: './tipos-licencia-edit.component.scss',
})
export class TiposLicenciaEditComponent extends EditComponentBase<
  TipoLicencia,
  TipoLicenciaFormModel
> {
  private readonly service = inject(TiposLicenciaService);
  protected readonly crud = this.service.crud;
  private loaded?: TipoLicencia;

  /** Solo root puede editar `esDefaultParaNuevoTenant` (la API lo valida; acá es UX). */
  protected readonly isRoot = inject(AuthStore).isRoot;
  protected readonly roles = signal<RolOption[]>([]);
  protected readonly selectedRoleIds = signal<ReadonlySet<number>>(new Set());
  /** Valor original del flag, para detectar si root lo cambió y disparar el endpoint. */
  private originalEsDefault = false;

  protected readonly model = signal<TipoLicenciaFormModel>({
    codigoTipoLicencia: '',
    descripcion: '',
    esDefaultParaNuevoTenant: false,
  });

  protected readonly form = form(this.model, (path) => {
    required(path.codigoTipoLicencia, { message: 'Requerido' });
    required(path.descripcion, { message: 'Requerido' });
    maxLength(path.descripcion, 100, { message: 'Máximo 100 caracteres' });
  });

  override ngOnInit(): void {
    super.ngOnInit();
    this.service.getRoles().subscribe((roles) => this.roles.set(roles));
  }

  protected toggleRole(rolId: number, checked: boolean): void {
    this.selectedRoleIds.update((current) => toggleInSet(current, rolId, checked));
  }

  protected toEntity(): TipoLicencia {
    // Los roles viajan como ids (`rolIds`); la API sincroniza la colección.
    // `tipoLicenciaRoles` (sólo respuesta) se omite del payload; `esDefaultParaNuevoTenant` no
    // viaja en el update (lo persiste el endpoint dedicado vía afterSave).
    const value = this.model();
    return {
      ...this.loaded,
      codigoTipoLicencia: value.codigoTipoLicencia,
      descripcion: value.descripcion,
      rolIds: [...this.selectedRoleIds()],
      tipoLicenciaRoles: undefined,
      // Explícito: que el spread de `loaded` no mande un valor stale si el input DTO lo incorpora.
      esDefaultParaNuevoTenant: undefined,
    };
  }

  protected patchForm(entity: TipoLicencia): void {
    this.loaded = entity;
    this.originalEsDefault = entity.esDefaultParaNuevoTenant ?? false;
    this.model.set({
      codigoTipoLicencia: entity.codigoTipoLicencia,
      descripcion: entity.descripcion,
      esDefaultParaNuevoTenant: this.originalEsDefault,
    });
    this.selectedRoleIds.set(new Set((entity.tipoLicenciaRoles ?? []).map((r) => r.rolId)));
  }

  /**
   * Tras el update normal, si el usuario es root y cambió el flag, lo persiste por el endpoint
   * dedicado `tipos-licencia/{id}/set-default-tenant` (usa el id ya guardado: sirve para alta y
   * edición). Si no es root o no cambió, no hace nada.
   */
  protected override afterSave(saved: TipoLicencia): Observable<unknown> {
    const value = this.model().esDefaultParaNuevoTenant;
    if (!this.isRoot() || saved.id == null || value === this.originalEsDefault) {
      return of(saved);
    }
    return this.service.setDefaultTenant(saved.id, value);
  }
}
