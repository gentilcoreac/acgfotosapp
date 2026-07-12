import { ApplicationRef } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { AllowedRoutesService, AuthStore } from '../../../core/auth';
import { DashboardContextService } from './dashboard-context.service';

/** JWT falso: header.payload.sig con el payload en base64url (lo que decodifica AuthStore). */
function fakeJwt(claims: Record<string, unknown>): string {
  const payload = btoa(JSON.stringify(claims)).replace(/=/g, '');
  return `header.${payload}.sig`;
}

/** Configura el contexto con una sesión (claims) y el set de paths permitidos que devuelve la API. */
function setup(
  claims: Record<string, unknown>,
  allowed: ReadonlySet<string> | null,
): DashboardContextService {
  TestBed.configureTestingModule({
    providers: [
      AuthStore,
      { provide: AllowedRoutesService, useValue: { getAllowedPaths: () => of(allowed) } },
      DashboardContextService,
    ],
  });
  TestBed.inject(AuthStore).setSession(fakeJwt(claims), new Date(Date.now() + 60000));
  const ctx = TestBed.inject(DashboardContextService);
  // Corre el effect que carga las capacidades (la fuente mock emite sincrónicamente).
  TestBed.inject(ApplicationRef).tick();
  return ctx;
}

describe('DashboardContextService', () => {
  it('root puede acceder a todo (sin depender de allowed-routes)', () => {
    const ctx = setup({ isRoot: 'True', tenant: '1' }, new Set());
    expect(ctx.isRoot()).toBe(true);
    expect(ctx.canAccess('/tenants')).toBe(true);
    expect(ctx.canAccess('/lo-que-sea')).toBe(true);
  });

  it('no-root: solo accede a las secciones permitidas', () => {
    const ctx = setup({ isRoot: 'False', tenant: '5' }, new Set(['/usuarios', '/grupos']));
    expect(ctx.isRoot()).toBe(false);
    expect(ctx.canAccess('/usuarios')).toBe(true);
    expect(ctx.canAccess('/grupos')).toBe(true);
    expect(ctx.canAccess('/tenants')).toBe(false);
  });

  it('falla abierto: si no se pudieron determinar las capacidades, deja ver todo', () => {
    const ctx = setup({ isRoot: 'False', tenant: '5' }, null);
    expect(ctx.canAccess('/tenants')).toBe(true);
  });

  it('ready() es true una vez resueltas las capacidades', () => {
    const ctx = setup({ isRoot: 'False', tenant: '5' }, new Set(['/usuarios']));
    expect(ctx.ready()).toBe(true);
  });

  it('isImpersonating refleja el claim del token', () => {
    const ctx = setup({ isRoot: 'False', tenant: '5', impersonatedBy: '1' }, new Set());
    expect(ctx.isImpersonating()).toBe(true);
  });
});
