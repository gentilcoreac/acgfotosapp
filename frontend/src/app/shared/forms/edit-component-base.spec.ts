import type { MockedObject } from 'vitest';
import { ChangeDetectionStrategy, Component, InjectionToken, inject, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { form, required } from '@angular/forms/signals';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { of } from 'rxjs';
import { CrudClient } from '../../core/http';
import { EditComponentBase, EditDialogData, EditableEntity } from './edit-component-base';

interface TestEntity extends EditableEntity {
  id?: number;
  name: string;
}

const CRUD = new InjectionToken<CrudClient<TestEntity>>('test-crud');

@Component({ changeDetection: ChangeDetectionStrategy.Eager, template: '' })
class TestEditComponent extends EditComponentBase<TestEntity> {
  protected readonly crud = inject(CRUD);
  protected readonly model = signal<TestEntity>({ id: 0, name: '' });
  protected readonly form = form(this.model, (path) => {
    required(path.name);
  });

  protected toEntity(): TestEntity {
    return this.model();
  }
  protected patchForm(entity: TestEntity): void {
    this.model.set(entity);
  }

  // Accesos para el test (los miembros de la base son protected).
  callSubmit(): void {
    this.submit();
  }
  callCancel(): void {
    this.cancel();
  }
  setName(value: string): void {
    this.model.update((m) => ({ ...m, name: value }));
  }
  get nameValue(): string {
    return this.model().name;
  }
  get editMode(): boolean {
    return this.isEdit;
  }
  get errorList(): string[] {
    return this.errors();
  }
}

describe('EditComponentBase', () => {
  let crud: MockedObject<CrudClient<TestEntity>>;
  let dialogRef: MockedObject<MatDialogRef<unknown, TestEntity>>;

  beforeEach(() => {
    crud = {
      getById: vi.fn().mockName('CrudClient.getById'),
      save: vi.fn().mockName('CrudClient.save'),
    } as unknown as MockedObject<CrudClient<TestEntity>>;
    dialogRef = {
      close: vi.fn().mockName('MatDialogRef.close'),
    } as unknown as MockedObject<MatDialogRef<unknown, TestEntity>>;
    TestBed.configureTestingModule({
      providers: [
        { provide: MatDialogRef, useValue: dialogRef },
        { provide: CRUD, useValue: crud },
        { provide: MAT_DIALOG_DATA, useValue: { id: 5 } },
      ],
    });
  });

  function create(data: EditDialogData | null): TestEditComponent {
    TestBed.overrideProvider(MAT_DIALOG_DATA, { useValue: data });
    const fixture = TestBed.createComponent(TestEditComponent);
    fixture.detectChanges();
    return fixture.componentInstance;
  }

  it('modo edición: carga la entidad y la vuelca al modelo del form', () => {
    crud.getById.mockReturnValue(of({ id: 5, name: 'cargado' }));
    const cmp = create({ id: 5 });
    expect(cmp.editMode).toBe(true);
    expect(crud.getById).toHaveBeenCalledWith(5);
    expect(cmp.nameValue).toBe('cargado');
  });

  it('modo alta: no carga nada', () => {
    const cmp = create(null);
    expect(cmp.editMode).toBe(false);
    expect(crud.getById).not.toHaveBeenCalled();
  });

  it('submit con form inválido no guarda y muestra el mensaje de campos obligatorios', () => {
    const cmp = create(null);
    cmp.callSubmit();
    expect(crud.save).not.toHaveBeenCalled();
    expect(cmp.errorList.length).toBe(1);
  });

  it('submit válido guarda y cierra el diálogo con la entidad', () => {
    const saved: TestEntity = { id: 9, name: 'guardado' };
    crud.save.mockReturnValue(of(saved));
    const cmp = create(null);
    cmp.setName('nuevo');
    cmp.callSubmit();
    expect(crud.save).toHaveBeenCalled();
    expect(dialogRef.close).toHaveBeenCalledWith(saved);
  });

  it('cancel cierra el diálogo', () => {
    const cmp = create(null);
    cmp.callCancel();
    expect(dialogRef.close).toHaveBeenCalled();
  });
});
