# Catálogo de casos E2E — Cliente AcgFotos

> Capa fina de smoke (Playwright nativo). Cada caso = un test E2E. El **detalle** (validaciones por
> campo, lógica fina) ya vive en API/componente — acá solo van las **jornadas críticas cross-feature**.
> Ver estrategia en [`README.md`](./README.md).
>
> Columnas: **ID** · **Caso** · **Prioridad** · **Precondición** · **Acción** · **Resultado esperado** ·
> **Previene** (el bug/riesgo concreto) · **También cubierto por** (capa de abajo que lo respalda → muestra
> por qué el E2E no re-prueba el detalle).
>
> Roles del seed referenciados: **root** (tenant raíz), **adminB** (admin de tenant 2, con licencia),
> **userB** (no-root tenant 2, con licencia), **userSinLic** (no-root sin licencia activa), **tenant con
> branding** (colores/logo propios).

**Total: 50 casos** — Alta: 29 · Media: 21.

| Grupo                              | Casos | Prioridad        |
| ---------------------------------- | ----- | ---------------- |
| A. Autenticación y arranque (AUTH) | 10    | 8 Alta / 2 Media |
| B. Autorización y ruteo (AUTHZ)    | 4     | 3 Alta / 1 Media |
| C. Aplicación activa (APP)         | 4     | 2 Alta / 2 Media |
| D. Impersonalización (IMP)         | 5     | 3 Alta / 2 Media |
| E. CRUD canónico — Usuarios (CRUD) | 9     | 5 Alta / 4 Media |
| F. Errores y feedback (ERR)        | 4     | 1 Alta / 3 Media |
| G. Theming / branding (THEME)      | 5     | 3 Alta / 2 Media |
| H. Perfil (PROFILE)                | 3     | 2 Alta / 1 Media |
| I. Auditoría (AUD)                 | 4     | 2 Alta / 2 Media |
| J. Logs (LOG)                      | 4     | 1 Alta / 3 Media |

---

## A. Autenticación y arranque (AUTH)

> Login por rol, guards, logout, restauración de sesión en F5, reconexión ante 429.

| ID          | Caso                                         | Prio  | Precondición                      | Acción                        | Resultado esperado                                                        | Previene                          | También cubierto por     |
| ----------- | -------------------------------------------- | ----- | --------------------------------- | ----------------------------- | ------------------------------------------------------------------------- | --------------------------------- | ------------------------ |
| E2E-AUTH-01 | Login root → home + menú completo            | Alta  | seed root                         | login root                    | aterriza en home; sidenav con todos los ítems del sistema                 | regresión del arranque root       | F-AUTH-20, API auth 110  |
| E2E-AUTH-02 | Login no-root con licencia → menú acotado    | Alta  | userB                             | login userB                   | home; sidenav solo con sus ítems (no admin-only)                          | leak de opciones de menú          | 420-f-allowed, 240-men   |
| E2E-AUTH-03 | Login no-root SIN licencia → bloqueado       | Alta  | userSinLic                        | login                         | rechazo con mensaje de licencia; no entra al home                         | acceso sin licencia               | AUTH-02/14/15 (API)      |
| E2E-AUTH-04 | Credenciales inválidas → error visible       | Alta  | —                                 | login con clave mala          | mensaje de error en el form; no navega; sin sesión                        | login mudo / navegación indebida  | F-AUTH (login), 110-auth |
| E2E-AUTH-05 | Submit vacío → validación, sin request       | Media | —                                 | submit sin completar          | campos marcan requerido; no se dispara POST                               | submit mudo / request basura      | 640-f-login              |
| E2E-AUTH-06 | anonGuard: logueado en /login → home         | Media | sesión root                       | navegar a `/login`            | redirige al home                                                          | mostrar login estando logueado    | F-AUTH-23                |
| E2E-AUTH-07 | authGuard: ruta protegida sin sesión → login | Alta  | sin sesión                        | navegar a `/usuarios` por URL | redirige a `/login`                                                       | acceso sin sesión                 | F-AUTH-21                |
| E2E-AUTH-08 | Logout limpia sesión y cachés                | Media | sesión root                       | logout                        | va a `/login`; back-button no reentra; allowed-routes/app-context limpios | residuo de sesión                 | F-AUTH-10, F-CTX-12      |
| E2E-AUTH-09 | F5 restaura sesión (refresh OK)              | Alta  | logueado, cookie refresh válida   | F5 en una ruta interna        | sigue logueado en la misma ruta, sin re-login                             | re-login en cada F5               | F-AUTH-24                |
| E2E-AUTH-10 | F5 con refresh 429 → reconexión              | Alta  | refresh devuelve 429 en bootstrap | F5                            | pantalla de reconexión; reintenta y entra (no patea a login)              | pateo a login por blip/rate-limit | F-AUTH-25, 310-rate      |

