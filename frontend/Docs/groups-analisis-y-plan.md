# Grupos de Usuarios — Análisis y Plan de Migración

> Documento **trasladable**: análisis de la implementación existente (Budgeting / Raymos) + plan
> para implementarla "bien" en AcgFotos (PowerBI Embedded) como feature **general**.
> El mismo plan sirve para portar la mejora a Budgeting/Raymos.
> Estado: **EN PROGRESO** — se completa el detalle archivo-por-archivo y se actualiza durante la implementación.

## 1. Contexto y alcance

Un **grupo de usuarios** es un agregado con un `Nombre` y N miembros (usuarios). Hoy existe en:

- **Budgeting** (`c:\PROYECTOS\Budgeting\ApiGeneral`) — versión "rica", en uso real (ver §3).
- **Raymos / EBudgetMigra** (`D:\Proyectos\Raymos\EBudgetMigra-API`) — **misma copia de código**, sin usos downstream.
- Front original: `PowerBI Embedded\ClienteOriginal` y `Budgeting\Cliente` (ABM list + edit en diálogo, endpoint `api/budget/group`).

**Decisión:** se implementa **limpio en AcgFotos** (módulo `general`, no `budget`), siguiendo el patrón de `roles`.
En AcgFotos **NO** existen BPF / ApprovalCycle / DAP → el feature es solo **Grupo + UsuarioGrupo (miembros)**.
El análisis de Budgeting/Raymos queda como **referencia** para portar el mismo plan allá.
Corresponde a la fila `groups` **PENDIENTE** del `migration-log.md` (Fase 3).

## 2. Modelo (entidad → uso)

```
BudGroup (bud_Group)  ──< BudUserGroup (bud_UserGroup) >── Usuario
   Name, TenantId          UserId, GroupId, TenantId
```

En **Budgeting** el grupo además es sujeto de permisos en 3 subsistemas (en AcgFotos NO existen):

```
BudGroup ─┬─< BudApprovalCycleUserGroupNode  → ciclos de aprobación (quién aprueba)
          ├─< BudBPFUserGroup                → BPF (acceso al flujo por grupo)
          └─< BudGroupDAP                    → DAP (acceso a filas/datos por grupo)
```

Patrón de consumo común: `...Group.UserGroup.Any(ug => ug.UserId == userId)`.

## 3. Hallazgos del original (qué mejorar)

### Performance

- **P1** — `GetGroupById` carga y mapea **toda** la tabla y filtra el id en memoria (`GroupAppService.cs:77`).
- **P2** — `GetAll` (listado) pagina **en memoria** tras materializar y mapear todo con perfiles de usuario completos (`GroupController.cs:35-37` + `ListExtentions.Paginar`). El listado solo muestra id/nombre.
- **P3** — `GetAllFull` siempre hace `Include(UserGroup).ThenInclude(User)` (`GroupAppService.cs:100-103`), aun donde no se usan los usuarios.
- **P4** — N+1 en notificaciones de aprobación: `_dbContext.Usuarios.First(x => x.Id == user.UserId)` por usuario dentro de un `foreach` (`ApprovalCycleAppService.cs:571-599`).
- **P5** — Todo **síncrono** (`GeneralDbContext.Commit()` = `SaveChanges()`, no async).

### Buenas prácticas / correctness

- **B1** — Borrar un grupo en uso revienta con **FK 547** sin validación. FKs a `BudGroup` inconsistentes: `BudUserGroup`/`BudGroupDAP` = Cascade; `BudApprovalCycleUserGroupNode` y `BudBPFUserGroup` = RESTRICT (sin `OnDelete`).
  **Decisión (Alberto):** borrar el grupo debe **cascadear** a BPF y ApprovalCycle (DAP ya cascada) → poner `OnDelete(Cascade)` en las 4 FKs Group; **no** validar-y-bloquear. Implicancia asumida: borrar un grupo lo quita silenciosamente de los nodos de aprobación/BPF que lo referenciaban. _(En AcgFotos esto no aplica: solo existe `UsuarioGrupo`.)_
- **B2** — **Merge por reflexión frágil**: `EntityBaseRepository.Edit` → `UpdateEntityColllection` empareja colecciones **por índice posicional**; `BudGroup` ya tiene 4 colecciones, hoy anda de casualidad (las no-mapeadas llegan `null`). Es la "red legacy" que en AcgFotos se reemplazó por `ChildCollectionSync.SyncBy` explícito.
- **B3** — Falta **índice único `(TenantId, GroupId, UserId)`** → el front (manda todos con `id=0`) puede **duplicar** membresías.
- **B4** — Falta índice sobre `TenantId` pese al filtro global multi-tenant en toda query.
- **B5** — `Name` es `nvarchar(max)` **nullable** (de ahí el `x.Name != null` defensivo). Debe ser requerido y acotado.
- **B6** — `TransactionScope` redundante + mezcla `_dbContext` + `EntityRepository` + `UnitOfWork` para una sola unidad de trabajo.
- **B7** — El DTO devuelto tras editar mapea el `newEntity` de entrada, no la entidad persistida (`GroupAppService.cs:63`).
- **B8** — Restos de copy-paste: método controller `AddOrUpdateApprovalCycle` en endpoint de group; namespaces cruzados; `UserGroup` (sing.) vs `Usuario.UserGroups` (plural); schema `tbi` (Raymos) vs `dbo` (Budgeting).

## 4. Plan "hacerlo bien" (alineado a `roles`/`tipos-licencias`/`permisos` de AcgFotos)

