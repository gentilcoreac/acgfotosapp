import { Component, ChangeDetectionStrategy, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormField, form } from '@angular/forms/signals';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { TbiTreeNode } from '../tbi-tree-select/tbi-tree-select.component';
import { TbiTreeRadioComponent } from './tbi-tree-radio.component';

@Component({
  imports: [FormField, TbiTreeRadioComponent],
  changeDetection: ChangeDetectionStrategy.Eager,
  template: `<tbi-tree-radio [formField]="form.id" [nodes]="nodes" />`,
})
class HostComponent {
  readonly nodes: TbiTreeNode[] = [
    { id: 1, name: 'Padre', children: [{ id: 2, name: 'Hijo' }] },
    { id: 3, name: 'Otro' },
  ];
  readonly model = signal<{ id: number | null }>({ id: null });
  readonly form = form(this.model);
}

describe('TbiTreeRadioComponent', () => {
  let fixture: ComponentFixture<HostComponent>;
  let host: HostComponent;

  const checkboxFor = (name: string): HTMLInputElement => {
    const label = Array.from(
      fixture.nativeElement.querySelectorAll('mat-checkbox') as NodeListOf<HTMLElement>,
    ).find((el) => el.textContent?.trim() === name);
    return label!.querySelector('input[type="checkbox"]') as HTMLInputElement;
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HostComponent],
      providers: [provideNoopAnimations()],
    }).compileComponents();
    fixture = TestBed.createComponent(HostComponent);
    host = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('el valor del modelo se refleja como único checkbox marcado', () => {
    host.model.set({ id: 3 });
    fixture.detectChanges();
    expect(checkboxFor('Otro').checked).toBe(true);
    expect(checkboxFor('Padre').checked).toBe(false);
  });

  it('marcar un nodo actualiza el modelo y reemplaza la selección previa', () => {
    checkboxFor('Padre').click();
    fixture.detectChanges();
    expect(host.model().id).toBe(1);

    checkboxFor('Otro').click();
    fixture.detectChanges();
    expect(host.model().id).toBe(3);
  });

  it('desmarcar el nodo activo deja el modelo en null', () => {
    host.model.set({ id: 1 });
    fixture.detectChanges();
    checkboxFor('Padre').click();
    fixture.detectChanges();
    expect(host.model().id).toBeNull();
  });
});
