/**
 * Body del endpoint `auth/resetear-password` (`ResetPasswordViewModel` de la API).
 *
 * `userName` y `code` llegan como query params del link del mail (`RecuperarClaveURL?userId=..
 * &code=..&userName=..&cliente=..`); la API resuelve el usuario por `emailOrUsername` (acepta
 * username o email), no por `userId`. `code` ya viene URL-decodeado por Angular al leer el query param.
 */
export interface ResetPassword {
  emailOrUsername: string;
  code: string;
  password: string;
  confirmPassword: string;
}