### API

1. **DTOs separados**: `GrupoHeaderDto` (lista: `id`, `nombre`, `cantidadMiembros`), `GrupoInputDto` (alta/edición: `nombre`, `usuarioIds: long[]`), `GrupoOutputDto` (detalle). Sin perfiles de usuario embebidos.
2. **Listado paginado y proyectado en SQL** (`Skip`/`Take` + `Select` al header con `cantidadMiembros` por subquery). Sin `Include` de usuarios ni paginación en memoria.
3. **GetById** por query directa (`WHERE Id == id`).
4. **Create/Update** con **sync explícito** de membresías vía `ChildCollectionSync.SyncBy(...)` (no merge por reflexión).
5. **Servicio async** end-to-end; sin `TransactionScope`; un solo `SaveChangesAsync`.
6. **Read path con Mapperly** (`GrupoMapper`), no AutoMapper.
7. **Migración**: índice único `(TenantId, GrupoId, UsuarioId)`, índice en `TenantId`, `Nombre` requerido y acotado (100).
8. **Delete** simple (en AcgFotos solo cascada a `UsuarioGrupo`). **NOTA para portar a Budgeting/Raymos**: poner `OnDelete(Cascade)` en las FKs Group de `BudBPFUserGroup` y `BudApprovalCycleUserGroupNode` (decisión §B1).

### Front

- `features/groups/{domain,data,ui}` con `injectCrudClient`.
- Lista con `tbi-table` + `tbi-row-action` (borrado con spinner/confirm).
- Edit con `EditComponentBase` (form reactivo): `nombre` (required) + **selector múltiple de usuarios** (manda `usuarioIds: number[]`).
- Registrar ruta + `MENU_LABELS`/`MENU_ITEMS` + `allowedRoutesGuard`; gating por permiso de menú.
- Specs (service + componente), como el resto.

## 5. Variaciones respecto del original (contrato)

| Aspecto          | Original (Budgeting/Raymos)                    | AcgFotos (nuevo)                                                         |
| ---------------- | ---------------------------------------------- | ---------------------------------------------------------------------- |
| Endpoint         | `api/budget/group`                             | `api/general/grupos` _(o `groups` — ver §7)_                           |
| Lista            | `GroupDto` completo con `UserGroup` + perfiles | `GrupoHeaderDto` (`id`, `nombre`, `cantidadMiembros`), paginado en SQL |
| Update payload   | `userGroup: [{userId, groupId, id}]`           | `usuarioIds: long[]`                                                   |
| GetById          | carga toda la tabla y filtra en memoria        | query directa por id                                                   |
| Sync de miembros | merge por reflexión                            | `ChildCollectionSync.SyncBy` explícito                                 |
| Mapping          | AutoMapper (`CreateMap...ReverseMap`)          | Mapperly (`GrupoMapper`)                                               |
| Concurrencia     | síncrono + `TransactionScope`                  | async, sin `TransactionScope`                                          |
| `Nombre`         | `nvarchar(max)` nullable                       | requerido, acotado                                                     |
| Índices          | sueltos no-únicos en `UserId`/`GroupId`        | único `(TenantId, GrupoId, UsuarioId)` + índice `TenantId`             |
| Borrado          | FK 547 si está en uso                          | cascada a `UsuarioGrupo` (sin subsistemas en AcgFotos)                   |
| Front edit       | diálogo + `app-multiple-search-selector`       | `EditComponentBase` + selector múltiple de usuarios (ver §7)           |

## 6. Plan de implementación en AcgFotos (detalle archivo-por-archivo)

> Derivado del mapeo del feature de referencia `roles` (workflow `groups-techbi-blueprint`).
> Convención asumida (ver §7): **ES** → entidad `Grupo` + join `UsuarioGrupo`; tablas `gen_Grupos` / `gen_UsuarioGrupos`; API `api/general/grupos`; front `features/grupos`, ruta `/grupos`, `injectCrudClient('grupos')`.

**Decisiones de modelo confirmadas:**

- `Grupo : MultiTenantEntityBase` (lleva `TenantId`). **OJO:** `Rol` hereda `EntityBase` — **no** copiar a ciegas; los grupos son por tenant.
- Relación N:N como **join explícito** `UsuarioGrupo` (espejo de `UsuarioRol`), no skip-navigation.
- Base: `ExtendedEntityAppServiceBase` (5 genéricos) + `ExtendedEntityApiControllerBase` (6) — **no** las variantes `Entity*` de 4 genéricos.
- Sync de miembros: `ChildCollectionSync.SyncBy(grupo.UsuarioGrupos, dto.UsuarioIds, ug => ug.UsuarioId, id => new UsuarioGrupo { UsuarioId = id })`. Deduplica por `HashSet`.

### API — archivos nuevos (12)

