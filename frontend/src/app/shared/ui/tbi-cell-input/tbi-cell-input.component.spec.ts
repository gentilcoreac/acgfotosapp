import { Component, ChangeDetectionStrategy } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { TbiCellInputComponent } from './tbi-cell-input.component';

@Component({
  imports: [ReactiveFormsModule, TbiCellInputComponent],
  changeDetection: ChangeDetectionStrategy.Eager,
  template: `<tbi-cell-input [formControl]="ctrl" [inputMode]="mode" ariaLabel="Valor" />`,
})
class HostComponent {
  readonly ctrl = new FormControl('', { nonNullable: true });
  mode: 'text' | 'numeric' | 'decimal' = 'text';
}

describe('TbiCellInputComponent', () => {
  let fixture: ComponentFixture<HostComponent>;
  let host: HostComponent;
  let input: HTMLInputElement;
  let field: TbiCellInputComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HostComponent],
      providers: [provideNoopAnimations()],
    }).compileComponents();
    fixture = TestBed.createComponent(HostComponent);
    host = fixture.componentInstance;
    fixture.detectChanges();
    input = fixture.nativeElement.querySelector('input');
    field = fixture.debugElement.children[0].componentInstance;
  });

  it('writeValue: el valor del control se refleja en el input', () => {
    host.ctrl.setValue('25');
    fixture.detectChanges();
    expect(field.value()).toBe('25');
    expect(input.value).toBe('25');
  });

  it('el input del usuario propaga al control como string', () => {
    input.value = 'tipeado';
    input.dispatchEvent(new Event('input'));
    expect(host.ctrl.value).toBe('tipeado');
  });

  it('inputMode numeric mantiene el input como type=text (valor string)', () => {
    host.mode = 'numeric';
    fixture.detectChanges();
    expect(input.getAttribute('type')).toBe('text');
    expect(input.getAttribute('inputmode')).toBe('numeric');
  });

  it('refleja el estado disabled del control', () => {
    host.ctrl.disable();
    fixture.detectChanges();
    expect(field.disabled()).toBe(true);
    expect(input.disabled).toBe(true);
  });
});
