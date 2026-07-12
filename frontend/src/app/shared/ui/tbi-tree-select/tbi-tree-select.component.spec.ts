import { Component, ChangeDetectionStrategy, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormField, form } from '@angular/forms/signals';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { TbiTreeNode, TbiTreeSelectComponent } from './tbi-tree-select.component';

@Component({
  imports: [FormField, TbiTreeSelectComponent],
  changeDetection: ChangeDetectionStrategy.Eager,
  template: `<tbi-tree-select [formField]="form.ids" [nodes]="nodes" />`,
})
class HostComponent {
  readonly nodes: TbiTreeNode[] = [
    {
      id: 1,
      name: 'Padre',
      children: [
        { id: 2, name: 'Hijo A' },
        { id: 3, name: 'Hijo B' },
      ],
    },
  ];
  readonly model = signal<{ ids: number[] }>({ ids: [] });
  readonly form = form(this.model);
}

describe('TbiTreeSelectComponent', () => {
  let fixture: ComponentFixture<HostComponent>;
  let host: HostComponent;

  /** Checkbox del nodo raíz (siempre renderizado, sin depender de la expansión). */
  const rootCheckbox = (): HTMLInputElement =>
    fixture.nativeElement.querySelector('input[type="checkbox"]') as HTMLInputElement;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HostComponent],
      providers: [provideNoopAnimations()],
    }).compileComponents();
    fixture = TestBed.createComponent(HostComponent);
    host = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('normaliza el padre como marcado cuando llegan todos los hijos (desde el modelo)', () => {
    host.model.set({ ids: [2, 3] });
    fixture.detectChanges();
    expect(rootCheckbox().checked).toBe(true);
    expect(rootCheckbox().indeterminate).toBe(false);
  });

  it('muestra el padre indeterminado con selección parcial', () => {
    host.model.set({ ids: [2] });
    fixture.detectChanges();
    expect(rootCheckbox().checked).toBe(false);
    expect(rootCheckbox().indeterminate).toBe(true);
  });

  it('marcar el padre propaga a los hijos (cascada) y actualiza el modelo', () => {
    rootCheckbox().click();
    fixture.detectChanges();
    expect(new Set(host.model().ids)).toEqual(new Set([1, 2, 3]));
  });

  it('desmarcar el padre limpia todo el subárbol', () => {
    host.model.set({ ids: [1, 2, 3] });
    fixture.detectChanges();
    rootCheckbox().click();
    fixture.detectChanges();
    expect(host.model().ids).toEqual([]);
  });
});