1. `AcgFotos.Base.Domain/Entities/Grupo.cs` — `Grupo : MultiTenantEntityBase { string Nombre; ICollection<UsuarioGrupo> UsuarioGrupos; }` (colección inicializada en ctor).
2. `AcgFotos.Base.Domain/Entities/UsuarioGrupo.cs` — join `: MultiTenantEntityBase { long UsuarioId; Usuario Usuario; long GrupoId; Grupo Grupo; }` (espejo de `UsuarioRol`).
3. `AcgFotos.Base.Infrastructure/Configurations/GrupoEFConfig.cs` — `ToTable("gen_Grupos")`; `Nombre` `HasMaxLength(100).IsRequired()`; índice en `TenantId`; `HasMany(UsuarioGrupos).WithOne(Grupo).HasForeignKey(GrupoId).OnDelete(Cascade)`. **Comentario-NOTA de portabilidad** (ver §1/§3 B1).
4. `AcgFotos.Base.Infrastructure/Configurations/UsuarioGrupoEFConfig.cs` — `ToTable("gen_UsuarioGrupos")`; FK a `Usuario` `OnDelete(Cascade)`; **índice ÚNICO** `(TenantId, GrupoId, UsuarioId)`.
5. `AcgFotos.Base.Infrastructure/Repositories/IGrupoRepository.cs` — `PaginateHeadersAsync` + `GetByIdWithUsuariosAsync` (tracked) + `GetByIdWithUsuariosReadOnlyAsync`.
6. `AcgFotos.Base.Infrastructure/Repositories/GrupoRepository.cs` — `PaginateHeadersAsync` proyecta en SQL `CantidadMiembros = g.UsuarioGrupos.Count()` (sin `Include` de usuarios) y delega a `BuildPaginationAsync`.
7. `AcgFotos.Base.Infrastructure/Repositories/Projections/GrupoHeaderProjection.cs` — `{ Id, Nombre, CantidadMiembros }`.
8. `AcgFotos.Base.Application/Dtos/GrupoDto.cs` — detalle `: DtoBase { Nombre; ICollection<UsuarioGrupoDto> UsuarioGrupos; }`.
9. `AcgFotos.Base.Application/Dtos/UsuarioGrupoDto.cs` — `: DtoBase { GrupoId; UsuarioId; }` (sin `Usuario` embebido).
10. `AcgFotos.Base.Application/Dtos/GrupoInputDto.cs` — write `: DtoBase { Nombre; List<long> UsuarioIds; }` (no expone `TenantId`).
11. `AcgFotos.Base.Application/Dtos/GrupoHeaderDto.cs` — `: HeaderDtoBase { Nombre; int CantidadMiembros; }`.
12. `AcgFotos.Base.Application/Mappers/Mapperly/GrupoMapper.cs` — `[Mapper]` con `ToDto(Grupo)`, `ToHeaderDto(Grupo)`, `ToHeaderDto(GrupoHeaderProjection)`, `ToDto(UsuarioGrupo)`; `[MapperIgnoreSource]` para navegaciones + `TenantId`. (Auto-registrado por sufijo `Mapper`.)

Además: `IServices/IGrupoAppService.cs` (interfaz vacía sobre `IExtendedEntityAppServiceBase<...>`) y `Services/GrupoAppService.cs` (overrides `SearchAsync` → `PaginateHeadersAsync`+`MapItems`; `GetEntityToUpdateAsync`→tracked; `GetByIdAsync`→readonly+`ToDto`; `ToOutput`; `SyncCollections`) y `Controllers/Api/GrupoController.cs` (`[Route("api/general/grupos")]` sobre `ExtendedEntityApiControllerBase`, sin endpoints extra). (Auto-registrados por convención.)

### API — ediciones de cableado (3 + 1 opcional)

- `AcgFotos.Base.Infrastructure/Data/AcgFotosDbContext.cs` — `DbSet<Grupo> Grupos` + `DbSet<UsuarioGrupo> UsuarioGrupos` (sin DbSet, EF no genera la migración).
- `AcgFotos.Base.Infrastructure/Data/BaseDbConfiguration.cs` — `ApplyConfiguration(new GrupoEFConfig())` + `ApplyConfiguration(new UsuarioGrupoEFConfig())`.
- `AcgFotos.Base.Controllers/AutofacModuleBase.cs` — `RegisterType<GrupoRepository>().As<IGrupoRepository>().InstancePerLifetimeScope()`. (AppService y Mapper se auto-registran por sufijo.)
- `AcgFotos.Base.Domain/Entities/Usuario.cs` — **opcional** (ver §7): nav inversa `ICollection<UsuarioGrupo> UsuarioGrupos` (consistencia con `Usuario.Roles`).

### Migración

`dotnet ef migrations add AddGrupos --project AcgFotos.Base.SqlMigrations --startup-project AcgFotos.Api` → verificar el `.cs`: `gen_Grupos` (Id, Nombre nvarchar(100), **TenantId**), `gen_UsuarioGrupos` (Id, UsuarioId, GrupoId, **TenantId**; FKs Cascade), índice `TenantId` + **único** `(TenantId, GrupoId, UsuarioId)`. `Down` dropea en orden inverso. Aplicar con `database update`.

### Seed (`Api/Docs/postman/seed.sql`, idempotente)

- **Permiso**: reutilizar existente (no crear). `PermisoId=6` (admin del tenant) o `=1` (root). Ver §7.
- **Menú** (Id libre, p.ej. 30): `INSERT gen_Menus(... MenuPadreId=2 (ApplicationSettings), PermisoId, RoutePath='/grupos', Codigo='Grupos', ImagenWeb='groups' ...)`. `[Codigo]` debe coincidir con la clave de `MENU_LABELS`.
- (Opcional) Grupo de ejemplo + miembro para probar el ABM.

### Front — archivos nuevos

