import { Component, ChangeDetectionStrategy, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormField, disabled, form, required } from '@angular/forms/signals';
import { MatSelect } from '@angular/material/select';
import { By } from '@angular/platform-browser';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { TbiSelectComponent, TbiSelectOption, TbiSelectOptionGroup } from './tbi-select.component';

@Component({
  imports: [FormField, TbiSelectComponent],
  changeDetection: ChangeDetectionStrategy.Eager,
  template: `
    <form novalidate>
      <tbi-select [formField]="form.tipo" label="Tipo" [options]="options" />
    </form>
  `,
})
class HostComponent {
  readonly options: TbiSelectOption<number>[] = [
    { value: 1, label: 'Uno' },
    { value: 2, label: 'Dos' },
  ];
  readonly model = signal<{ tipo: number | null }>({ tipo: null });
  readonly disabledFlag = signal(false);
  readonly form = form(this.model, (path) => {
    required(path.tipo, { message: 'Requerido' });
    disabled(path.tipo, { when: () => this.disabledFlag() });
  });
}

describe('TbiSelectComponent', () => {
  let fixture: ComponentFixture<HostComponent>;
  let host: HostComponent;
  let field: TbiSelectComponent<number>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HostComponent],
      providers: [provideNoopAnimations()],
    }).compileComponents();
    fixture = TestBed.createComponent(HostComponent);
    host = fixture.componentInstance;
    fixture.detectChanges();
    field = fixture.debugElement.query((de) => de.name === 'tbi-select').componentInstance;
  });

  it('el valor del modelo se refleja en el field', () => {
    host.model.set({ tipo: 2 });
    fixture.detectChanges();
    expect(field.value()).toBe(2);
  });

  it('handleChange propaga al modelo', () => {
    field.handleChange(1);
    expect(host.model().tipo).toBe(1);
  });

  it('refleja el estado disabled declarado en el schema', () => {
    host.disabledFlag.set(true);
    fixture.detectChanges();
    expect(field.disabled()).toBe(true);
  });

  it('muestra el mensaje del schema cuando el field está tocado e inválido', () => {
    field.touch.emit();
    fixture.detectChanges();
    const error = fixture.nativeElement.querySelector('.tbi-select__error');
    expect(error?.textContent).toContain('Requerido');
  });
});

@Component({
  imports: [FormField, TbiSelectComponent],
  changeDetection: ChangeDetectionStrategy.Eager,
  template: `
    <form novalidate>
      <tbi-select [formField]="form.periodo" label="Período" [groups]="groups" />
    </form>
  `,
})
class GroupedHostComponent {
  readonly groups: TbiSelectOptionGroup<number>[] = [
    {
      label: '2024',
      options: [
        { value: 20240101, label: 'Enero 2024' },
        { value: 20240201, label: 'Febrero 2024' },
      ],
    },
    { label: '2025', options: [{ value: 20250101, label: 'Enero 2025' }] },
  ];
  readonly model = signal<{ periodo: number | null }>({ periodo: null });
  readonly form = form(this.model);
}

describe('TbiSelectComponent (groups)', () => {
  let fixture: ComponentFixture<GroupedHostComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [GroupedHostComponent],
      providers: [provideNoopAnimations()],
    }).compileComponents();
    fixture = TestBed.createComponent(GroupedHostComponent);
    fixture.detectChanges();
  });

  it('renderiza mat-optgroup por grupo con sus opciones', async () => {
    const matSelect = fixture.debugElement.query(By.directive(MatSelect))
      .componentInstance as MatSelect;
    matSelect.open();
    fixture.detectChanges();
    await fixture.whenStable();

    const groups = document.querySelectorAll('mat-optgroup');
    expect(groups.length).toBe(2);
    expect(groups[0].textContent).toContain('2024');
    expect(groups[0].querySelectorAll('mat-option').length).toBe(2);
    expect(groups[1].querySelectorAll('mat-option').length).toBe(1);
  });
});
