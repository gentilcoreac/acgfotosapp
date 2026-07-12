import type { MockedObject } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MatDialog } from '@angular/material/dialog';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { of } from 'rxjs';
import { LogsService } from '../data/logs.service';
import { LogInfo } from '../domain/log.model';
import { LogsListComponent } from './logs-list.component';

describe('LogsListComponent', () => {
  let fixture: ComponentFixture<LogsListComponent>;
  let component: LogsListComponent;
  let dialog: MockedObject<MatDialog>;

  const row: LogInfo = {
    id: 1,
    message: 'boom',
    level: 'Error',
    timeStamp: '2026-06-24T10:00:00',
    tenantId: 2,
    exception: 'stack',
    properties: '{}',
  };

  beforeEach(async () => {
    dialog = {
      open: vi.fn().mockName('MatDialog.open'),
    } as unknown as MockedObject<MatDialog>;
    await TestBed.configureTestingModule({
      imports: [LogsListComponent],
      providers: [
        provideNoopAnimations(),
        {
          provide: LogsService,
          useValue: {
            getAllTenants: () => of({ items: [row], totalCount: 1 }),
            getTenants: () => of([{ id: 2, nombre: 'Tenant Dos' }]),
          },
        },
        { provide: MatDialog, useValue: dialog },
      ],
    }).compileComponents();
    fixture = TestBed.createComponent(LogsListComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('expone columnas con nivel y tenant', () => {
    const keys = component['columns'].map((c) => c.key);
    expect(keys).toContain('level');
    expect(keys).toContain('tenantId');
  });

  it('verDetalle abre el diálogo con el id (el detalle lo trae por id)', () => {
    component['verDetalle'](row);
    expect(dialog.open).toHaveBeenCalled();
    const config = vi.mocked(dialog.open).mock.lastCall![1];
    expect(config?.data).toEqual({ id: row.id });
  });

  it('aplicar() arma los filtros de fecha/nivel/tenant', () => {
    component['filtros'].setValue({
      fechaDesde: '2026-06-01',
      fechaHasta: '',
      level: 'Error',
      tenantId: '2',
    });
    component['aplicar']();
    expect(component['filters']()).toEqual({
      fechaDesde: '2026-06-01T00:00:00',
      level: 'Error',
      tenantId: 2,
    });
  });

  it('la columna Tenant resuelve el nombre cargado', () => {
    const col = component['columns'].find((c) => c.key === 'tenantId');
    expect(col?.cell?.(row)).toBe('Tenant Dos');
  });
});
