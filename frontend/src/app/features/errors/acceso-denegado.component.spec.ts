import type { Mock } from 'vitest';
import { signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { AuthService, AuthStore } from '../../core/auth';
import { AccesoDenegadoComponent } from './acceso-denegado.component';

describe('AccesoDenegadoComponent', () => {
  let fixture: ComponentFixture<AccesoDenegadoComponent>;
  let logout: Mock;

  beforeEach(async () => {
    logout = vi.fn().mockName('logout');
    await TestBed.configureTestingModule({
      imports: [AccesoDenegadoComponent],
      providers: [
        provideRouter([]),
        { provide: AuthStore, useValue: { currentUserName: signal('jdoe@tenant.com') } },
        { provide: AuthService, useValue: { logout } },
      ],
    }).compileComponents();
    fixture = TestBed.createComponent(AccesoDenegadoComponent);
    fixture.detectChanges();
  });

  it('muestra el 403, el usuario logueado y un link al home', () => {
    const el: HTMLElement = fixture.nativeElement;
    expect(el.querySelector('[data-testid="acceso-denegado-page"]')).toBeTruthy();
    expect(el.textContent).toContain('403');
    expect(el.querySelector('[data-testid="acceso-denegado-user"]')?.textContent).toContain(
      'jdoe@tenant.com',
    );
    const home = el.querySelector('[data-testid="error-go-home"]') as HTMLAnchorElement;
    expect(home.getAttribute('href')).toBe('/');
  });

  it('el botón "Cerrar sesión" llama a AuthService.logout', () => {
    const el: HTMLElement = fixture.nativeElement;
    (el.querySelector('[data-testid="error-logout"]') as HTMLButtonElement).click();
    expect(logout).toHaveBeenCalled();
  });
});
