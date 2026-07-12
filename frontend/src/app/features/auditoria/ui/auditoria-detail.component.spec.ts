import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MAT_DIALOG_DATA } from '@angular/material/dialog';
import { of } from 'rxjs';
import { AuditoriaService } from '../data/auditoria.service';
import { Auditoria } from '../domain/auditoria.model';
import { AuditoriaDetailComponent } from './auditoria-detail.component';

const registro: Auditoria = {
  id: 5,
  fechaHora: '2026-06-24T10:00:00',
  duracion: 12.5,
  servicio: 'AuthController',
  metodo: 'Token',
  parametros: '{"user":"root"}',
  httpMethod: 'POST',
  requestAbsolutePath: '/api/auth/token',
  clientIP: '127.0.0.1',
  clientUserAgent: 'jest',
  resultStatusCode: '200',
  resultContent: 'ok',
  usuarioNombre: 'root',
  impersonatedBy: null,
};

describe('AuditoriaDetailComponent', () => {
  let fixture: ComponentFixture<AuditoriaDetailComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AuditoriaDetailComponent],
      providers: [
        { provide: AuditoriaService, useValue: { getById: () => of(registro) } },
        { provide: MAT_DIALOG_DATA, useValue: { id: 5 } },
      ],
    }).compileComponents();
    fixture = TestBed.createComponent(AuditoriaDetailComponent);
    fixture.detectChanges();
  });

  it('carga el registro por id y muestra servicio.método y parámetros', () => {
    const el: HTMLElement = fixture.nativeElement;
    const text = el.querySelector('[data-testid="auditoria-detail"]')?.textContent ?? '';
    expect(text).toContain('AuthController.Token');
    expect(text).toContain('{"user":"root"}');
  });
});