- `features/grupos/domain/grupo.model.ts` — `Grupo` (dual: `usuarioGrupos?` en respuesta, `usuarioIds?` en request; `cantidadMiembros?` en header) + `UsuarioGrupo`.
- `features/grupos/data/grupos.service.ts` — `injectCrudClient<Grupo>('grupos')` + `getUsuarios()` (inyecta `UsuariosService`, mapea a `TbiSelectOption<number>`).
- `features/grupos/ui/grupos-list.component.{ts,html,scss}` — `tbi-table` (cols id/nombre/cantidadMiembros) + `tbi-row-action` delete; abre `GrupoEditComponent`.
- `features/grupos/ui/grupo-edit.component.{ts,html,scss}` — `EditComponentBase<Grupo>`; form `{ nombre (required), usuarios: number[] }`; `toEntity`→`usuarioIds`; `patchForm`←`usuarioGrupos.map(usuarioId)`; sin `afterSave`.
- `features/grupos/ui/*.spec.ts` — list (smoke) + edit (patchForm marca usuarios; toEntity manda `usuarioIds`; required).
- `shared/ui/tbi-search-select/` — CVA: input `mat-autocomplete` (búsqueda server-side) + chips removibles; value `TbiSearchSelectItem[]` (id+label). + spec.

### Front — ediciones de cableado

- `src/app/app.routes.ts` — `{ path: 'grupos', loadComponent: () => import('./features/grupos/ui/grupos-list.component')... }` dentro del layout (guard ya heredado).
- `src/app/layout/menu/menu-item.model.ts` — `MENU_ITEMS` (`{ label, icon:'groups', route:'/grupos' }`) + `MENU_LABELS` (clave = `[Codigo]` del seed).
- Este doc — marcar checklist a medida que avanza.

### Notas de correctness / riesgo

- **Multi-tenant**: `TenantId` en ambas tablas; `SetTenantToChildItems` (base) asigna `TenantId` a las filas join nuevas tras `SyncCollections` — verificar que `UsuarioGrupo` lo reciba (si no, queda `TenantId=0` y el filtro global lo oculta).
- **Testing del alta**: root **no** simulado → `AllowSaveMultiTenantEntities()` false → falla con `ErrorTenantNotRoot`. Probar con usuario de tenant o root simulando tenant (memoria `seed-dev-sin-usuario-noroot` / `multitenant-impersonalizacion`).
- `CantidadMiembros` vía `Count()` en SQL: confirmar que el filtro multi-tenant no altere el conteo según el contexto.
- **Selector escalable (resuelto):** `tbi-search-select` hace **búsqueda server-side** (typeahead contra `usuarios?searchText=&pageSize=20`) — NO carga todo el padrón. Requirió 2 toques de API: filtro `searchText` en `UsuarioRepository.PaginateHeadersAsync` + nombres de los miembros en el detalle (`UsuarioGrupoDto` enriquecido en `GrupoAppService.GetByIdAsync`, para los chips en edición).

## 7. Decisiones abiertas (requieren OK)

- **Naming (ES vs EN)** — recomendado **full ES** por consistencia con `features/{roles,permisos,usuarios,parametros,...}`: `features/grupos`, ruta `/grupos`, `api/general/grupos`, entidades `Grupo`/`UsuarioGrupo`. _(El prompt del mapeo usó `groups`; consistencia ⇒ `grupos`.)_ — **RESUELTO: full ES (`grupos`)**
- **Join explícito** `UsuarioGrupo` (vs skip-nav) — **RESUELTO: explícito** (patrón `UsuarioRol`; permite `TenantId` + índice único).
- **Nav inversa `Usuario.UsuarioGrupos`** — recomendado **SÍ** (consistencia con `Usuario.Roles`). — _default SÍ salvo objeción_
- **Componente selector de usuarios** — **RESUELTO: `tbi-search-select`** (multi-select con búsqueda server-side: `mat-autocomplete` + chips). Reemplazó al `tbi-multi-select` estático inicial, que no escalaba (cargaba todo el padrón). Reutilizable para elegir N entidades por búsqueda.
- **Permiso del menú** — admin de tenant (`PermisoId=6`, gral_admin) vs root-only (`PermisoId=1`). — **RESUELTO: admin del tenant (`PermisoId=6`)**
- **Icono**: `groups` (Material). — _default_
- **Índice único / EF**: `HasName` (legacy, como `UsuarioRolEFConfig`) vs `HasDatabaseName` — verificar versión EF al implementar. — _no bloqueante_

## 8. Checklist

- [x] API: entidad + join + EF config + índices (`Grupo`, `UsuarioGrupo`, `GrupoEFConfig`, `UsuarioGrupoEFConfig`)
- [x] API: DTOs Header/Input/Output (`GrupoHeaderDto`, `GrupoInputDto`, `GrupoDto`, `UsuarioGrupoDto`)
- [x] API: `GrupoMapper` (Mapperly)
- [x] API: AppService async + sync explícito + listado paginado SQL + getById directo
- [x] API: controller `api/general/grupos`
- [x] API: registro DbSet + DI/AutoFac + nav inversa `Usuario.UsuarioGrupos`
- [x] API: migración `AddGrupos` (índice único + `TenantId`, `Nombre` nvarchar(100), FKs Cascade) — aplicada a dev
- [x] API: seed (menú `Grupos`, `PermisoId=6`, `/grupos`) — aplicado a dev
- [x] Front: `features/grupos/{domain,data,ui}` + `tbi-search-select` (búsqueda server-side)
- [x] Front: ruta `/grupos` + `MENU_ITEMS`/`MENU_LABELS` (guard heredado)
- [x] Front: specs (7 nuevos)
- [x] Verificación: build prod + lint + stylelint + prettier + 181 tests verdes (API build 0 errores)
- [ ] PR(s) (API + front) — pendiente
- [ ] e2e de browser (verificación manual de Alberto)