## B. Autorización y ruteo por permisos (AUTHZ)

> El route-guard y el menú son la cara visible de la autorización. El detalle de la cadena de permisos
> está en la API (130-authz); acá se valida que el front **no expone** lo no permitido.

| ID           | Caso                                              | Prio  | Precondición                     | Acción                         | Resultado esperado                                                         | Previene                          | También cubierto por                    |
| ------------ | ------------------------------------------------- | ----- | -------------------------------- | ------------------------------ | -------------------------------------------------------------------------- | --------------------------------- | --------------------------------------- |
| E2E-AUTHZ-01 | Ruta no permitida (URL directa) → acceso denegado | Alta  | userB sin permiso de `endpoints` | navegar a `/endpoints` por URL | allowedRoutesGuard redirige a `/sin-permisos` (página 403); sigue logueado | leak de rutas por URL directa     | 420-f-allowed, 240-men (allowed-routes) |
| E2E-AUTHZ-02 | Menú trae solo ítems permitidos                   | Alta  | userB                            | login                          | sidenav sin ítems de permisos no asignados                                 | leak visual de opciones           | 240-men (principal)                     |
| E2E-AUTHZ-03 | `/principal` falla (429) → solo Inicio            | Media | menus/principal da 429           | login                          | sidenav muestra solo Inicio (no el estático completo)                      | leak visual de todas las opciones | 600-f-men                               |
| E2E-AUTHZ-04 | Root ve todos los ítems                           | Alta  | root                             | login                          | sidenav con el set completo del sistema                                    | regresión del menú root           | 240-men, F-AUTH-04                      |

## C. Aplicación activa (APP)

> Selector de aplicación en el sidenav: el header `aplicacionId` define qué ve el usuario. Bug histórico:
> header vacío → todo vacío.

| ID         | Caso                                  | Prio  | Precondición     | Acción                    | Resultado esperado                                        | Previene                                  | También cubierto por |
| ---------- | ------------------------------------- | ----- | ---------------- | ------------------------- | --------------------------------------------------------- | ----------------------------------------- | -------------------- |
| E2E-APP-01 | Switch de app recarga el menú una vez | Alta  | userB multi-app  | cambiar app en el sidenav | el menú recarga **una sola vez** con la app nueva         | menú desincronizado / doble carga         | F-CTX-08, F-CTX-16   |
| E2E-APP-02 | Selección de app persiste en F5       | Media | app seleccionada | F5                        | sigue la misma app (localStorage) sin flash de menú vacío | perder la selección / flash               | F-CTX-03, F-CTX-14   |
| E2E-APP-03 | Usuario single-app no ve selector     | Media | userB con 1 app  | login                     | el selector de app no se muestra                          | selector innecesario                      | F-CTX-11             |
| E2E-APP-04 | Root usa apps del tenant              | Alta  | root             | login                     | selector poblado con apps del tenant (no header vacío)    | el bug del header vacío → listados vacíos | F-CTX-02, 180-app    |

## D. Impersonalización (IMP)

> Root opera "como" un usuario destino (token re-scopeado, ADR-0002). La cookie-overlay firmada sostiene
> el contexto en refresh.

