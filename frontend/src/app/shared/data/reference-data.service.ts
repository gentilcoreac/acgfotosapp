import { Injectable, effect, inject } from '@angular/core';
import { Observable, map, shareReplay } from 'rxjs';
import { AuthStore } from '../../core/auth';
import { UsuariosService } from '../../features/usuarios/data/usuarios.service';
import { Usuario } from '../../features/usuarios/domain/usuario.model';

// Caché de sesión para datos de referencia que nuestros diálogos reusan (usuarios). Los datos son
// por-tenant: hay que descartarlos al cambiar de contexto (login / impersonalización / volver a
// root), o un usuario vería los del contexto anterior.
@Injectable({ providedIn: 'root' })
export class ReferenceDataService {
  private readonly usuariosService = inject(UsuariosService);
  private readonly auth = inject(AuthStore);

  private usuarios$?: Observable<Usuario[]>;

  // Invalida todo al cambiar el usuario efectivo (claim `sub`). `currentUserName` NO cambia en el
  // refresh silencioso (mismo usuario) → no tiramos la caché sin necesidad; solo en cambio de
  // contexto real. Mismo disparador que usa el menú/rutas para recargarse (ver AuthStore).
  private readonly invalidarAlCambiarContexto = effect(() => {
    this.auth.currentUserName();
    this.invalidateAll();
  });

  invalidateAll(): void {
    this.usuarios$ = undefined;
  }

  // Tabla de usuarios completa (la reusan los diálogos de asignación).
  getUsuarios(): Observable<Usuario[]> {
    this.usuarios$ ??= this.usuariosService.crud.getAll().pipe(
      map((r) => r.items),
      shareReplay(1),
    );
    return this.usuarios$;
  }

  invalidateUsuarios(): void {
    this.usuarios$ = undefined;
  }
}
