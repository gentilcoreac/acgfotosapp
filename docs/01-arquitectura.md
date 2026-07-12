# Arquitectura

## Punto de partida: código base propio

El proyecto NO arranca de cero: reutiliza el código base de `C:\PROYECTOS\CodigoBase` (API .NET +
Cliente Angular), renombrado a `AcgFotos.*` y sin el vertical Budget (ver ADR-09). Eso trae resuelta
toda la **plataforma**: autenticación JWT + refresh tokens, usuarios/roles/permisos/grupos, menús
dinámicos, multi-tenant, auditoría, rate limiting, logging (Serilog), theming por tenant y un shell
Angular 22 moderno (zoneless, signal forms, rxResource). Lo que se construye para AcgFotos es el
**vertical Fotos** sobre esa plataforma.

## Vista general

```
[Familia móvil]──┐
                 ├──▶ Angular 22 SPA ──▶ AcgFotos.Api (ASP.NET Core .NET 10) ──▶ SQL Server
[Admin desktop]──┘                          │
                                            ├──▶ IStorageProvider
                                            │      ├─ FileSystem (dev)
                                            │      └─ S3-compatible / R2 (prod, a implementar)
                                            │           ├─ originals/  (nunca expuesto al público)
                                            │           └─ derived/    (previews con watermark, thumbs)
                                            └──▶ Mercado Pago (fase 3)
```

Monolito **modular**: plataforma `Base` + verticales que se enchufan por módulos Autofac y
`AppModulesName` (config). Sin colas ni microservicios (ADR-04).

## Backend — estructura real

```
backend/
  AcgFotos.Core/                → framework transversal (auth, storage, email, excel, logs, i18n...)
  AcgFotos.Base.Domain/         → entidades de plataforma (Usuario, Rol, Tenant, Menu...)
  AcgFotos.Base.Application/    → AppServices + DTOs + validadores de plataforma
  AcgFotos.Base.Infrastructure/ → EF Core (AcgFotosDbContext), repos, configs
  AcgFotos.Base.Controllers/    → API de plataforma + AutofacModuleBase
  AcgFotos.Base.SqlMigrations/  → migraciones EF (historial lineal, único para todos los módulos)
  AcgFotos.Api/                 → host (Startup, appsettings, Swagger) — corre en :30000
  AcgFotos.Api.IntegrationTests/→ xUnit + Respawn contra AcgFotos_Tests (419 tests)
```

El vertical **Fotos** seguirá el mismo patrón que tenía Budget en el código base:
`AcgFotos.Fotos.{Domain,Application,Infrastructure,Controllers}` + `AutofacModuleFotos` + alta en
`AppModulesName`. Reglas del DAG de módulos: un vertical puede referenciar `Base`; `Base` jamás
referencia un vertical; verticales no se referencian entre sí (hay test de arquitectura que lo
verifica por reflection).

### Patrones obligatorios heredados (resumen; el detalle vive en el código)

- Flujo `Controller → AppService → Repository/DbContext`, con `ValidationService` para reglas que
  cruzan entidades. `ExtendedEntityApiControllerBase` da CRUD + paginación gratis.
- Multi-tenant: entidades heredan `MultiTenantEntityBase`; el estampado/guard de `TenantId` vive
  centralizado en `AcgFotosDbContext.SaveChanges` (no estampar a mano).
- FluentValidation: un validator NO corre por existir; si un AppService overridea `UpdateAsync`
  debe llamar `CheckInputValidations(dto)` explícitamente.
- `IQueryable` no sale de los repos; reads con `AsNoTracking()`.
- Migraciones: `dotnet ef migrations add <Nombre> --project AcgFotos.Base.SqlMigrations --startup-project AcgFotos.Api`.
  OJO: el SQL crudo (vistas como `vw_UsuarioRolesEfectivos`) no sobrevive a un squash/regeneración —
  mantenerlo a mano en la migración.

## Frontend — estructura real

```
frontend/src/app/
  core/      → auth (JWT+refresh, guards, impersonation), http (ApiClient, injectCrudClient), config
  shared/    → EditComponentBase (signal forms), controles tbi-*, feedback
  layout/    → main-layout (shell de ADMINISTRACIÓN: sidenav + menús dinámicos por permisos)
  features/  → una carpeta por feature, lazy (usuarios, roles, tenants, ... + fotos/ a crear)
```

- Angular 22 **zoneless**, OnPush, standalone, signal forms, `rxResource` para fetches reactivos.
- La zona de **familias** (galería/carrito) será un layout propio mobile-first fuera de
  `main-layout`, con sesión por código de álbum (no JWT de usuario).
- Dev server :4200 contra API :30000. Node ≥22.22.3 (portátil en `.tools/` si el global es menor).

## Base de datos: SQL Server

El código base está construido sobre SQL Server (`DatabaseFactory` → `UseSqlServer`, Serilog a
`gen_LogInfos`, Respawn SqlServer). Se mantiene (ADR-09): en dev SQL Server local
(`Server=localhost;Database=AcgFotos;Integrated Security=True`), tests contra `AcgFotos_Tests`
(se crea y migra sola). Tablas de plataforma con prefijo `gen_`; las del vertical Fotos usarán `fot_`.

## Storage de fotos

`AcgFotos.Core.Storage.IStorageProvider` ya existe con implementación `FileSystem` (config
`Storage:Provider`). Para las fotos del vertical:

- **Dev**: FileSystem (sin Docker ni MinIO — decisión simplificada respecto del plan original).
- **Prod**: implementación S3-compatible (Cloudflare R2 por costo de egreso ≈ 0) con URLs firmadas
  de expiración corta. Se agrega como `S3StorageProvider` cuando llegue la fase de deploy.
- Layout de claves y separación estricta `originals/` vs `derived/`: ver ADR-06.

## Seguridad de las imágenes (premisa central — ver ADR-01)

| Amenaza | Mitigación |
|---|---|
| Captura de pantalla / foto a la pantalla | Watermark sobre toda la imagen + baja resolución (capa 1); bloqueo real vía app Capacitor con `FLAG_SECURE` en Android (capa 3, post-MVP) |
| Descarga de la imagen mostrada | Es el preview con watermark; fricción extra en la web (capa 2) |
| Adivinar URLs | Storage privado + URLs firmadas + IDs no secuenciales |
| Acceso a álbum ajeno | Código → token de sesión acotado al álbum; la API filtra todo por ese álbum; rate-limiting ya presente en la plataforma |
| Robo del original | `originals/` solo accesible por endpoints admin autenticados |

## Autenticación

- **Admin (fotógrafo)**: la plataforma heredada completa — login usuario/contraseña, JWT + refresh
  token con rotación, lockout por intentos, roles/permisos por endpoint.
- **Familia**: canje de código de álbum → JWT de corta duración con claim `albumId` (a construir en
  el vertical; reutiliza la infraestructura JWT existente). Sin registro.

## Pagos (fase 3)

Mercado Pago Checkout Pro + webhook. Alternativa siempre disponible: pago en efectivo en la entrega.

## Hosting (decidir al final de Fase 2)

Requiere Windows/Linux con SQL Server o SQL Azure — evaluar costo contra VPS + SQL Server Express
(gratis hasta 10 GB, sobra para este volumen). Las fotos siempre en storage externo (R2), nunca en
el disco del servidor. HTTPS obligatorio.
