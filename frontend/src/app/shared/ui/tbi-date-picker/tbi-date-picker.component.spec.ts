import { Component, ChangeDetectionStrategy, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormField, form } from '@angular/forms/signals';
import { TbiDatePickerComponent } from './tbi-date-picker.component';

@Component({
  imports: [FormField, TbiDatePickerComponent],
  changeDetection: ChangeDetectionStrategy.Eager,
  template: `<form novalidate><tbi-date-picker [formField]="form.fecha" label="Fecha" /></form>`,
})
class HostComponent {
  readonly model = signal({ fecha: '' });
  readonly form = form(this.model);
}

describe('TbiDatePickerComponent', () => {
  let fixture: ComponentFixture<HostComponent>;
  let host: HostComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [HostComponent] }).compileComponents();
    fixture = TestBed.createComponent(HostComponent);
    host = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('el valor del modelo (yyyy-mm-dd) puebla el input sin romper', () => {
    host.model.set({ fecha: '2026-06-24' });
    fixture.detectChanges();
    const input = fixture.nativeElement.querySelector('input') as HTMLInputElement;
    expect(input.value).toContain('2026');
  });

  it('elegir una fecha escribe el modelo como yyyy-mm-dd', () => {
    const picker = fixture.debugElement.query((de) => de.name === 'tbi-date-picker')
      .componentInstance as TbiDatePickerComponent;
    picker['onDateChange'](new Date(2026, 5, 24));
    expect(host.model().fecha).toBe('2026-06-24');
  });

  it('limpiar la fecha deja el modelo vacío', () => {
    const picker = fixture.debugElement.query((de) => de.name === 'tbi-date-picker')
      .componentInstance as TbiDatePickerComponent;
    picker['onDateChange'](null);
    expect(host.model().fecha).toBe('');
  });
});

@Component({
  imports: [FormField, TbiDatePickerComponent],
  changeDetection: ChangeDetectionStrategy.Eager,
  template: `
    <form novalidate>
      <tbi-date-picker [formField]="form.fecha" label="Fecha" [min]="min()" [max]="max()" />
    </form>
  `,
})
class BoundedHostComponent {
  readonly model = signal({ fecha: '' });
  readonly form = form(this.model);
  readonly min = signal<string | undefined>('2024-01-15');
  readonly max = signal<string | undefined>('2024-02-15');
}

describe('TbiDatePickerComponent (min/max)', () => {
  it('las cotas yyyy-mm-dd llegan al matDatepicker como Date locales', async () => {
    await TestBed.configureTestingModule({ imports: [BoundedHostComponent] }).compileComponents();
    const fixture = TestBed.createComponent(BoundedHostComponent);
    fixture.detectChanges();

    const picker = fixture.debugElement.query((de) => de.name === 'tbi-date-picker')
      .componentInstance as TbiDatePickerComponent;
    expect(picker['minDate']()).toEqual(new Date(2024, 0, 15));
    expect(picker['maxDate']()).toEqual(new Date(2024, 1, 15));

    fixture.componentInstance.min.set(undefined);
    fixture.detectChanges();
    expect(picker['minDate']()).toBeNull();
  });
});
