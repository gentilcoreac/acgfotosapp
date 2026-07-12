import type { Mock } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { Observable, Subject, of } from 'rxjs';
import { ConfirmService } from '../../feedback/confirm.service';
import { TbiRowActionComponent } from './tbi-row-action.component';

describe('TbiRowActionComponent', () => {
  let fixture: ComponentFixture<TbiRowActionComponent>;
  let confirm: Mock;

  async function setup(
    run: () => Observable<unknown>,
    confirmResult: boolean | null = null,
  ): Promise<void> {
    confirm = vi.fn().mockName('confirm').mockReturnValue(of(confirmResult));
    await TestBed.configureTestingModule({
      imports: [TbiRowActionComponent],
      providers: [provideNoopAnimations(), { provide: ConfirmService, useValue: { confirm } }],
    }).compileComponents();

    fixture = TestBed.createComponent(TbiRowActionComponent);
    fixture.componentRef.setInput('icon', 'delete');
    fixture.componentRef.setInput('run', run);
    fixture.detectChanges();
  }

  const clickButton = (): void => {
    const btn: HTMLButtonElement = fixture.nativeElement.querySelector('button');
    btn.click();
    fixture.detectChanges();
  };

  const hasSpinner = (): boolean => !!fixture.nativeElement.querySelector('mat-progress-spinner');

  it('sin confirm: ejecuta run y muestra spinner mientras la API está en vuelo', async () => {
    const api = new Subject<void>();
    const run = vi.fn().mockName('run').mockReturnValue(api.asObservable());
    await setup(run);

    clickButton();
    expect(run).toHaveBeenCalled();
    expect(hasSpinner()).toBe(true); // en vuelo

    api.next();
    api.complete();
    fixture.detectChanges();
    expect(hasSpinner()).toBe(false); // reseteado
  });

  it('con confirm aceptado: ejecuta run tras confirmar', async () => {
    const run = vi.fn().mockName('run').mockReturnValue(of(undefined));
    await setup(run, true);
    fixture.componentRef.setInput('confirm', { title: 'X', message: 'Y' });
    fixture.detectChanges();

    clickButton();
    expect(confirm).toHaveBeenCalled();
    expect(run).toHaveBeenCalled();
  });

  it('con confirm cancelado: NO ejecuta run', async () => {
    const run = vi.fn().mockName('run').mockReturnValue(of(undefined));
    await setup(run, false);
    fixture.componentRef.setInput('confirm', { title: 'X', message: 'Y' });
    fixture.detectChanges();

    clickButton();
    expect(confirm).toHaveBeenCalled();
    expect(run).not.toHaveBeenCalled();
  });
});
