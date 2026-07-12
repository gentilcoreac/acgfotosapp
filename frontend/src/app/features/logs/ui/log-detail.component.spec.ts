import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MAT_DIALOG_DATA } from '@angular/material/dialog';
import { of } from 'rxjs';
import { LogsService } from '../data/logs.service';
import { LogInfo } from '../domain/log.model';
import { LogDetailComponent } from './log-detail.component';

const log: LogInfo = {
  id: 1,
  message: 'Algo explotó',
  messageTemplate: 'Algo {x}',
  level: 'Error',
  timeStamp: '2026-06-24T10:00:00',
  tenantId: 2,
  exception: 'System.Exception: boom',
  properties: '{"x":1}',
};

describe('LogDetailComponent', () => {
  let fixture: ComponentFixture<LogDetailComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [LogDetailComponent],
      providers: [
        { provide: LogsService, useValue: { getByIdAllTenants: () => of(log) } },
        { provide: MAT_DIALOG_DATA, useValue: { id: 1 } },
      ],
    }).compileComponents();
    fixture = TestBed.createComponent(LogDetailComponent);
    fixture.detectChanges();
  });

  it('carga el log por id y muestra mensaje, excepción y tenant', () => {
    const text = (fixture.nativeElement as HTMLElement).querySelector(
      '[data-testid="log-detail"]',
    )?.textContent;
    expect(text).toContain('Algo explotó');
    expect(text).toContain('System.Exception: boom');
    expect(text).toContain('2'); // tenant
  });
});