## 9. Modelo de permisos efectivos (directos ∪ grupos) ∩ licencia

**Decisión (confirmada con Alberto):** los **roles efectivos** de un usuario son los asignados
**directamente** (`gen_UsuarioRoles`) **∪** los heredados de sus **grupos** (`gen_UsuarioGrupos` →
`gen_GrupoRoles`), **acotados (∩) por los roles de su licencia activa** (ver **§10.3** — la licencia
es TOPE DURO para usuarios cliente/no-root; root exento). Fórmula:
`efectivos = (directos ∪ grupos) ∩ roles-de-su-licencia`. Los permisos salen de esos roles
(`gen_RolPermisos`).

Las dos vías de asignación (directa y por grupo) son **opcionales** y aditivas entre sí (suman, no
restan), **pero la licencia las acota**: un usuario nunca puede tener efectivo un rol fuera de su
licencia, venga de donde venga. La "resta"/claridad y el tope se reflejan en la **visualización de
roles efectivos** (read-only: origen _directo_/_grupo X_ + marca de _bloqueado por licencia_).

> Token vs server-side: la autorización se resuelve **server-side** (siempre fresca + revocable), no
> en el JWT. Hornear permisos en el token reintroduce staleness (valdrían hasta el refresh) — el JWT
> lleva solo identidad (`sub`/`tenant`/`isRoot`). La optimización de lectura del authz server-side la
> define **ADR-0003** (caché **versionado** por `authzVersion`: la clave se invalida sola cuando
> cambian los permisos), **no** el caché de **30 s** que hay hoy en `EndpointAuthoritation` — ese TTL
> de 30 s es **legacy y está pendiente de reemplazo** (ver §10.4). No se mete en el token.

### Fuente única de verdad: vista `vw_UsuarioRolesEfectivos`

La unión vivía **duplicada** en dos lugares (riesgo de drift): el filtro de autorización de endpoints
(`SecurityRepository.GetAll`, en `AcgFotos.Core`, SQL crudo + caché) y el armado del menú
(`UsuarioRepository.GetEffectiveRolIdsByUserIdAsync`, en `AcgFotos.Base`, EF). **Core no depende de
Base**, así que no pueden compartir un método C#. Solución: **una vista SQL** que define la unión
**una sola vez**; ambos la consumen.

```sql
CREATE VIEW dbo.vw_UsuarioRolesEfectivos AS
  SELECT ur.UsuarioId, ur.TenantId, ur.RolId, CAST('Directo' AS varchar(10)) AS Origen,
         CAST(NULL AS bigint) AS GrupoId
  FROM dbo.gen_UsuarioRoles ur
  UNION ALL
  SELECT ug.UsuarioId, ug.TenantId, gr.RolId, CAST('Grupo' AS varchar(10)) AS Origen, ug.GrupoId
  FROM dbo.gen_UsuarioGrupos ug
  INNER JOIN dbo.gen_GrupoRoles gr ON gr.TenantId = ug.TenantId AND gr.GrupoId = ug.GrupoId;
```

Lleva `Origen`/`GrupoId` para **doble uso**: autorización (consumidores hacen `DISTINCT RolId`) y, a
futuro, la **visualización de roles efectivos** (lee las filas con su origen). Si esa pantalla pidiera
muchos joins de descripciones, podrá tener su propio query; la vista no es "para un ABM", es del motor.

### Performance y buenas prácticas (clave)

- **`UNION ALL`** (no `UNION`): evita un `DISTINCT` interno inútil; la dedup la hacen los consumidores
  cuando corresponde (`DISTINCT RolId` en autorización). Por `UNION ALL`, la vista **no es
  materializable/indexable** (los indexed views de SQL Server prohíben `UNION [ALL]`) → la performance
  depende de los **índices de las tablas base**, no de la vista.
- **Índices que cubren la vista** (analizados sobre los existentes):
  - Branch directo: `uk_usuario_rol (UsuarioId, RolId)` → seek por `UsuarioId` con `RolId` en el índice
    = **covering**, sin lookups.
  - Join de grupos: el `INNER JOIN ... ON gr.TenantId = ug.TenantId AND gr.GrupoId = ug.GrupoId` usa
    `uk_tenant_grupo_rol (TenantId, GrupoId, RolId)` = **covering** (trae `RolId`).
  - **Gap corregido:** `gen_UsuarioGrupos` solo tenía `uk_tenant_grupo_usuario (TenantId, GrupoId,
UsuarioId)` → filtrar por `UsuarioId` (lo que hace la vista) **no podía hacer seek** (UsuarioId es
    la última columna). Se agrega índice **`ix_usuariogrupo_usuario (UsuarioId) INCLUDE (GrupoId,
TenantId)`** → seek por `UsuarioId` + covering del join.
- Con eso, cuando el consumidor pide solo `RolId` (autorización y menú) la vista resuelve **sin key
  lookups** en ninguna rama.
- Columnas explícitas y tipadas (`CAST` del literal `Origen` y del `NULL` de `GrupoId`); sin `SELECT *`;
  sin `SCHEMABINDING` (no aplica indexed view + no atar las tablas base).
- Consumo: EF la mapea como **entidad keyless `ToView`** (`UsuarioRolEfectivo`), nunca trackeada y
  fuera de migraciones (la vista la crea/borra la migración por SQL). Filtra por `UsuarioId` (que ya
  determina el tenant del usuario) — no necesita el filtro multi-tenant global.