| ID         | Caso                                          | Prio  | Precondición                 | Acción                                      | Resultado esperado                                                          | Previene                           | También cubierto por                |
| ---------- | --------------------------------------------- | ----- | ---------------------------- | ------------------------------------------- | --------------------------------------------------------------------------- | ---------------------------------- | ----------------------------------- |
| E2E-IMP-01 | Impersonar → banner + menú del destino        | Alta  | root                         | impersonar tenant/usuario (diálogo 2 pasos) | banner "operando como X"; menú reducido al destino; ícono impersonar oculto | regresión de impersonación         | F-IMP-13, 120-imp (API)             |
| E2E-IMP-02 | Salir → root restaurado                       | Alta  | impersonando                 | clic en Salir                               | sin banner; menú root completo; vuelve al home                              | no poder salir de la impersonación | F-IMP-07                            |
| E2E-IMP-03 | Refresh sostiene impersonación                | Alta  | impersonando                 | F5 / forzar refresh                         | sigue como destino (overlay re-emite token), no vuelve a root               | volver a root tras refresh         | F-IMP-14, 120-imp (refresh overlay) |
| E2E-IMP-04 | Diálogo: error al cargar tenants/usuarios     | Media | getAll tenants / users falla | abrir diálogo / elegir tenant               | muestra mensaje; no queda vacío mudo; limpia error al reintentar            | error tragado                      | F-IMP-10, F-IMP-11                  |
| E2E-IMP-05 | Ícono impersonar solo root y no al impersonar | Media | root / impersonando          | render del header                           | visible a root; oculto a no-root y mientras impersona                       | no-root accediendo al selector     | F-IMP-08, F-IMP-09                  |

## E. CRUD canónico — Usuarios (CRUD)

> **Usuarios** es el CRUD representativo (toca tabs, licencias, roles, apps, validación). Cubre el
> contrato `tbi-table` + diálogo de edición M3 + persistencia real contra la API. Las reglas de negocio
> finas (mass-assignment, aislamiento) ya están en API 150-usr.

| ID          | Caso                                    | Prio  | Precondición                 | Acción                                             | Resultado esperado                                          | Previene                                 | También cubierto por              |
| ----------- | --------------------------------------- | ----- | ---------------------------- | -------------------------------------------------- | ----------------------------------------------------------- | ---------------------------------------- | --------------------------------- |
| E2E-CRUD-01 | Alta de usuario → aparece en el listado | Alta  | adminB                       | nuevo → completar → guardar                        | toast de éxito; el usuario aparece en la grilla             | regresión del alta por UI                | 150-usr (USR-01), 490-f-forms     |
| E2E-CRUD-02 | Editar → cambios persisten tras F5      | Alta  | usuario existente            | editar campo → guardar → F5                        | el cambio sigue tras recargar (persistió en API)            | guardado que no persiste                 | 150-usr (update)                  |
| E2E-CRUD-03 | Refresh conserva la página del listado  | Alta  | listado en página ≥ 2        | editar una fila → guardar                          | la grilla refresca **conservando** la página actual         | regresión del fix de paginación          | 480-f-ui (tbi-table)              |
| E2E-CRUD-04 | Borrar con confirmación                 | Media | usuario borrable             | eliminar → confirmar                               | confirma; el usuario desaparece de la grilla; toast         | borrado sin confirmación / fila fantasma | 150-usr (delete)                  |
| E2E-CRUD-05 | Buscador server-side filtra             | Media | varios usuarios              | escribir en el buscador                            | la grilla filtra (debounce) a las coincidencias server-side | buscador roto                            | buscador-server-side, 510-f-usr   |
| E2E-CRUD-06 | Selector de columnas muestra/oculta     | Media | listado con columna opcional | toggle en menú "Columnas"                          | la columna aparece/desaparece                               | regresión del selector de columnas       | 480-f-ui                          |
| E2E-CRUD-07 | Paginación server-side trae otra data   | Media | > 1 página                   | pasar de página                                    | la grilla muestra el siguiente set (no el mismo)            | paginación que no pagina                 | 480-f-ui                          |
| E2E-CRUD-08 | Validación del form bloquea guardar     | Alta  | diálogo abierto              | guardar con campo requerido vacío / email inválido | marca error; no guarda; no cierra el diálogo                | guardado inválido                        | 490-f-forms, 150-usr (validators) |
| E2E-CRUD-09 | Sin permiso de alta no ve "Nuevo"       | Alta  | userB sin permiso de alta    | abrir el listado                                   | el botón "Nuevo" no se muestra (gateado por permiso)        | escalada por UI                          | 240-men, 510-f-usr                |

## F. Errores y feedback (ERR)

> El errorInterceptor es la única fuente de toasts (PR 1499/1500). Forma estándar `{message,errors,traceId}`.

