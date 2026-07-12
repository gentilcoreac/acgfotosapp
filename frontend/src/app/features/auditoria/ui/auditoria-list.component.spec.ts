import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MatDialog } from '@angular/material/dialog';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { of } from 'rxjs';
import { AuditoriaService } from '../data/auditoria.service';
import { AuditoriaListComponent } from './auditoria-list.component';

describe('AuditoriaListComponent', () => {
  let fixture: ComponentFixture<AuditoriaListComponent>;
  let component: AuditoriaListComponent;

  beforeEach(async () => {
    const crud = { getAllByCriteria: () => of({ items: [], totalCount: 0 }) };
    await TestBed.configureTestingModule({
      imports: [AuditoriaListComponent],
      providers: [
        provideNoopAnimations(),
        { provide: AuditoriaService, useValue: { crud, getById: () => of({}) } },
        { provide: MatDialog, useValue: { open: vi.fn().mockName('open') } },
      ],
    }).compileComponents();
    fixture = TestBed.createComponent(AuditoriaListComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('aplicar() arma el rango de fechas con bordes inclusivos del día + filtros de texto', () => {
    component['filtros'].setValue({
      fechaDesde: '2026-06-01',
      fechaHasta: '2026-06-24',
      servicio: 'AuthController',
      resultStatusCode: '500',
    });
    component['aplicar']();
    expect(component['filters']()).toEqual({
      fechaDesde: '2026-06-01T00:00:00',
      fechaHasta: '2026-06-24T23:59:59',
      servicio: 'AuthController',
      resultStatusCode: '500',
    });
  });

  it('limpiar() vacía los filtros', () => {
    component['filtros'].setValue({
      fechaDesde: '2026-06-01',
      fechaHasta: '',
      servicio: 'X',
      resultStatusCode: '',
    });
    component['aplicar']();
    component['limpiar']();
    expect(component['filters']()).toEqual({});
  });
});
