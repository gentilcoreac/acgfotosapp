import {
  DestroyRef,
  Directive,
  WritableSignal,
  computed,
  effect,
  inject,
  signal,
} from '@angular/core';
import { rxResource, takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FieldTree } from '@angular/forms/signals';
import { Observable, map, of, switchMap } from 'rxjs';
import { CrudClient } from '../../core/http';
import { ApiError, errorMessages } from '../../core/models';

/** Entidad editable (tiene id opcional: ausente = alta). */
export interface EditableEntity {
  id?: number | string;
}

/**
 * Base genérica de formularios de alta/edición (Signal Forms), independiente de dónde se presenta el
 * resultado. Maneja: carga de la entidad en modo edición, submit (save), y el estado en signals
 * (`loading`/`saving`/`errors`).
 *
 * La subclase concreta resuelve los 3 puntos que dependen de la presentación —
 * `EditComponentBase` para diálogo (`MatDialogRef`), `EditPageComponentBase` para página con ruta
 * propia (`Router`/`ActivatedRoute`) — además de lo ya requerido por el form: `model`, `form`,
 * `crud`, `toEntity()`, `patchForm()`.
 *
 * `resolveId()` se invoca desde un field initializer de ESTA clase, que corre durante el `super()`
 * de la subclase — sus propios campos (ej. `dialogRef`/`route`) todavía no existen en ese momento.
 * Por eso las implementaciones de `resolveId()` deben resolver sus dependencias con `inject(...)`
 * directo adentro del método, no leer un campo ya guardado de la subclase.
 */
@Directive()
export abstract class EditFormBase<T extends EditableEntity, TModel = T> {
  protected abstract readonly model: WritableSignal<TModel>;
  protected abstract readonly form: FieldTree<TModel>;
  protected abstract readonly crud: CrudClient<T>;
  /** Construye la entidad a guardar a partir del modelo del form. */
  protected abstract toEntity(): T;
  /** Vuelca la entidad cargada al modelo del form (modo edición). */
  protected abstract patchForm(entity: T): void;

  /** De dónde sale el id en modo edición (`undefined` = alta). Ver nota de la clase sobre `inject`. */
  protected abstract resolveId(): number | string | undefined;
  /** Qué hacer al terminar de guardar con éxito (cerrar diálogo / navegar). */
  protected abstract onSaved(saved: T): void;
  /** Qué hacer al cancelar (cerrar diálogo / volver). */
  protected abstract onCancel(): void;

  /**
   * Hook opcional posterior al guardado. La subclase concreta lo overridea para encadenar una
   * operación secundaria sobre la entidad ya guardada (ej. un endpoint dedicado para un campo con
   * reglas propias). Por defecto no hace nada. Si falla, el form queda abierto con un mensaje que
   * aclara que los datos principales SÍ se guardaron, y el reintento del submit corre como update
   * (no vuelve a crear la entidad).
   */
  protected afterSave(saved: T): Observable<unknown> {
    return of(saved);
  }

  protected readonly destroyRef = inject(DestroyRef);

  protected readonly loading = signal(false);
  protected readonly saving = signal(false);
  protected readonly errors = signal<string[]>([]);
  protected readonly isEdit = (this.resolveId() ?? null) !== null;

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

  /** `undefined` en alta: el resource no fetchea (mismo idioma "params `undefined` = skip" usado en
   * toda la fase 2 de `[T-RXRESOURCE]`). */
  private readonly entityId = this.resolveId();
  private readonly entityResource = rxResource({
    params: () => this.entityId,
    stream: ({ params }) => this.crud.getById(params),
  });

  constructor() {
    effect(() => {
      this.loading.set(this.entityResource.isLoading());
      // `hasValue()` es el type guard oficial de Angular para leer `.value()` sin riesgo de que un
      // resource en estado error relance la excepción.
      if (this.entityResource.hasValue()) {
        this.patchForm(this.entityResource.value());
        return;
      }
      const error = this.entityResource.error();
      if (error) {
        this.errors.set(errorMessages(error as unknown as ApiError));
      }
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
          this.onSaved(saved);
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
    this.onCancel();
  }
}
