import type { Mock } from 'vitest';
import { ApplicationRef } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { AuthStore } from '../../core/auth';
import { UsuariosService } from '../../features/usuarios/data/usuarios.service';
import { ReferenceDataService } from './reference-data.service';

/** JWT falso: header.payload.sig con el payload en base64url (lo que decodifica AuthStore). */
function fakeJwt(claims: Record<string, unknown>): string {
  const payload = btoa(JSON.stringify(claims)).replace(/=/g, '');
  return `header.${payload}.sig`;
}

describe('ReferenceDataService', () => {
  let usuariosGetAll: Mock;
  let svc: ReferenceDataService;

  /** Cambia el usuario efectivo del token (claim `sub`) y corre los effects pendientes. */
  function cambiarContexto(sub: string): void {
    TestBed.inject(AuthStore).setSession(fakeJwt({ sub }), new Date(Date.now() + 60000));
    TestBed.inject(ApplicationRef).tick();
  }

  beforeEach(() => {
    usuariosGetAll = vi
      .fn()
      .mockName('usuarios.getAll')
      .mockReturnValue(of({ items: [{ id: 1 }] }));

    TestBed.configureTestingModule({
      providers: [
        AuthStore,
        { provide: UsuariosService, useValue: { crud: { getAll: usuariosGetAll } } },
        ReferenceDataService,
      ],
    });

    // Sesión inicial (usuario "root") ANTES de instanciar el servicio, para que la primera corrida
    // del effect lea ese contexto sobre cachés vacías (no-op) y no contamine los conteos.
    TestBed.inject(AuthStore).setSession(fakeJwt({ sub: 'root' }), new Date(Date.now() + 60000));
    svc = TestBed.inject(ReferenceDataService);
    TestBed.inject(ApplicationRef).tick();
  });

  it('cachea: dos lecturas de getUsuarios pegan a la API una sola vez', () => {
    svc.getUsuarios().subscribe();
    svc.getUsuarios().subscribe();
    expect(usuariosGetAll).toHaveBeenCalledTimes(1);
  });

  it('invalida la caché al cambiar de contexto (login / impersonar / salir)', () => {
    svc.getUsuarios().subscribe(); // pobla la caché (1ª llamada)
    cambiarContexto('otro-usuario'); // cambia `sub` → el effect descarta la caché
    svc.getUsuarios().subscribe(); // debe volver a pegar (2ª llamada)
    expect(usuariosGetAll).toHaveBeenCalledTimes(2);
  });

  it('NO invalida en el refresh silencioso (mismo usuario, token nuevo)', () => {
    svc.getUsuarios().subscribe(); // 1ª llamada
    cambiarContexto('root'); // mismo `sub` → currentUserName no cambia → no se invalida
    svc.getUsuarios().subscribe();
    expect(usuariosGetAll).toHaveBeenCalledTimes(1);
  });
});
