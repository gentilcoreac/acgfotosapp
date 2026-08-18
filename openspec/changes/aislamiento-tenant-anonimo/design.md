## Context

Ver `proposal.md` § Why. Acá solo el relevamiento que acota el trabajo.

Se revisó endpoint anónimo por endpoint cuáles tocan entidades multi-tenant y cuáles dependen de la
cláusula `|| IsAnonymous`:

| Endpoint anónimo | ¿Depende de la cláusula? | Por qué |
|---|---|---|
| `POST /api/fotos/canje` | No | `CodigoAccesoRepository` ya declara `IgnoreQueryFilters()` |
| `POST /api/auth/refresh` | No | `RefreshTokenRepository` ya lo declara en sus tres métodos |
| `GET /api/tenant/public-style/{x}` | No | `Tenant` es `EntityBase`, no lleva filtro |
| `GET /api/parametro/valor-parametro-por-nombre/{n}` | No | `ParametroValorTenant` es `EntityBase` a propósito |
| `GET /api/home/version`, `discover` | No | No tocan entidades multi-tenant |
| `POST /api/auth/token` | **Sí** | `UsuarioRepository.GetByUserNameAsync` y `GetByUserNameWithAplicacionesAsync` no lo declaran |
| `POST /api/auth/olvide-password` | **Sí** | Misma resolución de usuario |
| `POST /api/auth/resetear-password` | **Sí** | Misma resolución de usuario |
| `POST /api/auth/confirmar-cuenta` | **Sí** | Misma resolución de usuario |

El trabajo real es un puñado de métodos de `UsuarioRepository` más lo que se consulte durante el
login mientras el contexto sigue anónimo (roles, licencias, aplicaciones). El relevamiento estático
no alcanza para cerrar esa lista con certeza: la suite de integración es la que la completa.

## Goals / Non-Goals

**Goals**

- Que el aislamiento entre tenants sea el default y el cruce sea una declaración visible en el
  código que la necesita.
- Que la regla quede protegida por un test que falle si alguien reintroduce la excepción.

**Non-Goals**

- Prender `AuthorizationEnabled` ni sembrar el catálogo de permisos por endpoint. Sigue siendo un
  trabajo aparte, anotado para Deploy.
- Revisar, agregar o quitar endpoints anónimos.
- Auditar los `IgnoreQueryFilters()` que ya existen. Están justificados con comentario y cada uno
  fue una decisión consciente; revisarlos es otro alcance.
- Cambiar cómo se resuelve el tenant en el login.

## Decisions

### D1 — Cerrar el default en lugar de restringir por endpoint

La alternativa considerada era dejar la cláusula y neutralizarla caso por caso: por ejemplo,
establecer un tenant "imposible" en el contexto de los endpoints anónimos que no necesitan cruzar.
Se descarta porque conserva el problema de fondo —el default sigue siendo abierto— y agrega una capa
de indirección que hay que recordar aplicar en cada endpoint anónimo nuevo. Es la misma trampa con
más pasos.

Cerrar el default invierte la carga: para cruzar tenants hay que escribirlo, y lo escrito se lee en
code review.

### D2 — Declarar el cruce en el método del repositorio, no en la capa de aplicación

Los cruces nuevos van donde ya están todos los demás: en el método del repositorio, con un comentario
que explica por qué esa consulta cruza. Es la convención viva del proyecto y mantiene la propiedad
útil de que buscar `IgnoreQueryFilters` en el código devuelve el inventario completo de accesos
cross-tenant.

### D3 — La suite de integración es el instrumento de relevamiento

El análisis estático identifica los métodos evidentes; los que se ejecutan durante el login antes de
que el contexto tenga tenant salen de correr la suite y ver qué falla. El orden de trabajo es:
quitar la cláusula, correr los 508 tests, y declarar los cruces que la suite exponga.

Esto es viable **porque el modo de falla es ruidoso**: falta un `IgnoreQueryFilters()` y el flujo se
rompe con una consulta vacía, no con una fuga silenciosa. Si el error posible fuera de menos
restricción en vez de más, este método de trabajo no serviría.

### D4 — Test de invariante que protege la regla

Se agrega un test que verifica que un contexto anónimo no lee entidades multi-tenant por el camino
normal. Sin él, la cláusula puede volver en un merge o en un backport desde CodigoBase y nadie se
entera.

Va junto a `MultiTenantInvariantTests`, que ya existe y ya cubre invariantes de este tipo.

## Risks / Trade-offs

- **Algún flujo de auth con un cruce no declarado llega a producción roto** → La suite de
  integración cubre login, refresh, contraseñas, confirmación de cuenta y canje. Se complementa con
  una verificación manual del login real en dev con un usuario no-root antes de cerrar.
- **Este repo es un fork de CodigoBase y allá la cláusula sigue existiendo** → Un backport futuro
  puede reintroducirla. Lo cubre el test de invariante de D4. Queda anotado en
  `docs/05-notas-abiertas.md` como divergencia deliberada respecto del código base, para que la
  próxima vuelta de sincronización no la deshaga por descuido.
- **La lista de cruces a declarar no se conoce con certeza antes de empezar** → Aceptado en D3: el
  descubrimiento es por suite, y el modo de falla lo hace seguro.

## Migration Plan

Sin migración de datos: el cambio es de comportamiento de consulta, no de esquema.

1. Quitar la cláusula de los dos `DbContext`.
2. Correr la suite completa y declarar los cruces que expongan los fallos.
3. Verificación manual del login en dev con un usuario no-root.
4. **Rollback**: revertir el commit. No hay estado persistido que deshacer.