| ID         | Caso                                     | Prio  | Precondición                | Acción                   | Resultado esperado                                                                                             | Previene                                      | También cubierto por             |
| ---------- | ---------------------------------------- | ----- | --------------------------- | ------------------------ | -------------------------------------------------------------------------------------------------------------- | --------------------------------------------- | -------------------------------- |
| E2E-ERR-01 | Error 500 → un solo toast (coalescencia) | Alta  | acción que devuelve 500     | disparar el error        | aparece **un** toast (no doble cartel); coalescencia 3s                                                        | error silencioso o doble toast                | 450-f-http (F-HTTP), 340-errores |
| E2E-ERR-02 | Toast con "Ver detalle" / ref            | Media | error con `traceId`/detalle | abrir "Ver detalle"      | muestra detalle y `traceId`; botón Copiar                                                                      | error sin trazabilidad                        | 450-f-http (ErrorSnack)          |
| E2E-ERR-03 | Spinner anti doble-submit                | Media | acción que pega a la API    | clic en guardar/ingresar | botón en loading mientras pega; no permite doble submit                                                        | doble submit / submit mudo                    | F-IMP-12, spinners-api           |
| E2E-ERR-04 | Ruta inexistente → página 404            | Media | autenticado                 | navegar a ruta basura    | muestra la página 404 dentro del shell (`** → /404`); el errorInterceptor también lleva acá ante un 404 de API | ruta rota sin manejo / 404 que rebota al home | app.routes.ts, error.interceptor |

## G. Theming / branding (THEME)

> El tema/branding por tenant se aplica en runtime (M3 + colores/logo del tenant; ver 470-f-theme).

| ID           | Caso                                                  | Prio  | Precondición                                  | Acción                        | Resultado esperado                                                                            | Previene                                 | También cubierto por         |
| ------------ | ----------------------------------------------------- | ----- | --------------------------------------------- | ----------------------------- | --------------------------------------------------------------------------------------------- | ---------------------------------------- | ---------------------------- |
| E2E-THEME-01 | Tema del tenant aplicado al loguear                   | Alta  | userB de tenant con branding                  | login                         | colores/logo del tenant aplicados al shell                                                    | tema genérico                            | 470-f-theme, 230-ten (style) |
| E2E-THEME-02 | Branding del login por host/tenant                    | Media | host de tenant con branding                   | abrir `/login`                | logo/fondo del tenant en la pantalla de login                                                 | login sin branding                       | 470-f-theme, 590-f-ten       |
| E2E-THEME-03 | Re-tema al impersonar                                 | Alta  | root impersona a tenant con branding distinto | impersonar                    | el branding cambia al del tenant destino; al salir vuelve al de root                          | branding pegado del contexto anterior    | 470-f-theme, 440-f-imp       |
| E2E-THEME-04 | Theming aplicado tras login (M3/favicon/título/brand) | Alta  | logueado (root)                               | mirar el shell                | `--mat-sys-*` activas, `color-scheme` seteado, favicon con href, título, brand (logo o texto) | estilos/imágenes no aplicados tras login | 470-f-theme                  |
| E2E-THEME-05 | Toggle claro/oscuro cambia el tema                    | Media | logueado                                      | clic en el toggle del toolbar | `color-scheme` de `<html>` cambia (light↔dark)                                                | toggle de modo roto                      | F-AUTH (theme store)         |

## H. Perfil (PROFILE)

> "Mi perfil" self-service: ver/editar datos propios + cambiar contraseña. Accesible desde el avatar del
> sidenav. Ruta básica `/perfil` (no depende del menú).

| ID             | Caso                                         | Prio  | Precondición | Acción                               | Resultado esperado                                          | Previene                          | También cubierto por                 |
| -------------- | -------------------------------------------- | ----- | ------------ | ------------------------------------ | ----------------------------------------------------------- | --------------------------------- | ------------------------------------ |
| E2E-PROFILE-01 | Avatar del shell → /perfil con datos propios | Alta  | sesión root  | clic en el avatar/nombre del sidenav | navega a `/perfil`; muestra userName/email del usuario      | perfil inaccesible / datos ajenos | 150-usr (mi-perfil), spec componente |
| E2E-PROFILE-02 | Editar y guardar muestra confirmación        | Alta  | en `/perfil` | cambiar teléfono → Guardar           | toast de éxito (persistió contra la API)                    | guardado mudo / no persiste       | 150-usr (update-profile)             |
| E2E-PROFILE-03 | Cambiar contraseña valida coincidencia       | Media | en `/perfil` | nueva ≠ repetir → Cambiar contraseña | error "no coinciden"; no llama a la API; sigue en `/perfil` | cambio con confirmación inválida  | spec componente                      |

