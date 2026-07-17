import type { MockedObject } from 'vitest';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter, Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { FamiliaService } from '../../../../core/familia';
import { AppConfigService } from '../../../../core/config';
import { ApiError } from '../../../../core/models';
import { CanjeComponent } from './canje.component';

function makeActivatedRoute(codigo: string | null): ActivatedRoute {
  return {
    snapshot: { paramMap: convertToParamMap(codigo ? { codigo } : {}) },
  } as unknown as ActivatedRoute;
}

describe('CanjeComponent', () => {
  let familiaSpy: MockedObject<FamiliaService>;

  function configure(codigoDeLink: string | null = null): void {
    familiaSpy = { canjear: vi.fn().mockName('FamiliaService.canjear') } as unknown as MockedObject<FamiliaService>;
    TestBed.configureTestingModule({
      imports: [CanjeComponent],
      providers: [
        provideRouter([]),
        provideNoopAnimations(),
        { provide: FamiliaService, useValue: familiaSpy },
        { provide: ActivatedRoute, useValue: makeActivatedRoute(codigoDeLink) },
        { provide: AppConfigService, useValue: { config: () => ({ appTitle: 'AcgFotos' }) } },
      ],
    });
  }

  function create(): CanjeComponent {
    const fixture = TestBed.createComponent(CanjeComponent);
    fixture.detectChanges();
    return fixture.componentInstance;
  }

  it('no canjea con el form vacío', () => {
    configure();
    const cmp = create();
    cmp.submit();
    expect(familiaSpy.canjear).not.toHaveBeenCalled();
  });

  it('canje OK navega a /mi-album', () => {
    configure();
    const navSpy = vi
      .spyOn(TestBed.inject(Router), 'navigateByUrl')
      .mockReturnValue(undefined as unknown as Promise<boolean>);
    familiaSpy.canjear.mockReturnValue(
      of({ token: 't', validTo: 'x', eventoId: 1, nombreEvento: 'Egresados 2026', participantes: [] }),
    );
    const cmp = create();
    cmp.form.setValue({ codigo: 'K7F3-9QMD' });
    cmp.submit();
    expect(familiaSpy.canjear).toHaveBeenCalledWith('K7F3-9QMD');
    expect(navSpy).toHaveBeenCalledWith('/mi-album');
  });

  it('ante error muestra los mensajes del ApiError', () => {
    configure();
    const apiError: ApiError = { status: 400, message: 'Código inválido o vencido.', errors: [] };
    familiaSpy.canjear.mockReturnValue(throwError(() => apiError));
    const cmp = create();
    cmp.form.setValue({ codigo: 'ZZZZ-9999' });
    cmp.submit();
    expect(cmp.errors()).toEqual(['Código inválido o vencido.']);
    expect(cmp.loading()).toBe(false);
  });

  it('con :codigo en la ruta precarga y dispara el canje solo', () => {
    configure('K7F3-9QMD');
    familiaSpy.canjear.mockReturnValue(
      of({ token: 't', validTo: 'x', eventoId: 1, nombreEvento: 'Egresados 2026', participantes: [] }),
    );
    const cmp = create();
    expect(cmp.form.getRawValue().codigo).toBe('K7F3-9QMD');
    expect(familiaSpy.canjear).toHaveBeenCalledWith('K7F3-9QMD');
  });
});