## 10. Tareas / validaciones pendientes

### 10.1 Validar la autorización de ENDPOINTS — ✅ VALIDADO EN DEV (2026-06-10)

El circuito de endpoint-authz (`EndpointAuthoritation` → `SecurityRepository.GetAll` → vista
`vw_UsuarioRolesEfectivos`) **se probó completo en dev** (rama Api `feature/endpoint-authz-discovery`,
ADR-0004): API con `AuthorizationEnabled=true` por **env var** (sin tocar appsettings), catálogo
poblado con `GET /api/general/discover` (root) y seed `Docs/postman/seed-permiso-endpoints.sql`
(idempotente: `gral_cli` base / `gral_admin` admin / impersonalización sólo-root).

Resultados (matriz HTTP real, tokens firmados con la key de dev):

- Usuario `test` (rol **solo por grupo**, licenciado): 200 en GETs (menús, usuarios, mi-perfil,
  `tenants/{id}/administradores`) y carve-outs; **401** en auditoría, escrituras admin y `/discover`.
  El `DELETE` denegado corta **antes** del action (no muta nada).
- Usuario `admintest` (rol solo por grupo, **fuera de su licencia**, `PermitidoPorLicencia=0`):
  **401 en todo** → la licencia es tope duro también en endpoint-authz. ✔
- Admin cliente (`test5`): 200 en auditoría, pasa authz en escrituras; `/discover` 401 (sólo root).
- Frescura del caché versionado (ADR-0003) en vivo: revocar mapeo + bump → 401 inmediato; restaurar
  - bump → 200.

**Tarea — PENDIENTE (resta realizarse en qa/prd):** validar el endpoint-authz en **qa/prd** (flag on +
tablas pobladas): un usuario cuyo rol venga **solo por grupo** debe poder pegar a los endpoints de ese
rol y recibir 401 en el resto; y un rol de grupo **fuera de su licencia** debe quedar **bloqueado**
(tope duro). No se puede anticipar en dev por lo anterior; queda como gate previo a prod.

### 10.2 Sincronizar con Raymos **y Budgeting** (bidireccional)

El modelo de roles efectivos quedó **mejor acá** (unión por grupos + **tope de licencia** vía la
vista única). Hay que **sincronizarlo en los dos derivados**:

- **Raymos** (`EBudgetMigra-API`) y **Budgeting** (`ApiGeneral`): su
  `Mvz.Fwk/Security/SecurityRepository.GetAll` resuelve los roles **solo desde `gen_UsuarioRoles`
  (directos)** — **NO** la unión por grupos **ni** el tope de licencia. Portar a ambos el mismo
  modelo (`vw_UsuarioRolesEfectivos` + `PermitidoPorLicencia` o equivalente) en `SecurityRepository`
  **y** en la resolución de menú. Ojo esquema/nombres: Raymos usa `tbi.` (no `dbo.`) y `tbi.usuarios`
  (no `gen_Usuarios`).
- ~~**A REVISAR (traer DE Raymos)**: mejor captura/validación de endpoints~~ — **HECHO (2026-06-10,
  superado)**: se analizó el patch de Raymos (commit `6754b38`: leer `HttpMethodAttribute.Template`)
  y en AcgFotos se fue un paso más allá: discovery desde `IActionDescriptorCollectionProvider` con
  mapper compartido con el filtro (match por construcción, sin drift posible) — ver **ADR-0004** en
  la Api. Queda como **backport candidato → Raymos** (reemplazar su patch de reflexión por esto).

Foco actual: cerrar bien **grupos en AcgFotos**; la sync a los derivados es trabajo posterior.

### 10.3 La licencia es TOPE DURO (intersección) — REQUERIDO (bug actual)

**La implementación actual es aditiva pura (`efectivos = directos ∪ grupos`) y NO intersecta con la
licencia del usuario → es un AGUJERO DE SEGURIDAD** (decisión de Alberto: inaceptable). Un grupo le
otorga a un miembro un rol que **su** licencia no incluye, y hoy ese rol queda efectivo (menú/authz).

**Caso real (dev):** admintest tiene licencia "Admin del Tenant Cliente" (habilita solo el rol
"Administrador Cliente"), pero por el grupo "GRUPO ADMINS" recibe "Planificador Editor" (rol de la
licencia "Planificador", que admintest NO tiene) → hoy tendría poderes de Planificador **sin licencia**.

**Modelo (IMPLEMENTADO 2026-06-08):** `efectivos = (directos ∪ grupos) ∩ roles-de-la-licencia-activa-del-usuario`.
La licencia es el techo de lo que un usuario puede hacer, **sin importar de dónde venga el rol** (directo
o grupo). Aplica a **menú y autorización de endpoints** (ambos resolvers).

**Decisiones (resueltas con Alberto):**

- **Alcance**: capar **todo** rol efectivo (directos ∪ grupos) ∩ licencia. ✓
- **Root exento**: root no pasa por esta resolución (menú por `PermisoRoot`; authz `CheckPemissions()`
  = false para root real). El tope aplica a usuarios **cliente/no-root** (incl. impersonados). ✓
- **Roles huérfanos** (en ninguna licencia): para no-root **caen** (estricto: solo vale lo que la
  licencia incluye). Para root no aplica (exento). ✓

**Cómo quedó implementado:**

