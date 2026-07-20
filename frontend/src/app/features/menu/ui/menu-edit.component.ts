import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
  signal,
} from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import {
  FormField,
  disabled,
  form,
  maxLength,
  maxLengthError,
  pattern,
  required,
  validate,
} from '@angular/forms/signals';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule } from '@angular/material/dialog';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { catchError, forkJoin, of } from 'rxjs';
import { EditComponentBase } from '../../../shared/forms/edit-component-base';
import { TbiButtonComponent } from '../../../shared/ui/tbi-button/tbi-button.component';
import {
  TbiSelectComponent,
  TbiSelectOption,
} from '../../../shared/ui/tbi-select/tbi-select.component';
import { TbiTextFieldComponent } from '../../../shared/ui/tbi-text-field/tbi-text-field.component';
import { lookupResource } from '../../../shared/util/lookup-resource';
import { MenusService } from '../data/menus.service';
import { Menu } from '../domain/menu.model';

/** `orden` se edita como texto (input numérico con pattern); se convierte a número en `toEntity`. */
interface MenuFormModel {
  aplicacionId: number | null;
  nombre: string;
  codigo: string;
  routePath: string | null;
  orden: string;
  imagenWeb: string | null;
  menuPadreId: number | null;
  permisoId: number | null;
  visibleSideMenu: boolean;
  visibleDash: boolean;
  estado: boolean;
}

// TODO (Fase 4 - i18n): textos en español por ahora.
@Component({
  selector: 'tbi-menu-edit',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FormField,
    MatDialogModule,
    MatButtonModule,
    MatProgressBarModule,
    MatSlideToggleModule,
    TbiTextFieldComponent,
    TbiSelectComponent,
    TbiButtonComponent,
  ],
  templateUrl: './menu-edit.component.html',
  styleUrl: './menu-edit.component.scss',
})
export class MenuEditComponent extends EditComponentBase<Menu, MenuFormModel> {
  private readonly service = inject(MenusService);
  protected readonly crud = this.service.crud;
  private loaded?: Menu;

  private readonly aplicacionesResource = lookupResource(() => this.service.getAplicaciones(), []);
  protected readonly aplicacionOptions = computed<TbiSelectOption<number>[]>(() =>
    (this.aplicacionesResource.value() ?? []).map((a) => ({ value: a.id, label: a.nombre })),
  );
  /** Opciones que dependen de la aplicación elegida (incluyen un "(Ninguno)" porque son opcionales). */
  protected readonly permisoOptions = signal<TbiSelectOption<number | null>[]>([]);
  protected readonly menuPadreOptions = signal<TbiSelectOption<number | null>[]>([]);

  protected readonly model = signal<MenuFormModel>({
    aplicacionId: null,
    nombre: '',
    codigo: '',
    routePath: null,
    orden: '',
    imagenWeb: null,
    menuPadreId: null,
    permisoId: null,
    visibleSideMenu: true,
    visibleDash: false,
    estado: true,
  });

  // El permiso y el menú padre dependen de la aplicación elegida: deshabilitados sin aplicación
  // (declarativo, ya no hace falta enable/disable imperativo). `aplicacionId` es requerido (criterio
  // del original); `orden` se exige entero ≥ 1 (la API lo valida `NotEmpty`).
  // Longitudes espejan la DB (gen_Menus: nombre/codigo 50, imagenWeb 100). Ojo: el MenuDtoValidator
  // de la API permite 100 pero la columna es 50 — entre 51 y 100 chars explotaría en SQL.
  protected readonly form = form(this.model, (path) => {
    required(path.aplicacionId, { message: 'Requerido' });
    required(path.nombre, { message: 'Requerido' });
    maxLength(path.nombre, 50, { message: 'Máximo 50 caracteres' });
    required(path.codigo, { message: 'Requerido' });
    maxLength(path.codigo, 50, { message: 'Máximo 50 caracteres' });
    // `imagenWeb` es nullable (`string | null`): `maxLength()` solo acepta valores no-nulos, de ahí
    // el `validate` manual con el mismo error que produciría.
    validate(path.imagenWeb, ({ value }) => {
      const v = value();
      return v != null && v.length > 100
        ? maxLengthError(100, { message: 'Máximo 100 caracteres' })
        : undefined;
    });
    required(path.orden, { message: 'Requerido' });
    pattern(path.orden, /^[1-9]\d*$/, { message: 'Entero mayor a 0' });
    disabled(path.permisoId, { when: ({ valueOf }) => valueOf(path.aplicacionId) == null });
    disabled(path.menuPadreId, { when: ({ valueOf }) => valueOf(path.aplicacionId) == null });
  });