## I. Auditoría (AUD)

> Listado read-only de auditoría (root-only, `PermisoRoot`). Tabla paginada + filtros (fecha/servicio/status)
>
> - detalle por id. El aislamiento cross-tenant y la autorización los cubre la API (260-aud, 130-authz).

| ID         | Caso                                             | Prio  | Precondición | Acción                               | Resultado esperado                                  | Previene                        | También cubierto por                |
| ---------- | ------------------------------------------------ | ----- | ------------ | ------------------------------------ | --------------------------------------------------- | ------------------------------- | ----------------------------------- |
| E2E-AUD-01 | Carga del listado con filtros y filas            | Alta  | root         | abrir `/auditoria`                   | heading + barra de filtros + filas                  | regresión de la pantalla        | AuditoriaIsolation (API)            |
| E2E-AUD-02 | "Ver detalle" abre el diálogo                    | Media | root         | acción de fila → Ver detalle         | diálogo con el registro completo (params/respuesta) | detalle roto                    | AuditoriaIsolation (detalle por id) |
| E2E-AUD-03 | Filtrar por status inexistente → vacío + Limpiar | Media | root         | Resp. HTTP = 999 → Filtrar → Limpiar | listado vacío; Limpiar restaura las filas           | filtro que no filtra            | Auditoria filtros (API)             |
| E2E-AUD-04 | Filtrar por rango de fechas reduce resultados    | Alta  | root         | Desde = hoy (calendario) → Filtrar   | el total del paginator baja                         | filtro de fechas roto (bug fix) | AuditoriaIsolation AUD-08 (API)     |

## J. Logs (LOG)

> Log de aplicación (root cross-tenant, `logInfo/AllTenants`). Tabla paginada + detalle por fila. El gate
> root-raíz y el aislamiento los cubre la API (270-log).

| ID         | Caso                                            | Prio  | Precondición            | Acción                           | Resultado esperado                                              | Previene                                                | También cubierto por   |
| ---------- | ----------------------------------------------- | ----- | ----------------------- | -------------------------------- | --------------------------------------------------------------- | ------------------------------------------------------- | ---------------------- |
| E2E-LOG-01 | Carga del log con filas                         | Alta  | root (2 logs sembrados) | abrir `/logs`                    | heading + filas (cross-tenant)                                  | regresión de la pantalla                                | LogInfoIsolation (API) |
| E2E-LOG-02 | "Ver detalle" abre el diálogo                   | Media | root                    | acción de fila → Ver detalle     | diálogo con mensaje/excepción/propiedades (cargado por id)      | detalle roto                                            | spec componente        |
| E2E-LOG-03 | Buscar por mensaje filtra (server-side)         | Media | root (2 logs sembrados) | escribir en el buscador          | el listado filtra server-side al mensaje coincidente            | buscador roto                                           | LogInfo filtros (API)  |
| E2E-LOG-04 | Botón Filtrar (nivel) recarga; Limpiar restaura | Media | root (2 logs sembrados) | elegir nivel → Filtrar → Limpiar | Filtrar deja solo el nivel elegido; Limpiar vuelve a traer todo | filtro que no recarga (tbi-table reacciona a `filters`) | LogInfo filtros (API)  |

---

## Estado de implementación

