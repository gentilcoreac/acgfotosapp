/**
 * Body del endpoint `auth/confirmar-cuenta` (`ConfirmarCuentaViewModel` de la API).
 *
 * `userId` y `code` llegan como query params del link del mail que arma la API
 * (`ConfirmarCuentaURL?userId=..&code=..&userName=..&cliente=..`). De esos cuatro, la API solo
 * usa `userId` y `code` (token de Identity); `userName`/`cliente` viajan en el link por contexto
 * pero no son parte de este body. `code` ya viene URL-decodeado por Angular al leer el query param.
 */
export interface ConfirmAccount {
  userId: number;
  code: string;
  password: string;
  confirmPassword: string;
}
