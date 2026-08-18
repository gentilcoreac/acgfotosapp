## Why

El filtro global multi-tenant de EF se desactiva por completo cuando el request es anónimo:

```csharp
// AcgFotos.Base.Infrastructure/Data/AcgFotosDbContext.cs:158
// AcgFotos.Core/Infrastructure/CustomDbContext.cs:65
c => c.TenantId == _appContext.TenantId || _appContext.IsAnonymous;
```

Es *permitir por defecto*: todo endpoint anónimo que se agregue en el futuro hereda acceso
cross-tenant a cada entidad multi-tenant, sin que nadie lo haya decidido y sin dejar rastro de esa
decisión en el código.

El repositorio ya estableció el patrón contrario y lo usa en todos lados donde alguien pensó el
problema: `IgnoreQueryFilters()` explícito en el método del repositorio, con un comentario que
justifica por qué esa consulta cruza tenants — impersonación, admins de otro tenant, barrido del
worker de fotos, refresh tokens y, notablemente, el canje de códigos de acceso, que **es un endpoint
anónimo y aun así pide el permiso explícitamente**. El comentario del canje razona como si el filtro
estuviera activo, lo que muestra que la convención viva del proyecto es el opt-in explícito: la
cláusula global es la excepción, no la regla.

Ahora importa porque el módulo de pagos (change `pagos-mercado-pago`) suma un webhook público —
necesariamente anónimo — que mueve el estado de cobro. Ese change se defiende por su cuenta, pero
hay una diferencia real entre "el webhook es seguro porque lo escribimos con cuidado" y "el webhook
es seguro porque el sistema no permite otra cosa". Con el filtro cerrado, un error futuro en ese
camino devuelve resultados vacíos en vez de datos de otro tenant.

## What Changes

- Se elimina la cláusula `|| _appContext.IsAnonymous` de los dos `DbContext`. El aislamiento de
  tenant pasa a aplicar siempre; el acceso cross-tenant queda como opt-in explícito por consulta.
- Los flujos de autenticación que hoy dependen de la cláusula —las búsquedas de `Usuario` del login,
  recuperación de contraseña y confirmación de cuenta— pasan a declarar `IgnoreQueryFilters()` en su
  método de repositorio, con el comentario que justifica el cruce, igual que el resto del repo.
- Test de invariante que falla si la cláusula vuelve: un request anónimo no debe poder leer una
  entidad multi-tenant de un tenant ajeno por el camino filtrado normal.

Alcance deliberadamente acotado: **no** se revisa el catálogo de permisos por endpoint, **no** se
toca `AuthorizationEnabled`, y **no** se agregan ni se quitan endpoints anónimos. Solo se cambia el
default del filtro y se declaran explícitamente los cruces que ya existen.

## Capabilities

### New Capabilities

- `aislamiento-multi-tenant`: garantía de que los datos de un tenant no son legibles ni modificables
  desde el contexto de otro, incluido el contexto anónimo, y la forma explícita en que una consulta
  puede declarar que necesita cruzar tenants.

### Modified Capabilities

Ninguna: no cambia el comportamiento observable de ninguna capability existente. El login, el canje y
los flujos de contraseña siguen funcionando igual — cambia dónde está declarado el permiso para
cruzar tenants, no quién puede hacerlo.

## Impact

**Backend**

- `AcgFotos.Core/Infrastructure/CustomDbContext.cs` y
  `AcgFotos.Base.Infrastructure/Data/AcgFotosDbContext.cs`: la expresión del filtro global.
- `AcgFotos.Base.Infrastructure/Repositories/UsuarioRepository.cs`: las búsquedas usadas por los
  flujos anónimos de autenticación.
- Posibles ajustes en las consultas de roles, licencias y aplicaciones que corren durante el login,
  mientras el contexto todavía es anónimo.
- `AcgFotos.Api.IntegrationTests`: test de invariante nuevo; la suite existente (508) es la red de
  seguridad principal de este change.

**Riesgo y por qué es asumible**

El modo de falla es ruidoso y temprano: si falta declarar un cruce, **el login se rompe de entrada**
y la suite de integración lo marca. No hay un modo de falla silencioso que filtre datos — el error
posible es de más restricción, no de menos. Ese es el sentido de hacerlo: mover el default hacia el
lado donde equivocarse es barato.
