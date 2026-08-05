import { Component, ChangeDetectionStrategy, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormField, form, validate } from '@angular/forms/signals';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { TbiSliderComponent } from './tbi-slider.component';

@Component({
  imports: [FormField, TbiSliderComponent],
  changeDetection: ChangeDetectionStrategy.Eager,
  // minimo/maximo del control MÁS ANCHO que el rango del schema: si coincidieran, el propio
  // input[type=range] clampearía el valor y nunca se podría reproducir el error del schema.
  template: `<form novalidate><tbi-slider [formField]="form.campo" label="Campo" [minimo]="0" [maximo]="100" /></form>`,
})
class HostComponent {
  readonly model = signal({ campo: 5 });
  readonly form = form(this.model, (path) => {
    validate(path.campo, ({ value }) =>
      value() >= 0 && value() <= 10 ? undefined : { kind: 'rango', message: 'Fuera de rango' },
    );
  });
}

describe('TbiSliderComponent', () => {
  let fixture: ComponentFixture<HostComponent>;
  let host: HostComponent;
  let input: HTMLInputElement;
  let field: TbiSliderComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HostComponent],
      providers: [provideNoopAnimations()],
    }).compileComponents();
    fixture = TestBed.createComponent(HostComponent);
    host = fixture.componentInstance;
    fixture.detectChanges();
    input = fixture.nativeElement.querySelector('input');
    field = fixture.debugElement.query((de) => de.name === 'tbi-slider').componentInstance;
  });

  it('el valor del modelo se refleja en el input y en el texto', () => {
    host.model.set({ campo: 7 });
    fixture.detectChanges();
    expect(field.value()).toBe(7);
    expect(input.value).toBe('7');
    expect(fixture.nativeElement.querySelector('.tbi-slider__val').textContent).toContain('7');
  });

  it('mover el slider propaga al modelo', () => {
    input.value = '3';
    input.dispatchEvent(new Event('input'));
    expect(host.model().campo).toBe(3);
  });

  it('muestra el error del schema tras blur', () => {
    expect(fixture.nativeElement.querySelector('.tbi-slider__error')).toBeNull();

    input.value = '99';
    input.dispatchEvent(new Event('input'));
    input.dispatchEvent(new Event('blur'));
    fixture.detectChanges();

    const error = fixture.nativeElement.querySelector('.tbi-slider__error');
    expect(error?.textContent).toContain('Fuera de rango');
  });
});