  /** Refetchea sola cuando cambia la aplicación elegida (cancela el pedido en vuelo anterior). */
  private readonly dependientesResource = rxResource({
    params: () => this.form.aplicacionId().value(),
    stream: ({ params: aplicacionId }) =>
      aplicacionId == null
        ? of(null)
        : forkJoin({
            permisos: this.service.getPermisosDeAplicacion(aplicacionId),
            menus: this.service.getMenusDeAplicacion(aplicacionId),
          }).pipe(
            // el toast de error lo emite el errorInterceptor (global)
            catchError(() => of(undefined)),
          ),
  });

  /** El patch inicial (edición) arma esta aplicación ANTES de volcar el modelo: cuando el effect
   * corra y la aplicación ACTUAL siga siendo esta, no limpia permiso/menú padre — se compara contra
   * el valor vigente en vez de un booleano "consumido" (un booleano es frágil si dos cambios de
   * aplicación ocurren antes de que el effect llegue a correr para el primero: se coalescen en una
   * sola ejecución y el booleano se consumiría para el cambio equivocado). `undefined` = sin
   * supresión armada (distinto de `null`, que es "sin aplicación elegida", un id válido). */
  private suppressClearForAplicacionId: number | null | undefined = undefined;

  constructor() {
    super();
    // Permiso y menú padre dependen de la aplicación: al cambiarla se recargan sus listas y se
    // limpia lo elegido (salvo el patch inicial, ver `suppressClearForAplicacionId`).
    effect(() => {
      const currentAplicacionId = this.form.aplicacionId().value();
      const result = this.dependientesResource.value();
      if (result === undefined) {
        return;
      }
      const suppress = this.suppressClearForAplicacionId === currentAplicacionId;
      this.suppressClearForAplicacionId = undefined;
      if (!suppress) {
        this.model.update((m) => ({ ...m, permisoId: null, menuPadreId: null }));
      }
      if (result === null) {
        this.permisoOptions.set([]);
        this.menuPadreOptions.set([]);
        return;
      }
      this.permisoOptions.set([
        { value: null, label: '(Ninguno)' },
        ...result.permisos.map((p) => ({ value: p.id, label: p.nombre })),
      ]);
      this.menuPadreOptions.set([
        { value: null, label: '(Sin padre)' },
        // Un menú no puede ser su propio padre.
        ...result.menus
          .filter((m) => m.id !== this.loaded?.id)
          .map((m) => ({ value: m.id, label: `${m.codigo} — ${m.nombre}` })),
      ]);
    });
  }

  protected toEntity(): Menu {
    const value = this.model();
    return {
      ...this.loaded,
      aplicacionId: value.aplicacionId,
      nombre: value.nombre,
      codigo: value.codigo,
      routePath: value.routePath?.trim() || null,
      orden: Number(value.orden),
      imagenWeb: value.imagenWeb?.trim() || null,
      menuPadreId: value.menuPadreId,
      permisoId: value.permisoId,
      visibleSideMenu: value.visibleSideMenu,
      visibleDash: value.visibleDash,
      estado: value.estado,
      // Sólo respuesta; no se reenvían.
      permisoNombre: undefined,
      aplicacionDescripcion: undefined,
    };
  }

  protected patchForm(entity: Menu): void {
    this.loaded = entity;
    // Arma la supresión para esta aplicación ANTES de volcar el modelo: el effect no la trata como
    // cambio del usuario (no poda permiso/menú padre) — `dependientesResource` refetchea sola.
    this.suppressClearForAplicacionId = entity.aplicacionId ?? null;
    this.model.set({
      aplicacionId: entity.aplicacionId ?? null,
      nombre: entity.nombre,
      codigo: entity.codigo,
      routePath: entity.routePath ?? null,
      orden: entity.orden != null ? String(entity.orden) : '',
      imagenWeb: entity.imagenWeb ?? null,
      menuPadreId: entity.menuPadreId ?? null,
      permisoId: entity.permisoId ?? null,
      visibleSideMenu: entity.visibleSideMenu ?? true,
      visibleDash: entity.visibleDash ?? false,
      estado: entity.estado ?? true,
    });
  }
}