| Caso                                | Estado                                                                                                                                             |
| ----------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------- |
| AUTH-01, AUTH-04, AUTH-05, AUTH-07  | ✅ implementado y verde (`specs/auth.spec.ts`)                                                                                                     |
| AUTH-06, AUTH-08, AUTH-09, AUTH-10  | ✅ implementado y verde (`specs/session.spec.ts`)                                                                                                  |
| AUTH-02, AUTH-03, AUTHZ-01/02/03    | ✅ implementado y verde (`specs/auth-roles.spec.ts`, contra `AcgFotos_TestE2E` con userb/usersinlic)                                                 |
| AUTHZ-04 (root ve todo)             | ✅ cubierto por AUTH-01 (`expectRootContext`)                                                                                                      |
| IMP-01/02/03/04                     | ✅ implementado y verde (`specs/impersonation.spec.ts`, root impersona userb en `AcgFotos_TestE2E`)                                                  |
| IMP-05 (ícono solo root)            | ✅ cubierto por AUTH-01 (root lo ve) + AUTH-02 (no-root no) + IMP-01 (oculto al impersonar)                                                        |
| APP-01/02/03                        | ✅ implementado y verde (`specs/app-context.spec.ts`; userb multi-app + userc single-app)                                                          |
| APP-04 (root: header no vacío)      | ✅ cubierto por AUTH-01 (root ve el menú completo ⇒ su app resolvió; el selector es solo no-root)                                                  |
| THEME-04, THEME-05                  | ✅ implementado y verde (`specs/theme.spec.ts`, post-login vs dev)                                                                                 |
| THEME-01/02/03                      | ✅ implementado y verde (`specs/theme-branding.spec.ts`; tenant 2 brandeado en `AcgFotos_TestE2E`)                                                   |
| CRUD-01/02/03/04/05/06/07/08        | ✅ implementado y verde (`specs/usuarios-crud.spec.ts`; alta/editar-F5/conservar-página/borrar/buscador/columnas/paginación/validación)            |
| CRUD-09 (sin permiso no ve "Nuevo") | N/A — el front muestra "Nuevo" **siempre** (no gatea por permiso); la protección real es el route-guard (AUTHZ-01) + authz server-side             |
| ERR-01/02/03/04                     | ✅ implementado y verde (`specs/errors.spec.ts`; coalescencia / Ver detalle+ref / spinner anti doble-submit / ruta inexistente→**página 404**)     |
| PROFILE-01/02/03                    | ✅ implementado y verde (`specs/profile.spec.ts`; avatar→/perfil+datos / editar+guardar real / validación de contraseñas)                          |
| AUD-01/02/03/04                     | ✅ implementado y verde (`specs/auditoria.spec.ts`; carga+filtros / detalle / filtro status / filtro fechas) — sobre `AcgFotos_TestE2E`              |
| LOG-01/02/03/04                     | ✅ implementado y verde (`specs/logs.spec.ts`; carga+filas cross-tenant / detalle por id / búsqueda por mensaje) — 2 logs sembrados en el seed e2e |

Notas:

- **AUTH-05** afirma que aparece "Requerido" en ambos campos + no dispara el POST + no navega. El bug de
  feedback de validación (matInput sin `ngControl` propio → `errorStateMatcher` muerto) **se corrigió** en
  `tbi-text-field` (ver README §9). Pendiente el mismo fix en `tbi-select`.
- **THEME-04/05** son el ángulo runnable hoy (theming M3 + favicon + título + brand aplicados tras login,
  y toggle claro/oscuro). El branding de un tenant concreto (THEME-01/02/03) necesita el seed dedicado.
- **ERR-01** se prueba determinista: en `/usuarios` se inyectan dos búsquedas que fallan 500 con mensajes
  distintos (`#1`/`#2`); la coalescencia (3s) descarta el segundo → el cartel `#2` **nunca aparece**, queda
  el `#1`, y hay un solo `.tbi-snack--error`.
- **ERR-02** son **dos tests**: detalle (`errors[]` → "Ver detalle" + lista + "Copiar") y ref técnica
  (`traceId` sin detalle → "ref: …" copiable). Son **mutuamente excluyentes** por diseño de
  `NotificationService` (`ref = hasDetail ? undefined : traceId`), por eso van separados.
- **ERR-03** se prueba sobre el **login** (`tbi-button [loading]`): se retiene la respuesta de `/auth/token`
  con `page.route` para observar el botón en "Ingresando…" + deshabilitado + spinner; el request es único.

## Notas de implementación (al pasar a código)

- **Sesión → login por test:** `loginAs(page, creds)` en `beforeEach` (NO storageState compartido,
  porque el refresh token rota → sería single-use/flaky; ver README §4). Los specs de login/validación
  arrancan anónimos.
- **CRUD canónico:** se eligió **Usuarios** por ser el más rico. Si más adelante se quiere un segundo
  CRUD smoke, **Roles** (tree-select de permisos) o **Tenants** (branding) son los siguientes candidatos
  — pero recordar la pirámide: el detalle ya está en API.
- **Datos mutables (CRUD/THEME):** crear con sufijo único y limpiar, o apoyarse en el reseed.
- **Decisiones abiertas** que tocan estos casos: reseed (endpoint vs DB reset), mensaje exacto de
  AUTH-03 (no-root sin licencia), y página 404 (ERR-04). Ver §9 del README.
