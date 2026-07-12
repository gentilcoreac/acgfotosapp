# Checklist E2E (manual)

> Casos end-to-end que se validan **a mano en el browser** (lo que los unit tests no cubren:
> el roundtrip real de cookies, navegación, guards). **Este archivo es único y se va
> incrementando** con una sección por feature/flujo.
>
> **Automatización**: la suite **Playwright** ya existe (Fases 0-4 verdes) — ver
> [`e2e/README.md`](./e2e/README.md) y [`e2e/casos-e2e.md`](./e2e/casos-e2e.md). Varias de estas
> jornadas ya están automatizadas (p. ej. el login→F5→logout de abajo ≈ `E2E-AUTH-09`). Este checklist
> queda para validaciones manuales puntuales y como semillero de nuevos casos a automatizar.
>
> **Cómo levantar el entorno:**
>
> - API: `cd Api/AcgFotos.Api && dotnet run --no-launch-profile --urls "http://localhost:30000"`
>   (con `ASPNETCORE_ENVIRONMENT=Development`).
> - Cliente: `cd Cliente && npm start` (http://localhost:4200).
> - Usuario de prueba: `root` / `Root@AcgFotos2026!`.

---

## Auth — Refresh token (login → F5 → logout)

**Estado:** ✅ validado 2026-05-27

1. Sin sesión, abrir `http://localhost:4200` → redirige a `/login` (authGuard).
2. Login (`root` / `Root@AcgFotos2026!`) → navega a `/` y muestra "Sesión activa" (home).
3. **F5 en `/`** → **mantiene la sesión** y queda en home (el `provideAuthSessionRestore`
   refresca con la cookie); no vuelve a `/login`. ← caso clave del refresh.
4. DevTools → Application → Cookies (`localhost:30000`): `refreshToken` con `HttpOnly`,
   `Secure`, `SameSite=Strict`, `Path=/api/auth`.
5. DevTools → Network: al hacer F5, hay un `POST /api/auth/refresh` → **200** (renovación silenciosa).
6. "Cerrar sesión" → vuelve a `/login`, se borra la cookie; un F5 después queda en `/login`.

## General — Parámetros por tenant (override por tenant)

**Estado:** ✅ validado 2026-06-05 (Alberto)

1. Login `root` → el menú lateral muestra "Parámetros por tenant" → entrar.
2. Elegir un **tenant** → se cargan sus **aplicaciones**; elegir una → se lista la grilla de
   parámetros con su **valor efectivo** (las filas con override muestran el ícono de personalizado).
3. **Editar** un parámetro (lápiz) → el editor inline aparece **sin agrandar la fila**; cambiar el
   valor y confirmar (check, con spinner) → toast OK y la fila queda marcada como override.
4. **Restaurar** (restore) en una fila con override → confirma → vuelve al **valor por defecto** y
   desaparece el ícono.
5. Tipos de dato: texto/entero usan el input compacto (`tbi-cell-input`, `inputmode` numérico en
   enteros); booleano usa el toggle Sí/No.
6. Cambiar de tenant/aplicación recarga la grilla; sin selección completa, muestra el mensaje guía.

<!-- Próximas secciones (ej. "General — Roles CRUD", "Multi-tenant — selección de tenant") se agregan acá. -->
