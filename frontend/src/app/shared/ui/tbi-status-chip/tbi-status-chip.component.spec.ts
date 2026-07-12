import { Component, ChangeDetectionStrategy } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { TbiChipTone, TbiStatusChipComponent } from './tbi-status-chip.component';

@Component({
  imports: [TbiStatusChipComponent],
  changeDetection: ChangeDetectionStrategy.Eager,
  template: `<tbi-status-chip [label]="label" [tone]="tone" [icon]="icon" />`,
})
class HostComponent {
  label = 'Activo';
  tone: TbiChipTone = 'success';
  icon: string | null = 'check_circle';
}

describe('TbiStatusChipComponent', () => {
  let fixture: ComponentFixture<HostComponent>;
  let host: HostComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [HostComponent] }).compileComponents();
    fixture = TestBed.createComponent(HostComponent);
    host = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('muestra el label, el ícono y la clase de tono', () => {
    const chip: HTMLElement = fixture.nativeElement.querySelector('.tbi-status-chip');
    expect(chip.textContent).toContain('Activo');
    expect(chip.textContent).toContain('check_circle');
    expect(chip.classList).toContain('tbi-status-chip--success');
  });

  it('cambia la clase de tono y omite el ícono cuando es null', () => {
    host.tone = 'error';
    host.icon = null;
    fixture.detectChanges();

    const chip: HTMLElement = fixture.nativeElement.querySelector('.tbi-status-chip');
    expect(chip.classList).toContain('tbi-status-chip--error');
    expect(chip.querySelector('.tbi-status-chip__icon')).toBeNull();
  });
});
