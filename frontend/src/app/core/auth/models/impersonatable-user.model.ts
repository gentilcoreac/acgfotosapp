/**
 * Usuario candidato a impersonalización (ADR-0002), tal como lo devuelve
 * `GET api/auth/impersonatable-users/{tenantId}`. Son **todos** los usuarios del tenant.
 */
export interface ImpersonatableUser {
  /** Id del usuario — target del impersonate. */
  id: number;
  userName: string;
  nombre: string;
  apellido: string;
  email: string;
}
