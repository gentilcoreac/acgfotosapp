import { Component, ChangeDetectionStrategy, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormField, form } from '@angular/forms/signals';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { Observable, of } from 'rxjs';
import { TbiSearchSelectComponent, TbiSearchSelectItem } from './tbi-search-select.component';

@Component({
  imports: [FormField, TbiSearchSelectComponent],
  changeDetection: ChangeDetectionStrategy.Eager,
  template: `<tbi-search-select [formField]="form.miembros" label="Miembros" [search]="search" />`,
})
class HostComponent {
  readonly search = (term: string): Observable<TbiSearchSelectItem[]> =>
    of([{ id: 9, label: `Resultado ${term}` }]);
  readonly model = signal<{ miembros: TbiSearchSelectItem[] }>({ miembros: [] });
  readonly form = form(this.model);
}

describe('TbiSearchSelectComponent', () => {
  let fixture: ComponentFixture<HostComponent>;
  let host: HostComponent;
  let field: TbiSearchSelectComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HostComponent],
      providers: [provideNoopAnimations()],
    }).compileComponents();
    fixture = TestBed.createComponent(HostComponent);
    host = fixture.componentInstance;
    fixture.detectChanges();
    field = fixture.debugElement.query((de) => de.name === 'tbi-search-select').componentInstance;
  });

  it('el valor del modelo se refleja en los seleccionados', () => {
    host.model.set({ miembros: [{ id: 1, label: 'Ana' }] });
    fixture.detectChanges();
    // toMatchObject: Signal Forms taggea los items de arrays del modelo con un Symbol de tracking.
    expect(field.value()).toMatchObject([{ id: 1, label: 'Ana' }]);
  });

  it('addItem agrega y propaga al modelo', () => {
    field.addItem({ id: 2, label: 'Bruno' });
    expect(host.model().miembros).toEqual([{ id: 2, label: 'Bruno' }]);
  });

  it('addItem no duplica por id', () => {
    field.addItem({ id: 2, label: 'Bruno' });
    field.addItem({ id: 2, label: 'Bruno' });
    expect(host.model().miembros.length).toBe(1);
  });

  it('remove quita y propaga al modelo', () => {
    host.model.set({
      miembros: [
        { id: 1, label: 'Ana' },
        { id: 2, label: 'Bruno' },
      ],
    });
    fixture.detectChanges();
    field.remove({ id: 1, label: 'Ana' });
    expect(host.model().miembros).toMatchObject([{ id: 2, label: 'Bruno' }]);
  });
});
