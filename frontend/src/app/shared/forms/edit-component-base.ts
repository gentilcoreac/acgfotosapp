import { Directive, inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { EditFormBase, EditableEntity } from './edit-form-base';

export type { EditableEntity } from './edit-form-base';

/** Data que recibe el diálogo de edición. */
export interface EditDialogData {
  id?: number | string;
}

/**
 * Base para componentes de edición/alta en diálogo (Signal Forms). Ver `EditFormBase` para lo
 * genérico (carga, submit, estado). Se abre con `MatDialog.open(MiEditComponent, { data: { id } })`.
 *
 * En el template: `<form novalidate (submit)="$event.preventDefault(); submit()">` (el `novalidate`
 * es necesario porque ya no está el `FormGroupDirective` que lo agregaba) y `[formField]="form.x"`
 * en cada control (los `tbi-*` funcionan por el bridge CVA del directive `FormField`).
 */
@Directive()
export abstract class EditComponentBase<T extends EditableEntity, TModel = T> extends EditFormBase<
  T,
  TModel
> {
  protected readonly dialogRef = inject<MatDialogRef<unknown, T>>(MatDialogRef);

  protected override resolveId(): number | string | undefined {
    const data = inject<EditDialogData | null>(MAT_DIALOG_DATA, { optional: true });
    return data?.id ?? undefined;
  }

  protected override onSaved(saved: T): void {
    this.dialogRef.close(saved);
  }

  protected override onCancel(): void {
    this.dialogRef.close();
  }
}