- La vista `vw_UsuarioRolesEfectivos` expone el flag **`PermitidoPorLicencia`** (`1` si el rol está en
  la licencia activa del usuario — `gen_UsuarioTipoLicencia` IsActive → `gen_TipoLicenciaRoles`).
  Migración `AddLicenciaCapToVwUsuarioRolesEfectivos` (recrea la vista), aplicada a dev.
- Los resolvers de **menú** (`GetEffectiveRolIdsByUserIdAsync`) y **authz** (`SecurityRepository.GetAll`)
  filtran `PermitidoPorLicencia = 1` (capan no-root).
- **Visualización** (tab "Roles efectivos"): devuelve **todos** los roles + el flag; la UI marca los
  bloqueados ("Bloqueado por licencia", tachado).
- **Verificado en vivo (2026-06-08):** admintest (licencia "Admin del Tenant Cliente") con un rol de
  grupo NO licenciado para él → `menus/principal` **vacío** (bloqueado). Con un rol licenciado → aparece.

> Nota perf: el flag es un `EXISTS` correlacionado por fila contra la licencia activa (tablas chicas).
> Si hiciera falta, indexar `gen_UsuarioTipoLicencia(UsuarioId, IsActive)` + `gen_TipoLicenciaRoles(TipoLicenciaId, RolId)`.

### 10.4 Caché de authz versionado (ADR-0003 §6.3) — ✅ IMPLEMENTADO (2026-06-09)

El **ADR-0003** (`Api/Docs/adrs/0003-seguridad-por-grupos.md`) decidió **reemplazar el caché de
endpoints de `EndpointAuthoritation`** (expiración **absoluta de 30 s**, ya retirada) por un **caché
versionado**: la clave es `endpoints:{usuario}:{tenant}:{authzVersion}` y `authzVersion` (tabla
`gen_AuthzVersion`, 1 fila) se **incrementa al guardar cualquier entidad de autorización** → la frescura
es **al instante** (no hasta 30 s) y **no recomputa si nada cambió**, con TTL **deslizante** (~1 h, config
`Authorization:CacheSlidingMinutes`) solo para higiene de memoria.

**Cómo quedó implementado (rama API `feature/authz-cache-versionado`):**

- **`gen_AuthzVersion`** (entidad `AuthzVersion`, NO multi-tenant; migración `AddAuthzVersion` con seed
  `Id=1, Version=0`, aplicada a dev).
- **Read-side** `IAuthzVersion.Get()` (`AcgFotos.Core/Security`, impl `AuthzVersionReader` por SQL crudo
  sobre la conexión cross-cutting, mismo patrón que `SecurityRepository` — Core no tiene EF de Base).
  `EndpointAuthoritation` lo inyecta, arma la clave versionada, cachea un **`HashSet<string>` de firmas**
  de endpoints (chequeo O(1), antes era un `.Any()` lineal) con `SlidingExpiration`.
- **Bump transaccional en `AcgFotosDbContext.SaveChanges/SaveChangesAsync`** (no `Bump()` disperso por
  AppService): se sobreescriben los `SaveChanges*`, se escanea el `ChangeTracker` (por `Metadata.ClrType`)
  y, si hay cambios en una entidad de autorización, se incrementa la fila `AuthzVersion` **dentro del mismo
  `SaveChanges`** → atómico con el cambio que lo dispara y **imposible de olvidar** en código nuevo.

> **Divergencia deliberada vs. el ADR (mejora):** el ADR proponía `IAuthzVersion { Get(); Bump(); }` con
> `Bump()` llamado desde los AppServices de autorización. Se eligió **bump centralizado en el DbContext**
> (vía `ChangeTracker`) en lugar de esparcir llamadas `Bump()`: es atómico (misma transacción), no puede
> olvidarse al sumar features, y deja `IAuthzVersion` como **solo lectura**. El set de entidades que
> disparan bump es un **superset** del listado del ADR §6.3: suma `UsuarioTipoLicencia` y
> `TipoLicenciaRoles` porque la **licencia es TOPE DURO** (§10.3, posterior al ADR) — cambiar la licencia o
> sus roles cambia qué endpoints ve el usuario.

**Verificado (dev, 2026-06-09):** build 0 errores; migración aplicada; fila semilla `Id=1, Version=0`.
**Bump verificado e2e** (no depende del flag de authz): logueado como root, dos escrituras authz
(`roles/{id}/set-default-tenant` ida y vuelta) subieron `gen_AuthzVersion.Version` **+1 cada una**, y un
`GET` de roles **no la movió** (0). La **clave versionada en authz** sí queda como gate de qa/prd (mismo
gotcha de §10.1: `AuthorizationEnabled=false` + `gen_Endpoints` vacías en dev).

> **Bug cazado por la prueba e2e (ya corregido):** la primera versión sobreescribía las **cuatro**
> sobrecargas de `SaveChanges*`; como EF hace que `SaveChangesAsync(ct)` delegue en
> `SaveChangesAsync(bool, ct)`, el bump se contaba **doble** (+2 por escritura). Fix: dejar el bump **solo**
> en las sobrecargas con `acceptAllChangesOnSuccess` (el embudo canónico de EF) → +1. El build no lo
> detectaba; lo agarró el test de runtime.

### 10.5 Mensaje para root en la tab "Ver Roles efectivos" — ✅ IMPLEMENTADO (2026-06-09)

