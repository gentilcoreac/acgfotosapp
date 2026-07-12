# Spec — Refresh Token (contrato cliente ↔ API)

> **Estado: IMPLEMENTADO** (2026-05-26). API en `Api/feature/refresh-tokens` (validada e2e
> por HTTP) y wiring del cliente en `feature/fase-1-refresh-wiring` (interceptor 401→refresh
> serializado + reintento, `withCredentials`, restore en bootstrap, `/auth/logout`; 39 unit
> tests verdes). **Pendiente**: test e2e de browser (login → F5), bloqueado hasta migrar la
> página de login (Fase 3). Endpoints finales: `POST /api/auth/refresh` y `POST /api/auth/logout`.
>
> Documento histórico del contrato esperado (escrito en "Opción A"). El diseño definitivo de
> la API está en `Api/Docs/design-refresh-tokens.md`.

## Objetivo

- **Access token** corto (ej. 5–15 min), viaja como `Authorization: Bearer`. Vive solo en
  memoria en el cliente (`AuthStore`).
- **Refresh token** largo (ej. 7–30 días), en **cookie `HttpOnly; Secure; SameSite`**.
  Solo se usa contra `/auth/refresh`. JS nunca lo lee (inmune a XSS).
- Renovación silenciosa en F5 y ante expiración (401), sin re-login con credenciales.
- **Revocación real** server-side vía `SecurityStamp` + rotación.

## Cambios en la API

### 1. Entidad `RefreshToken` (persistida) + migración

Campos sugeridos: `Id`, `UserId`, `TokenHash` (hash del token, no el token plano),
`ExpiresAt`, `CreatedAt`, `CreatedByIp`, `RevokedAt`, `ReplacedByTokenHash`, `SecurityStamp`
(snapshot al emitir). Persistirlo es lo que habilita revocación y detección de reuso.

### 2. Login (`POST /api/auth/token`) — extender

Además del `TokenModelDto` actual, al autenticar OK:

- Emitir un refresh token, guardarlo (hasheado) y **setear la cookie**:
  `Set-Cookie: refreshToken=<token>; HttpOnly; Secure; SameSite=Strict; Path=/api/auth; Max-Age=<segundos>`
- El body de respuesta **no cambia** (el cliente sigue leyendo `token`/`validTo`/`tenantId`/`hasVerifiedEmail`).

### 3. `POST /api/auth/refresh` (nuevo, anónimo salvo cookie)

- Lee el refresh token de la cookie (no del body).
- Valida: existe, no expirado, no revocado, `SecurityStamp` coincide con el usuario actual.
- **Rota**: marca el usado como revocado (`RevokedAt`, `ReplacedByTokenHash`) y emite uno nuevo
  (nueva cookie).
- Responde el mismo shape que el login (`TokenModelDto`: `token`, `validTo`, `tenantId`,
  `hasVerifiedEmail`).
- **Detección de reuso**: si llega un refresh token ya revocado, revocar toda la cadena del
  usuario (posible robo) y responder 401.

### 4. `POST /api/auth/logout` (nuevo)

- Revoca el refresh token de la cookie y la borra (`Set-Cookie ... Max-Age=0`).

### 5. CORS / cookies

- Habilitar credenciales: `Access-Control-Allow-Credentials: true` y origin explícito
  (no `*`) para que el navegador envíe/acepte la cookie.

## Integración del lado cliente (cuando esté la API)

Ya está casi todo preparado; los puntos a tocar:

1. `AuthService.refreshSession()` ya existe → agregar `withCredentials: true` a esa request
   (vía `ApiClient`/opciones) para que viaje la cookie.
2. `authInterceptor`: reemplazar el seam del `401` (hoy `logout()`) por **refresh serializado**
   (un único refresh en vuelo compartido con `shareReplay`) + **reintento** del request original;
   si el refresh falla → `logout()`.
3. **Restore de sesión en bootstrap**: agregar un `provideAppInitializer` que llame a
   `refreshSession()` y tolere el fallo (sin sesión → login). Esto hace que el **F5** mantenga
   la sesión.
4. `AuthService.login()`/`logout()`: requests con `withCredentials: true`.

## Referencias

- Hallazgos de seguridad del original: `Docs/fwk-notes.md`.
- Decisión y fases: `Docs/refactor-plan.md` (Fase 1).
