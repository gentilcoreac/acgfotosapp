import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiClient } from '../../../core/http';
import { ChangePassword, Perfil } from '../domain/profile.model';

/**
 * Servicio de datos del perfil propio. No es un CRUD (no hay id en la URL): la API resuelve el
 * usuario por el token. Lectura/edición de los datos propios (`mi-perfil`/`update-profile`) y cambio
 * de contraseña (`auth/cambiar-password`).
 */
@Injectable({ providedIn: 'root' })
export class ProfileService {
  private readonly api = inject(ApiClient);

  /** Perfil del usuario logueado (lo resuelve la API por el token). */
  getPerfil(): Observable<Perfil> {
    return this.api.get<Perfil>('general/usuarios/mi-perfil');
  }

  /** Persiste los datos editables del perfil (nombre/apellido/teléfono; el resto es read-only). */
  updateProfile(perfil: Perfil): Observable<unknown> {
    return this.api.post('general/usuarios/update-profile', perfil);
  }

  /** Cambia la contraseña propia (valida la actual server-side). */
  changePassword(model: ChangePassword): Observable<unknown> {
    return this.api.post('auth/cambiar-password', model);
  }
}
