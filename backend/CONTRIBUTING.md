# Contributing Guide — AcgFotos API

## Índice

- [Documentación y Setup](#documentación-y-setup)
- [Idioma](#idioma)
- [Branching Strategy](#branching-strategy)
- [Convenciones de Código](#convenciones-de-código)
  - [Formato (enforced por .editorconfig)](#formato-enforced-por-editorconfig)
  - [Naming Conventions](#naming-conventions)
  - [Comentarios](#comentarios)
  - [Patrones Obligatorios](#patrones-obligatorios)
- [Checklist: Nuevo endpoint / entidad del vertical Fotos](#checklist-nuevo-endpoint--entidad-del-vertical-fotos)
- [Pull Requests](#pull-requests)
- [Testing](#testing)
- [Ambientes](#ambientes)
- [Troubleshooting](#troubleshooting)

---

## Documentación y Setup

- **Setup local desde cero** (clonar, DB, migraciones, seed inicial, levantar el host): ver
  [`../README.md`](../README.md) § Entorno de desarrollo.
- **Origen del proyecto**: fork de `C:\PROYECTOS\CodigoBase` (API + Cliente), renombrado
  `TechBI → AcgFotos`, sin el vertical Budget — ver ADR-09 en
  [`../docs/04-decisiones.md`](../docs/04-decisiones.md).
- **Decisiones de arquitectura**: [`../docs/04-decisiones.md`](../docs/04-decisiones.md) (ADRs).
  Patrones heredados de la plataforma (capas, multi-tenant, FluentValidation, repos):
  [`../docs/01-arquitectura.md`](../docs/01-arquitectura.md).
- **Roadmap y estado**: [`../docs/03-fases.md`](../docs/03-fases.md) (fases) y
  [`../docs/05-notas-abiertas.md`](../docs/05-notas-abiertas.md) (pendientes).

## Idioma

- El código (clases, métodos, variables, DTOs) se escribe en **inglés**, salvo las entidades de
  dominio (`Tenant`, `Usuario`, `Rol`, `Evento`, `Grupo`, `Participante`, `Pedido`...) que mantienen
  su nombre en español (naming genérico del ADR-10).
- Los comentarios y la documentación se escriben en **español**.
- Los mensajes de commit se escriben en **español, minúscula** (`commitlint`/`husky` lo exige —
  `fix: ...` / `feat: ...`, nunca `Fix: ...` ni sentence-case).

## Branching Strategy

| Branch | Propósito |
|---|---|
| `master` | Integración |
| `feature/<nombre>` | Desarrollo de funcionalidades |
| `fix/<nombre>` | Correcciones de bugs |
| `doc/<nombre>` | Actualización de documentación |

Ramas nuevas salen de `master` actualizado, no de una rama de feature en curso de otro subsistema
(evita arrastrar migraciones EF ajenas — la historia de `Migrations/` es lineal).

## Convenciones de Código

### Formato (enforced por `.editorconfig`)

- **Indentación**: 4 espacios C#.
- **Encoding**: UTF-8, final newline.
- **Usings**: `System.*` primero.
- **`this.`**: no usar para campos. Sí para propiedades, métodos y eventos.
- **Keywords vs Types**: preferir `string`/`int`/etc. sobre `String`/`Int32`/etc.
- **`var`**: preferido para tipos obvios.

### Naming Conventions

| Elemento | Convención | Ejemplo |
|---|---|---|
| Clases, métodos, propiedades públicas | PascalCase | `EventoAppService`, `GetByIdAsync` |
| Interfaces | `I` + PascalCase | `IEventoAppService` |
| Campos privados de instancia | `_camelCase` | `_dbContext`, `_eventoRepository` |
| Parámetros y variables locales | camelCase | `tenantId`, `result` |
| Entidades de plataforma (Base) | sin prefijo (español) | `Tenant`, `Usuario`, `Menu` |
| Entidades del vertical Fotos | sin prefijo propio (español, ADR-10) | `Evento`, `Grupo`, `Participante`, `Pedido` |
| Tablas SQL | `gen_` (plataforma) / `fot_` (Fotos), plural | `gen_Menus`, `fot_Pedidos` |
| DTOs | Sufijo `Dto` (+ `HeaderDto`/`InputDto` cuando el agregado separa read/write) | `EventoDto`, `EventoInputDto` |
| Application Services | Sufijo `AppService` | `EventoAppService` |
| Domain/validation Services | Sufijo `Service`/`ValidationService` | `GeneradorCodigoAcceso` |
| Controllers | Sufijo `Controller` | `EventoController` |
| Validators (FluentValidation) | Sufijo `InputDtoValidator` | `EventoInputDtoValidator` |

### Comentarios

Regla dura, con incidente real detrás (ver más abajo): **el código se escribe para que se entienda
solo; el comentario es la excepción, no la norma.**

**Comentar SOLO lo que el código no puede decir por sí mismo:**
- Una restricción externa no obvia (por qué un valor de storage/config exige un patrón determinado,
  por qué un pipeline en background necesita un orden puntual).
- Una divergencia deliberada del código base o de lo "obvio" (por qué esta tabla NO cascadea, por qué
  se eligió `Restrict` en vez de `Cascade`).
- Un workaround puntual, con motivo.
- Un catch que silencia intencionalmente (documentar por qué no debe propagar).

**NO comentar:**
- Lo que el nombre del método/variable ya dice (`// busca el tenant por id` sobre
  `GetTenantById(id)`).
- La justificación de por qué el cambio es correcto, o la historia de cómo se llegó a él — eso es
  contenido del **mensaje de commit**, no del código. El código no lleva bitácora de su propia
  edición.
- **Referencias a tareas o fases de trabajo internas/efímeras** (IDs de sesión, nombres de tarea de
  un plan de trabajo puntual, `[T-ALGO]` o similar). Un comentario así queda sin sentido para
  cualquiera que lea el archivo sin el contexto de esa sesión puntual — y esa sesión no es
  recuperable. La trazabilidad de *por qué* se hizo un cambio vive en el mensaje de commit y en
  `docs/04-decisiones.md`/`docs/05-notas-abiertas.md`, que sí son recuperables y buscables.
- Un `TODO` sin dueño ni destino. Si hay trabajo futuro genuino y planificado, el `TODO` debe
  apuntar a algo que alguien pueda ir a leer — un ADR, una entrada de `docs/05-notas-abiertas.md`, un
  ticket real — nunca a una fase interna que "ya se sabe cuál es" porque se habló en una conversación.

*Por qué existe esta regla*: en el código base del que forkea este proyecto, durante un forward-port
un comentario de test terminó citando un identificador de tarea de una sesión de trabajo puntual
(`T-VALIDATORS-AUDIT`) — sin ningún significado para quien lea el test después. El motivo por el que
se escribió así (cross-referenciar el propio seguimiento de tareas de esa sesión hacia el código, por
costumbre) no es una razón válida: esa referencia sirve para el que la escribió, en ese momento, no
para el repositorio.

### Patrones Obligatorios

#### Flujo de capas

```
Controller → AppService → Repository / DbContext
                       ↓ ValidationService (reglas de negocio que cruzan entidades)
```

- **Controllers**: solo orquestación. Heredan de `ExtendedEntityApiControllerBase<...>` (CRUD +
  paginación) cuando el recurso encaja; endpoints custom van con `[HttpPost]`/`[Route]` propios.
- **AppServices**: lógica de aplicación, transacciones, mapeo DTO ↔ Entity (Mapperly en el read
  path; write path con `SetValues` + `ChildCollectionSync`).
- **ValidationServices**: reglas de negocio que necesitan consultar la base (unicidad, relaciones
  cruzadas) — corren ANTES de persistir, desde el `UpdateAsync` del AppService. No se hacen async en
  `FluentValidation` — eso queda solo para forma.

#### FluentValidation — la trampa real que ya pasó una vez

Un `XxxInputDtoValidator : AbstractValidator<XxxInputDto>` en `InputValidators/` **NO se ejecuta solo
por existir**. `ExtendedEntityAppServiceBase.CheckInputValidations<G>(dto)` lo busca por reflexión y
lo corre, pero **solo si algo lo invoca explícitamente** — el `UpdateAsync` genérico de la base lo
llama automáticamente; cualquier `AppService` que **overridea `UpdateAsync` completo** (patrón común
cuando hay lógica propia antes/después de persistir, ver `GrupoAppService`) tiene que llamar
`this.CheckInputValidations(dto)` a mano, sobre el DTO con el mismo nombre de tipo que el validator
busca. Antes de dar por sentado que un validator nuevo "ya está cableado", confirmarlo con una
llamada real contra el host (un payload inválido debe volver 400, no 200 con el dato basura
persistido).

#### Multi-tenancy — estampado centralizado

- Toda entidad de negocio hereda de `MultiTenantEntityBase`.
- **El estampado de `TenantId` y el guard multi-tenant viven centralizados en
  `AcgFotosDbContext.SaveChanges`** (regla "estampar-si-0"). Los `AppService` del vertical Fotos
  **NO** estampan `TenantId` a mano ni llaman `IAppContext.AllowSaveMultiTenantEntities()` — crean la
  entidad con `TenantId=0` y comitean; el DbContext se encarga. `TenantId` es inmutable en updates
  (invariante impuesta por el mismo embudo).
- Para setup por-entidad que antes vivía en un `SetupContext` por AppService, usar el hook
  `BeforeUpdate(entity, dto, isNew)` de `ExtendedEntityAppServiceBase`.
- Para operaciones root cross-tenant, el header `SimulatedTenant:<id>` scopea el guard en dev/test;
  en producción la ruta real es el flujo de administración de tenants. En dev, el usuario `fotografo`
  (tenant 2, ver `../README.md`) es quien prueba los ABMs del vertical Fotos — `root` administra la
  plataforma pero el guard multi-tenant le impide cargar datos de negocio en su propio tenant.

#### Referencias cross-módulo (vertical → Base)

El vertical Fotos **puede** referenciar `Base.Domain` y navegar por EF a sus entidades
(`Usuario`/`Tenant`) — `ProjectReference` + navegación + FK real, nunca id-valor pelado ni SQL
cross-cutting. Lo que **no** se permite: referencias entre verticales (si en el futuro hay más de
uno), y `Base` nunca referencia un vertical (hay test de arquitectura que lo verifica por reflection,
`AcgFotos.Api.IntegrationTests/Architecture`).

#### Storage de fotos — separación originals/derived

Cualquier código que arme una **key de storage** (`IStorageProvider`) a partir de datos de
entrada (nombre de archivo subido, id de evento/grupo) tiene que pasar por los helpers existentes en
`AcgFotos.Fotos.Application.Storage`/`AcgFotos.Fotos.Infrastructure.Storage` — nunca concatenar el
nombre de archivo del usuario directo en una ruta. `originals/` y `derived/` son prefijos distintos
con reglas de acceso distintas (ver ADR-01/ADR-06 en `docs/04-decisiones.md`); un endpoint público
jamás debe poder construir o recibir una key bajo `originals/`.

#### EF Core — tamaños de columna

Al portar/crear un EF config, revisar `HasMaxLength`/nullability **contra la regla de negocio real**
que la valida. No heredar el número de otro config "porque estaba así" sin verificar el límite real.

#### Repositorios

- `IQueryable<T>` no sale del repo. El AppService consume métodos con nombre semántico
  (`Task<T>`, `Task<IReadOnlyList<T>>`, `Task<PaginationSet<T>>`).
- Reads → `AsNoTracking()` por default; `.AsSplitQuery()` opt-in por método con 2+ colecciones
  incluidas.
- Repo concreto se registra explícito en `AutofacModuleBase`/`AutofacModuleFotos` (no
  auto-discover) — ver `AutofacModuleFotos.cs` como referencia de qué se registra y por qué.

#### Manejo de excepciones

- No envolver en `Exception` plana (`throw new Exception(ex.Message)`) — pierde stack trace.
- `ExceptionHandlingMiddleware` ya tipa la respuesta según el tipo (`BusinessValidationException` →
  400). Dejar que se propague en vez de wrappear.
- Catch que silencia intencionalmente → comentario explícito con el motivo.

#### Dependency Injection

- Autofac, no el DI nativo de ASP.NET Core.
- `AutofacModuleBase` (plataforma) + `AutofacModuleFotos` (vertical Fotos). La convención por sufijo
  de `AutofacBaseModule` busca `<módulo>.Infraestructure` y no matchea `AcgFotos.Fotos.Infrastructure`
  — por eso los repos/servicios del vertical se registran explícitos en `Load()`, no por
  auto-discovery.

## Checklist: Nuevo endpoint / entidad del vertical Fotos

1. Entidad en `AcgFotos.Fotos.Domain/Entities/` (español, sin prefijo — ADR-10), hijos ricos del
   agregado en el mismo namespace.
2. EF config en `AcgFotos.Fotos.Infrastructure/Persistence/Ef/Configurations/` — revisar
   `HasMaxLength`/`IsRequired`/FKs contra la regla de negocio real (ver arriba), no copiar de otro
   config sin verificar.
3. Migración: `dotnet ef migrations add <Nombre> --project AcgFotos.Base.SqlMigrations --startup-project AcgFotos.Api`
   → aplicar con `dotnet ef database update` (mismos parámetros) contra la DB dev antes de dar por
   cerrado el cambio. El SQL crudo (vistas) se mantiene a mano en la migración.
4. DTOs en `AcgFotos.Fotos.Application/Dtos/` (Header/Detail/Input si el agregado lo justifica).
5. Mapper Mapperly (read) + write path (`SetValues`/`ChildCollectionSync`) en
   `AcgFotos.Fotos.Application/Mappers/`.
6. Si hay reglas de negocio que cruzan entidades o requieren consultar la base: método en el
   `ValidationService`/validator del agregado, invocado explícito desde `UpdateAsync` si el
   AppService overridea ese método — no asumir que un `FluentValidation` alcanza para eso (ver la
   trampa de arriba).
7. Repo en `AcgFotos.Fotos.Domain/Repositories/` (interfaz) +
   `AcgFotos.Fotos.Infrastructure/Persistence/Repositories/` (impl), registrado explícito en
   `AutofacModuleFotos`.
8. AppService en `AcgFotos.Fotos.Application/Services/` + `IServices/`.
9. Controller en `AcgFotos.Fotos.Controllers/Api/`, ruta `api/fotos/<recurso>`.
10. Si el endpoint queda root-only hasta que el front lo consuma: anotarlo; abrir permisos
    (`gen_PermisoEndpoints`) recién cuando el front lo use de verdad.
11. Test xUnit en `AcgFotos.Api.IntegrationTests/<Subsistema>/` (ver Testing) + actualizar
    `docs/05-notas-abiertas.md`/`docs/03-fases.md` si corresponde.

## Pull Requests

- Título descriptivo, en español.
- El PR debe compilar sin errores nuevos y sin bajar la suite de tests.
- Antes de abrir el PR: `dotnet build` + `dotnet test AcgFotos.Api.IntegrationTests` verdes, y si el
  cambio toca un flujo real (no solo unidad), un chequeo contra el host levantado (ver Testing).

## Testing

- **`AcgFotos.Api.IntegrationTests`** (xUnit): suite principal (419 tests), corre contra
  `AcgFotos_Tests` (se crea/migra sola; **Respawn** resetea entre tests, ignorando los lookups
  seedeados por migración). `dotnet test AcgFotos.Api.IntegrationTests`. Filtrar:
  `--filter "FullyQualifiedName~NombreDelTest"`.
- **Verificación manual real**: para cualquier fix de validación/seguridad, no alcanza con
  build+tests verdes — levantar el host (`dotnet run --project AcgFotos.Api --launch-profile http`)
  y probar el caso real con un request. Si se crea data de prueba en la DB dev durante la
  verificación, **limpiarla antes de cerrar** (`sqlcmd` directo si el endpoint de borrado no alcanza
  por FKs).

## Ambientes

| Ambiente | Config | Base de datos |
|---|---|---|
| `Development` (local) | `appsettings.Development.json` | `AcgFotos` (SQL Server local) |
| Tests | (Respawn, in-process) | `AcgFotos_Tests` |

Dev creds: `root` / `Root@AcgFotos2026!` (Administrador → bypassa authz) y `fotografo` / misma clave
(tenant 2, no-root — ver `../README.md`).

## Troubleshooting

| Problema | Solución |
|---|---|
| Un validator nuevo no rechaza nada | Confirmar que `CheckInputValidations` se invoque de verdad desde el `UpdateAsync` real (ver "FluentValidation — la trampa real" arriba) — no asumir por la existencia del validator. |
| Migración pendiente al aplicar EF config | `dotnet ef migrations add` contra `AcgFotos.Base.SqlMigrations`/`AcgFotos.Api`, después `dotnet ef database update`. |
| Build falla copiando DLLs (`MSB3027`/archivo bloqueado) | Un `dotnet run`/host quedó corriendo en background — matarlo (`taskkill /F /IM dotnet.exe` en Windows) antes de rebuildear. |
| 500 en vez de 400 ante un dato inválido | Falta el validator/chequeo en el punto de entrada. |
| Repo/servicio nuevo del vertical no se resuelve por DI | La convención por sufijo de `AutofacBaseModule` no matchea `AcgFotos.Fotos.Infrastructure` — registrarlo explícito en `AutofacModuleFotos.Load()`. |
