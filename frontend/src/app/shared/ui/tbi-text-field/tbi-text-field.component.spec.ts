import { Component, ChangeDetectionStrategy, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormField, disabled, form, required } from '@angular/forms/signals';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { TbiTextFieldComponent } from './tbi-text-field.component';

@Component({
  imports: [FormField, TbiTextFieldComponent],
  changeDetection: ChangeDetectionStrategy.Eager,
  template: `<form novalidate><tbi-text-field [formField]="form.campo" label="Campo" /></form>`,
})
class HostComponent {
  readonly model = signal({ campo: '' });
  readonly disabledFlag = signal(false);
  readonly form = form(this.model, (path) => {
    required(path.campo, { message: 'Requerido' });
    disabled(path.campo, { when: () => this.disabledFlag() });
  });
}

describe('TbiTextFieldComponent', () => {
  let fixture: ComponentFixture<HostComponent>;
  let host: HostComponent;
  let input: HTMLInputElement;
  let field: TbiTextFieldComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HostComponent],
      providers: [provideNoopAnimations()],
    }).compileComponents();
    fixture = TestBed.createComponent(HostComponent);
    host = fixture.componentInstance;
    fixture.detectChanges();
    input = fixture.nativeElement.querySelector('input');
    field = fixture.debugElement.query((de) => de.name === 'tbi-text-field').componentInstance;
  });

  it('el valor del modelo se refleja en el input', () => {
    host.model.set({ campo: 'hola' });
    fixture.detectChanges();
    expect(field.value()).toBe('hola');
    expect(input.value).toBe('hola');
  });

  it('input del usuario propaga al modelo', () => {
    input.value = 'tipeado';
    input.dispatchEvent(new Event('input'));
    expect(host.model().campo).toBe('tipeado');
  });

  it('required: sin tocar el campo no muestra error; tras blur sí, con el mensaje del schema', () => {
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('.tbi-text-field__error')).toBeNull();

    input.dispatchEvent(new Event('blur'));
    fixture.detectChanges();

    const error = fixture.nativeElement.querySelector('.tbi-text-field__error');
    expect(error?.textContent).toContain('Requerido');
  });

  it('refleja el estado disabled declarado en el schema', () => {
    host.disabledFlag.set(true);
    fixture.detectChanges();
    expect(field.disabled()).toBe(true);
    expect(input.disabled).toBe(true);
  });
});
