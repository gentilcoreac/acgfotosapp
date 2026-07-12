import { Component, ChangeDetectionStrategy } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { of } from 'rxjs';
import { ConfirmService } from '../../feedback/confirm.service';
import { TbiRowAction, TbiRowActionsComponent } from './tbi-row-actions.component';

interface Row {
  id: number;
  name: string;
}

@Component({
  imports: [TbiRowActionsComponent],
  changeDetection: ChangeDetectionStrategy.Eager,
  template: `<tbi-row-actions [row]="row" [actions]="actions" />`,
})
class HostComponent {
  readonly row: Row = { id: 1, name: 'alfa' };
  edited: Row | null = null;
  ran = false;
  readonly actions: TbiRowAction<Row>[] = [
    { icon: 'edit', label: 'Editar', handler: (r) => (this.edited = r) },
    { icon: 'visibility', label: 'Oculta', hidden: () => true, handler: () => undefined },
    {
      icon: 'delete',
      label: 'Eliminar',
      danger: true,
      run: () => {
        this.ran = true;
        return of(null);
      },
    },
  ];
}

describe('TbiRowActionsComponent', () => {
  let fixture: ComponentFixture<HostComponent>;
  let host: HostComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HostComponent],
      providers: [
        provideNoopAnimations(),
        // Stub: evita levantar MatDialog real para las acciones sin confirmación.
        { provide: ConfirmService, useValue: { confirm: () => of(true) } },
      ],
    }).compileComponents();
    fixture = TestBed.createComponent(HostComponent);
    host = fixture.componentInstance;
    fixture.detectChanges();
  });

  function inlineButtons(): HTMLButtonElement[] {
    return Array.from(fixture.nativeElement.querySelectorAll('.tbi-row-actions__inline button'));
  }

  it('omite las acciones con `hidden`', () => {
    const labels = inlineButtons().map((b) => b.getAttribute('aria-label'));
    expect(labels).toEqual(['Editar', 'Eliminar']);
  });

  it('ejecuta el handler sincrónico con la fila', () => {
    const editBtn = inlineButtons().find((b) => b.getAttribute('aria-label') === 'Editar');
    editBtn?.click();
    expect(host.edited).toBe(host.row);
  });

  it('ejecuta la acción `run` (sin confirmación)', () => {
    const delBtn = inlineButtons().find((b) => b.getAttribute('aria-label') === 'Eliminar');
    delBtn?.click();
    expect(host.ran).toBe(true);
  });
});
