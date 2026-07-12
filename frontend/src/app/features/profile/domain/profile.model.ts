/**
 * Perfil del usuario logueado (`GET usuarios/mi-perfil` → `UsuarioPerfilDto`). `userName`, `email`,
 * `administrador`, `emailConfirmed` y `claveVigente` son **read-only** en esta pantalla (la API no los
 * recibe en el update-profile o los gestiona por otra vía). `profilePicture` viaja como base64 y se
 * conserva tal cual en el round-trip (la edición de foto es un follow-up).
 */
export interface Perfil {
  id?: number;
  userName: string;
  nombre: string;
  apellido: string;
  email: string;
  telefono?: number | null;
  administrador: boolean;
  emailConfirmed: boolean;
  claveVigente: boolean;
  profilePicture?: string | null;
}

/** Body de `POST auth/cambiar-password` (`ChangePasswordInputModel`). */
export interface ChangePassword {
  currentPassword: string;
  newPassword: string;
  newConfirmPassword: string;
}
