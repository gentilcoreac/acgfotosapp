import { Component, ChangeDetectionStrategy, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormField, disabled, form, required } from '@angular/forms/signals';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { TbiTextareaComponent } from './tbi-textarea.component';

@Component({
  imports: [FormField, TbiTextareaComponent],
  changeDetection: ChangeDetectionStrategy.Eager,
  template: `<form novalidate><tbi-textarea [formField]="form.campo" label="Campo" /></form>`,
})
class HostComponent {
  readonly model = signal({ campo: '' });
  readonly disabledFlag = signal(false);
  readonly form = form(this.model, (path) => {
    required(path.campo, { message: 'Requerido' });
    disabled(path.campo, { when: () => this.disabledFlag() });
  });
}

describe('TbiTextareaComponent', () => {
  let fixture: ComponentFixture<HostComponent>;
  let host: HostComponent;
  let textarea: HTMLTextAreaElement;
  let field: TbiTextareaComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HostComponent],
      providers: [provideNoopAnimations()],
    }).compileComponents();
    fixture = TestBed.createComponent(HostComponent);
    host = fixture.componentInstance;
    fixture.detectChanges();
    textarea = fixture.nativeElement.querySelector('textarea');
    field = fixture.debugElement.query((de) => de.name === 'tbi-textarea').componentInstance;
  });

  it('el valor del modelo se refleja en el textarea', () => {
    host.model.set({ campo: 'hola' });
    fixture.detectChanges();
    expect(field.value()).toBe('hola');
    expect(textarea.value).toBe('hola');
  });

  it('input del usuario propaga al modelo', () => {
    textarea.value = 'tipeado';
    textarea.dispatchEvent(new Event('input'));
    expect(host.model().campo).toBe('tipeado');
  });

  it('required: sin tocar el campo no muestra error; tras blur sí, con el mensaje del schema', () => {
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('.tbi-textarea__error')).toBeNull();

    textarea.dispatchEvent(new Event('blur'));
    fixture.detectChanges();

    const error = fixture.nativeElement.querySelector('.tbi-textarea__error');
    expect(error?.textContent).toContain('Requerido');
  });

  it('refleja el estado disabled declarado en el schema', () => {
    host.disabledFlag.set(true);
    fixture.detectChanges();
    expect(field.disabled()).toBe(true);
    expect(textarea.disabled).toBe(true);
  });

  it('usa 4 rows por defecto', () => {
    expect(textarea.rows).toBe(4);
  });
});
