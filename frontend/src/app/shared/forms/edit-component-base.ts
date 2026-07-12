import {
  DestroyRef,
  Directive,
  OnInit,
  WritableSignal,
  computed,
  inject,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FieldTree } from '@angular/forms/signals';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { Observable, map, of, switchMap } from 'rxjs';
import { CrudClient } from '../../core/http';
import { ApiError, errorMessages } from '../../core/models';

/** Entidad editable (tiene id opcional: ausente = alta). */
export interface EditableEntity {
  id?: number | string;
}

/** Data que recibe el diálogo de edición. */
export interface EditDialogData {
  id?: number | string;
}

/**
 * Base para componentes de edición/alta en diálogo (Signal Forms). Maneja: carga de la entidad en
 * modo edición, submit (save) y cierre del diálogo, con estado en signals
 * (`loading`/`saving`/`errors`).
 *
 * La subclase provee: `model` (signal con el modelo del form — `TModel` puede tener campos UI-only
 * que no viajan a la entidad), `form` (`form(this.model, schemaFn)` de `@angular/forms/signals`),
 * `crud` (`injectCrudClient<T>('Entity')`), `toEntity()` (modelo → entidad) y `patchForm()`
 * (entidad → modelo). Se abre con `MatDialog.open(MiEditComponent, { data: { id } })`.
 *
 * En el template: `<form novalidate (submit)="$event.preventDefault(); submit()">` (el `novalidate`
 * es necesario porque ya no está el `FormGroupDirective` que lo agregaba) y `[formField]="form.x"`
 * en cada control (los `tbi-*` funcionan por el bridge CVA del directive `FormField`).
 */
@Directive()
export abstract class EditComponentBase<T extends EditableEntity, TModel = T> implements OnInit {
  protected abstract readonly model: WritableSignal<TModel>;
  protected abstract readonly form: FieldTree<TModel>;
  protected abstract readonly crud: CrudClient<T>;
  /** Construye la entidad a guardar a partir del modelo del form. */
  protected abstract toEntity(): T;
  /** Vuelca la entidad cargada al modelo del form (modo edición). */
  protected abstract patchForm(entity: T): void;

  /**
   * Hook opcional posterior al guardado, antes de cerrar el diálogo. La subclase lo overridea para
   * encadenar una operación secundaria sobre la entidad ya guardada (ej. un endpoint dedicado para
   * un campo con reglas propias). Por defecto no hace nada. Si devuelve un observable, el diálogo
   * se cierra cuando éste completa. Si falla, el diálogo queda abierto con un mensaje que aclara
   * que los datos principales SÍ se guardaron, y el reintento del submit corre como update (no
   * vuelve a crear la entidad).
   */
  protected afterSave(saved: T): Observable<unknown> {
    return of(saved);
  }

  protected readonly dialogRef = inject<MatDialogRef<unknown, T>>(MatDialogRef);
  protected readonly destroyRef = inject(DestroyRef);
  private readonly data = inject<EditDialogData | null>(MAT_DIALOG_DATA, { optional: true });

  protected readonly loading = signal(false);
  protected readonly saving = signal(false);
  protected readonly errors = signal<string[]>([]);
  protected readonly isEdit = (this.data?.id ?? null) !== null;

  /** Mensaje del submit inválido. Las subclases con pestañas pueden agregar "revisá todas las pestañas". */
  protected invalidSubmitMessage = 'Completá los campos obligatorios resaltados.';

  /** Cambios fuera del form (colecciones/archivos manejados con signals sueltos); la subclase lo
   * marca en sus handlers si los tiene. */
  protected readonly dirtyExtra = signal(false);

  /**
   * Hubo cambios del usuario. `form().dirty()` de Signal Forms sólo se marca con interacción real
   * (el `model.set` de la carga NO lo marca — misma semántica que el `dirty` del reactive form
   * anterior), combinado con los cambios del estado por signals (`dirtyExtra`).
   */
  protected readonly dirty = computed(() => this.form().dirty() || this.dirtyExtra());

  /** Entidad ya creada por un submit anterior cuyo `afterSave` falló: el reintento debe ser update. */
  private savedPendingAfterSave?: T;

  ngOnInit(): void {
    const id = this.data?.id;
    if (id == null) {
      return;
    }
    this.loading.set(true);
    this.crud
      .getById(id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (entity) => {
          this.patchForm(entity);
          this.loading.set(false);
        },
        error: (error: ApiError) => {
          this.errors.set(errorMessages(error));
          this.loading.set(false);
        },
      });
  }

  protected submit(): void {
    const state = this.form();
    if (state.invalid()) {
      // Evita el "submit mudo": marca los campos tocados (dispara los errores de cada uno) y
      // muestra un mensaje.
      state.markAsTouched();
      this.errors.set([this.invalidSubmitMessage]);
      return;
    }
    this.saving.set(true);
    this.errors.set([]);
    // Si un submit anterior ya creó la entidad (save ok + afterSave fallido), reintentar como update.
    let entity = this.toEntity();
    if (entity.id == null && this.savedPendingAfterSave?.id != null) {
      entity = { ...entity, id: this.savedPendingAfterSave.id };
    }
    let savedThisSubmit: T | undefined;
    this.crud
      .save(entity)
      .pipe(
        switchMap((saved) => {
          savedThisSubmit = saved;
          return this.afterSave(saved).pipe(map(() => saved));
        }),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (saved) => {
          this.savedPendingAfterSave = undefined;
          this.saving.set(false);
          this.dialogRef.close(saved);
        },
        error: (error: ApiError) => {
          const messages = errorMessages(error);
          if (savedThisSubmit) {
            this.savedPendingAfterSave = savedThisSubmit;
            this.errors.set([
              'Los datos se guardaron, pero falló una operación posterior. Reintentá para completarla.',
              ...messages,
            ]);
          } else {
            this.errors.set(messages);
          }
          this.saving.set(false);
        },
      });
  }

  protected cancel(): void {
    this.dialogRef.close();
  }
}