La tab muestra los roles efectivos del usuario editado con su marca de **bloqueado por licencia**. Para
un usuario **root** el tope de licencia **no aplica** (ver §10.3, "Root exento": menú por `PermisoRoot`,
authz `CheckPemissions()`=false), así que mostrarle roles "bloqueados" sería **engañoso**. Cuando el
usuario es root, la tab muestra un **mensaje** ("el tope de licencia no aplica, conserva todos sus roles")
y **oculta las marcas** de bloqueo.

**Cómo se resolvió el "¿es root el usuario editado?" SIN cambio de back** (rama
`feature/usuario-root-tope-licencia`): se gatea con **`AuthStore.isRoot()` del usuario LOGUEADO**, no con
un flag del editado. Vale por una equivalencia del modelo actual:

- root es **por-tenant** (`isRoot = tenantId == RootTenantId`), así que **todo** usuario del tenant root
  es root por definición (no puede haber un no-root ahí);
- root sólo edita usuarios de **otros** tenants **impersonando**, y el JWT scopeado (ADR-0002) hace que
  ahí `isRoot` pase a `false`.

⇒ **viewer `isRoot()=true` ⟺ todos los usuarios que está editando son root.** Es **cosmético** (la
exención real es server-side), por eso el atajo es seguro. Implementación: en `usuario-edit.component.html`
el hint y la marca `Bloqueado por licencia` se condicionan con `isRoot()` (`!isRoot() && !permitidoPorLicencia`).
+2 specs (activan la tab: no-root muestra la marca, root la oculta y muestra el mensaje).

> **Descartadas** (innecesarias con el atajo): exponer `esRoot` en `UsuarioDto`, o hardcodear `tenantId===1`
> en el front. **Revisar** este atajo el día que root deje de ser **por-tenant** (p.ej. un flag root
> por-usuario dentro del tenant root): ahí habría que exponer el dato del editado desde el back.

## 11. Mejoras futuras (post-cierre, ideas de Alberto)

> Se encaran **después** de que grupos quede sólido (validación en qa incluida).

- **11.1 Selector de roles del grupo etiquetado por tipo de licencia — ✅ IMPLEMENTADO (2026-06-09).**
  Cada rol del selector (`grupo-edit`) muestra **chip(s)** de la(s) licencia(s) del tenant que lo
  incluyen, para que el admin entienda de qué licencia viene cada rol. Se eligió **chips por rol** (no
  agrupar): un rol puede estar en varias licencias y no es partición estricta.
  - **API:** `roles/del-tenant` pasó de serializar la entidad `Rol` a un DTO `RolDelTenantDto`
    (`id`, `descripcion`, `licencias: [{id, descripcion}]`). La proyección (`RolDelTenantProjection`,
    repo `GetDelTenantConLicenciasAsync`) arma en **SQL** los roles licenciados del tenant + sus
    licencias, **limitando los chips a las licencias del tenant** (no las ajenas). Sin N+1, sin migración.
  - **Front:** `RolOption.licencias` + `mat-chip-set` por rol en `grupo-edit`. +1 spec. Build API 0
    errores; 187 tests + lint + stylelint + build:prod verdes. (Ramas `feature/grupos-roles-chips-licencia`
    API+Cliente.) Conecta con 11.2.
- **11.2 Alerta de mezcla de licencias — ✅ IMPLEMENTADO (alerta blanda, 2026-06-10).** Se eligió la
  **opción (a) alerta blanda** (no la restricción dura (b)) y **mensaje genérico** (sin nombrar las
  licencias, por ahora). En `grupo-edit`, si los miembros seleccionados tienen **≥2 licencias activas
  distintas**, aparece un aviso: _"Estás mezclando usuarios con distintas licencias. Cada miembro solo
  recibe los roles del grupo que su licencia incluye."_ Recalculado en vivo al agregar/quitar miembros.
  - **API:** el detalle del grupo (`grupos/{id}`) enriquece cada miembro con su `usuarioTipoLicenciaActivaId`
    (`UsuarioGrupoDto`; repo `GetActiveTipoLicenciaIdByUsuarioIdsAsync`, una query). El buscador de
    usuarios ya traía la licencia (`UsuarioHeaderDto.TipoLicenciaActiva`).
  - **Front:** mapa `usuarioId → licencia` que se llena con el buscador y con los miembros cargados;
    `mezclaLicencias` (computed) dispara el aviso. +2 specs (mezcla sí / no). Build API 0 err; 189 tests +
    lint + stylelint + build:prod. (Ramas `feature/grupos-alerta-mezcla-licencias` API+Cliente.) Sólo en
    el **ABM de edición** (no en el listado) y **mensaje genérico**, según se acordó; el listado y/o
    nombrar las licencias quedan como mejora si hace falta. **NO verificado e2e en vivo** (requería armar
    un grupo con miembros de distinta licencia; cubierto por unit tests con render real).
  - _Pendiente de §11.2 (no hecho): declarar un "tipo" de grupo por licencia y la variante (b) restricción
    dura. La alerta (a) suele alcanzar; con el tope de licencia ya implementado, mezclar es inofensivo a
    nivel seguridad (el rol queda bloqueado a quien no le corresponde), sólo confuso._
- **11.3 (posible evolución) Tab "Ver Roles efectivos" como simulador ("Ver roles como").** Hoy es
  read-only del estado actual y, desde 2026-06-09, ya es **reactiva** a lo que se toca en el form sin
  guardar (cambiar la licencia o los roles directos se refleja al instante en la tab — ver §6 punto 6).
  El siguiente paso sería un what-if explícito: "¿qué pasaría si le cambio la licencia / lo saco del
  grupo X?". Es la semilla de un **simulador de roles**.
