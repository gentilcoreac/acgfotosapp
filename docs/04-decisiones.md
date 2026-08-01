# Decisiones de diseño (ADRs)

Formato corto: contexto → decisión → consecuencias. Si una decisión se revierte, no borrarla: marcarla "Reemplazada por ADR-XX".

## ADR-01 — Protección de imágenes por capas: watermark siempre + bloqueo de capturas donde la plataforma lo permite

**Contexto**: la premisa es dificultar al máximo la captura de pantalla. La foto sacada a la pantalla con otro celular se acepta como riesgo residual (el watermark la neutraliza). Realidad técnica por plataforma:

- **Navegador (web pura)**: NO existe API para impedir ni detectar capturas. Solo fricción cosmética (bloquear clic derecho, arrastre e impresión). DRM tipo Widevine para ennegrecer capturas es inviable para fotos (complejidad enorme, comportamiento inconsistente por dispositivo) — descartado.
- **App Android** (incluso híbrida): `FLAG_SECURE` hace que capturas y grabaciones de pantalla salgan **negras**. Protección real. Android es ~85–90% de los celulares en Argentina.
- **App iOS**: no se puede impedir; sí se puede **detectar** la captura a posteriori y reaccionar (avisar/ocultar).

**Decisión** — tres capas, de la que siempre funciona a la más fuerte:

1. **Watermark automático del sistema + baja resolución + originales nunca expuestos** (defensa base, cubre el 100% de los casos incluida la foto a la pantalla). El watermark lo aplica SOLO el sistema al subir: el fotógrafo sube el original limpio (necesario para imprimir) y el sistema genera los derivados marcados. Costo marginal ≈ cero (CPU), sin trabajo manual.
2. **Fricción en la web**: deshabilitar clic derecho, arrastre e impresión de la galería. Documentado como disuasión, no como seguridad.
3. **App móvil híbrida (Capacitor sobre el mismo Angular)** con `FLAG_SECURE` en Android y detección de captura en iOS. Es la vía real de "bloquear la captura" para la mayoría de los usuarios. Planificada post-MVP (ver fases); la web queda como fallback para quien no instale la app.

**Consecuencias**: el MVP sigue siendo web (sin fricción de instalación para validar el negocio); el pipeline de derivados es núcleo del sistema; la capa 3 exige mantener el frontend "empaquetable" con Capacitor (evitar dependencias solo-navegador sin fallback).

## ADR-02 — Familias sin registro: código de acceso por álbum

**Contexto**: obligar a familias a crear cuenta genera fricción, abandono y soporte ("olvidé la contraseña") que recaería en el fotógrafo.

**Decisión**: cada álbum tiene un código corto (repartible como tarjeta con QR o por WhatsApp). El código emite un token de sesión acotado a ese álbum. Datos de contacto se piden recién al confirmar el pedido.

**Consecuencias**: quien tenga el código ve el álbum (riesgo aceptado: son fotos con watermark y el círculo es la familia). Códigos revocables y con rate-limiting en el endpoint de canje.

## ADR-03 — Mantener .NET Core + Angular

**Contexto**: se evaluaron alternativas (Next.js/React full-stack). El desarrollador ya domina .NET y Angular.

**Decisión**: ASP.NET Core + Angular. Ninguna alternativa aporta una ventaja material para esta app, y el objetivo dominante es **terminarla**.

**Consecuencias**: dos proyectos (API + SPA) en lugar de un full-stack único; se acepta.

## ADR-04 — Monolito modular, sin colas

**Contexto**: volumen esperado: decenas de familias por evento, pocos eventos por mes, un admin.

**Decisión**: un solo servicio ASP.NET Core (monolito modular: plataforma Base + vertical Fotos, ver ADR-09), procesamiento de imágenes in-process (a lo sumo `BackgroundService` con canal en memoria para uploads masivos).

**Consecuencias**: operación y hosting simples y baratos. Si el procesamiento in-process molestara en cargas grandes, el salto natural es una cola — no antes.

*(Actualizado por ADR-09: la base de datos es SQL Server **de forma transitoria** mientras se construye sobre el código base; la decisión vigente acordada con Alberto es migrar a PostgreSQL más adelante — ver ADR-09 y el pendiente en notas abiertas.)*

## ADR-05 — Storage S3-compatible privado con URLs firmadas; R2 como proveedor

**Contexto**: las fotos son casi todo el tráfico; el egreso de S3/Azure Blob se cobra. Guardar fotos en el disco del servidor ata el deploy y complica backups.

**Decisión**: bucket privado S3-compatible, elección primaria **Cloudflare R2** (egreso gratis). Se implementa detrás de la abstracción `IStorageProvider` que el Core ya trae (config `Storage:Provider`); en **dev alcanza el provider FileSystem existente** — sin Docker ni MinIO. Lectura pública solo por URLs firmadas (~15 min).

**Consecuencias**: hosting de la API desacoplado de las fotos; costo de tráfico ≈ 0; dev sin infraestructura extra; el provider S3 se escribe recién cuando llegue el deploy.

## ADR-06 — Separación estricta originals/ vs derived/

**Contexto**: el mayor riesgo real de "robo" es servir el original por error.

**Decisión**: prefijos separados en el bucket; la capa pública de la API solo puede construir claves de `derived/`; la descarga de originales existe únicamente en endpoints admin autenticados (para imprimir).

**Consecuencias**: el error "se filtró un original" se vuelve estructuralmente difícil, no una cuestión de acordarse.

## ADR-07 — Pedidos con snapshot de precio e inmutables tras confirmar

**Contexto**: los precios cambian entre eventos e incluso durante uno; las familias pueden querer editar pedidos.

**Decisión**: `PedidoItem` congela el precio unitario al confirmar. El pedido confirmado no se edita por la familia; el admin puede cancelarlo y la familia rehace.

**Consecuencias**: contabilidad simple y sin disputas de "cuando lo pedí valía X"; menos UI de edición en el MVP.

## ADR-08 — Mercado Pago, pero recién en Fase 3, y con efectivo siempre como alternativa

**Contexto**: hoy el negocio cobra en efectivo en la entrega; el pago online es mejora, no requisito del reemplazo del "casa por casa".

**Decisión**: MVP sin pagos online (pedido = compromiso, se cobra al entregar). Fase 3 agrega Checkout Pro con webhook.

**Consecuencias**: el MVP llega antes; el modelo de `Pedido` ya prevé los campos de pago para no migrar dolorosamente.

## ADR-09 — Reutilizar el código base propio (CodigoBase) en lugar de arrancar de cero

**Contexto**: existe un código base propio probado (`C:\PROYECTOS\CodigoBase`: API .NET 10 modular + cliente Angular 22) con toda la plataforma resuelta: auth JWT + refresh, usuarios/roles/permisos/grupos, menús dinámicos, multi-tenant, auditoría, rate limiting, theming, suite de tests de integración y e2e. Su vertical de negocio (Budget) no aplica acá.

**Decisión**: fork del código base renombrado a `AcgFotos.*`, excluyendo el vertical Budget completo (proyectos, wiring, tests, migraciones regeneradas). El front parte de la rama `feature/rxresource-adoption` (mejoras con resources). AcgFotos se construye como el vertical **Fotos** siguiendo el mismo patrón modular que tenía Budget (`AcgFotos.Fotos.*` + módulo Autofac + `AppModulesName`; tablas `fot_`).

**Consecuencias**:
- Semanas de plataforma gratis, y patrones/convenciones ya documentados en el propio código.
- ~~**SQL Server de forma transitoria**~~ **MIGRADO A POSTGRESQL (2026-07-24, ver ADR-14)**: el Core corría sobre `UseSqlServer`; ya corre sobre Npgsql, con la suite de integración completa (508/508) verificada contra Postgres local.
- Sin MinIO/Docker en dev: el Core trae `IStorageProvider` con FileSystem. Actualiza ADR-05.
- Se hereda complejidad que AcgFotos no usa hoy (multi-tenant, licencias, grupos): se ACEPTA y no se poda — multi-tenant mapea al futuro "otros fotógrafos" y podar la plataforma rompería la posibilidad de traer fixes del código base.
- Verificación del fork: suite de integración 419/419, Vitest 325/325, lint OK.

## ADR-10 — Naming genérico de punta a punta: Grupo / Participante / LugarOrganizacion

**Contexto**: el negocio se abre a eventos no escolares (cumpleaños, deportes, bautismos — pedido 2026-07-13). Naming acordado (2026-07-14): `Curso` → **Grupo**, `Álbum (alumno)` → **Participante**, `Colegio` → **Lugar/Organización**. Se aplicó antes del alta real del negocio para que el fotógrafo opere desde el día uno con los nombres definitivos, y ANTES de Fase 2 para que el código de familias ya nazca con el naming correcto. Pedido explícito de Alberto: renombrar también las entidades del dominio, no solo etiquetas (hacerlo ahora, pre-producción y sin datos reales, es lo más barato que va a ser jamás).

**Decisión**: rename de punta a punta, sin capa de traducción UI ↔ dominio:

- **Dominio/DB**: entidades `Grupo` y `Participante` (`Participante.Nombre`, ex `NombreAlumno`), `Evento.LugarOrganizacion`; tablas `fot_Grupos` y `fot_Participantes`; FKs `GrupoId`/`ParticipanteId` en `fot_Fotos`, `fot_CodigosAcceso` y `fot_Pedidos`. Migración `RenombrarGrupoParticipante` escrita a mano como RENAMEs (el scaffold de EF proponía drop/create y perdía datos).
- **API**: rutas `api/fotos/grupos`, DTOs `Grupo*`/`Participante*`/`TarjetasGrupoDto`, parámetros `grupoId`/`participanteId`.
- **Front**: feature `features/fotos/grupos`, ruta `/fotos/grupos`, menú "Grupos" (código `FotosGrupos`, ícono `people`), modelos/servicios renombrados.
- Los conceptos que no son entidades conservan su vocabulario natural en la UI ("las fotos van al álbum de Ana" como frase es válido; la entidad es Participante).

**Consecuencias**: en el sidenav de root conviven "Grupos" (vertical Fotos) y "Grupos" de plataforma (grupos de usuarios, sección Gestión) — se distinguen por sección e ícono, y el fotógrafo real no verá los menús de plataforma; en el backend conviven `AcgFotos.Fotos.Domain.Entities.Grupo` y el `Grupo` de la plataforma (namespaces distintos — cuidado con los `using` al tocar ambos). Docs/02 queda como fuente del naming. La migración de renombre es reversible (Down completo).

## ADR-11 — JWT de familia reutiliza la infraestructura JWT existente, con `sessionType` como discriminador

**Contexto**: ADR-02 y docs/01-arquitectura.md ya prescriben que el canje de código emite "un JWT de
corta duración... reutiliza la infraestructura JWT existente". La plataforma firma sus tokens con
`JwtSecurityTokenConfig` (Key/Issuer/Audience únicos, `AcgFotos.Core.Security`), pero
`AuthenticationHelper.OnTokenValidated` (Core) revalida el `SecurityStamp` del token contra
`gen_Usuarios` — chequeo que no tiene sentido para una sesión de familia, que no tiene usuario de
plataforma.

**Decisión**: el token de familia (`FamiliaTokenFactory`, `AcgFotos.Fotos.Application`) se firma con
la MISMA `JwtSecurityTokenConfig` (mismo signing key/issuer/audience) que los tokens de plataforma,
pero lleva un claim propio `sessionType=familia` que lo discrimina, `tenant`/`eventoId`/
`participanteId` (este último repetible — la sesión ya nace lista para más de un participante,
docs/05: hermanos / persona en dos grupos) y una duración propia de 30 minutos
(`Fotos:DuracionSesionFamiliaMinutos`, decisión de Alberto 2026-07-16), no la
`DurationInMinutes` de la sesión del fotógrafo/admin.

**Consecuencias**: el canje (este ítem) YA emite el token con esta forma. El canje en sí no depende
de esto — nunca usa `AppContext` para decisiones de seguridad, siempre resuelve tenant/evento desde
la fila de `CodigoAcceso` recién leída de la base (ver comentarios en `CodigoAccesoRepository`).

**Implementación (ítem Galería, 2026-07-18)**: `OnTokenValidated` (`AuthenticationHelper`, Core)
reconoce `sessionType=familia` y saltea el chequeo de `SecurityStamp` (excepción chica, análoga a la
que ya existe para `isRoot`) — el JWT de familia ya autentica como cualquier `[Authorize]` estándar.
`IAppContext`/`AppContext` (Core) suman `IsFamiliaSession`/`FamiliaEventoId`/`FamiliaParticipanteIds`,
parseados de esos mismos claims (con los nombres de claim repetidos como literales en Core, ya que
Core no puede referenciar el vertical Fotos — mantenerlos sincronizados con `FamiliaTokenFactory` si
cambian). Queda un problema aparte que ADR-11 no había anticipado: `EndpointAuthoritation` valida
permisos contra la matriz de `gen_Usuarios`/roles, que una sesión de familia no tiene — así que,
lejos de heredar el `[Authorize]` estándar sin más, cualquier endpoint autenticado le daba 403
**salvo que se le diera un bypass total**, lo que habría sido peor (una sesión de familia accediendo
a cualquier endpoint admin). Se resolvió con un allowlist explícito: `[AllowFamiliaSession]`
(atributo marcador, Core) en cada acción que deba aceptar esa sesión — hoy solo
`FamiliaGaleriaController` (`api/fotos/familia/fotos`, listado + thumb/preview, ambos scopeados
ÚNICAMENTE por `FamiliaParticipanteIds`, nunca por parámetro del request; sin `/original`, ADR-06).
Cualquier endpoint sin la marca es 403 para una sesión de familia, aunque el JWT sea válido —
verificado en `AuthzFamiliaSessionTests` (con `AuthorizationEnabled=true`) y en
`FamiliaGaleriaTests` (alcance de datos).

**Defensa en profundidad (2026-07-18)**: el flag global `AuthorizationEnabled` está en `false` por
defecto (dev y el `appsettings.json` base) — inutiliza el allowlist de arriba en la práctica, y
prenderlo es un rollout de plataforma completo (ver docs/05), no algo a resolver desde Fotos.
Mientras tanto, `FamiliaSessionGuard.EnsureNoFamiliaSession` (`AcgFotos.Fotos.Application/Security`)
se llama al inicio de todos los métodos públicos de `EventoAppService`, `GrupoAppService` y
`FotoAppService`, y rechaza una sesión de familia con `ForbiddenException` → 403 (Core,
`ExceptionHandlingMiddleware`), **sin depender de `AuthorizationEnabled`**. Verificado con un smoke
test manual (una sesión de familia lograba borrar la foto de otro participante por el endpoint
admin) y con `FamiliaSessionAdminGuardTests` (reproduce el hallazgo, corre con authz OFF a
propósito).

## ADR-12 — Cambio de estado del pedido: cualquier estado salvo no-op, sin workflow forzado

**Contexto**: el admin de pedidos (Fase 2) modela un flujo "normal" lineal (Pendiente/Pagado →
Impreso → Entregado). La primera versión validaba ESE workflow estrictamente (rechazaba saltos y
retrocesos con 400). Alberto lo probó y pidió más flexibilidad: un click equivocado (o querer
corregir un estado mal puesto) no debía quedar bloqueado — "me gusta la idea de usabilidad [de los
botones guiados], pero algo más flexible para algún caso excepcional" (2026-07-20).

**Decisión**: `PedidoAppService.CambiarEstadoAsync` acepta cualquier `EstadoPedido` destino distinto
del actual (incluye `Pagado`/`Cancelado`, hoy sin flujo automático que los produzca, pero
alcanzables a mano). Lo único que rechaza con 400 es "cambiar" al mismo estado en el que ya está
(no-op sin sentido). La guía queda del lado de la UI, no del backend: el admin de pedidos ofrece dos
botones contextuales para el camino normal (`Marcar impreso` / `Marcar entregado`, sin confirmación)
y un botón "Corregir estado…" aparte, con confirmación, para el caso excepcional — ambos en el
footer del diálogo de detalle, el segundo visualmente secundario (2026-07-26, ver quinta vuelta en
docs/03-fases.md: antes vivía en el header, separado de los botones guiados, y eso confundía).

## ADR-13 — Lista de impresión: dos vistas en un export, sin PDF real server-side, proporciones por regex best-effort

**Contexto**: último ítem de Fase 2. El laboratorio externo del fotógrafo pide los originales en una
medida específica sin nomenclatura propia, y algunos lo reciben por correo con una descripción —
necesita, por evento, tanto cuánto producir de cada foto+tamaño (para el laboratorio) como cómo
repartir esas copias entre las familias al volver.

**Decisión**:
- **Dos vistas en un solo export**: agregado por foto+tamaño (`PedidoItem` agrupado por
  `FotoId+TamanoPrecioId`, sumando `Cantidad`) y detalle agrupado por participante/álbum. Se arman
  en memoria en `PedidoAppService.GetListaImpresionAsync` (volumen bajo por evento, no justifica
  traducir el `GroupBy` a SQL).
- **Filtro de estados sin default forzado en el backend**: el endpoint recibe la lista de
  `EstadoPedido` a incluir (CSV en el query string — `estados=1,0` — porque el `QueryParams` del
  front solo admite valores escalares, no arrays) y filtra exactamente por eso; una lista vacía da
  resultado vacío. El default "Pagado preseleccionado" es una decisión de UX del diálogo del front,
  no una regla de negocio del backend.
- **Salida sin generar PDF real server-side**: se reusa el patrón ya validado en `/fotos/tarjetas`
  (HTML autocontenido en una ventana nueva vía `window.open` + `print()`, "Guardar como PDF" del
  navegador) en vez de sumar una librería de PDF nueva — más un botón aparte de CSV del agregado
  (generado client-side) para los laboratorios que piden planilla en vez de PDF/imagen.
- **Proporciones — best-effort, sin tocar el catálogo todavía**: `TamanoPrecio.Nombre` es texto
  libre por evento (ej. "10x15"), sin ancho/alto numéricos. El aviso de desajuste (recorte fuerte al
  imprimir) parsea el nombre con regex `NxM` y lo compara contra `Foto.Ancho/Alto` (que sí existen,
  en píxeles); si el nombre no matchea, no avisa — no bloquea. Agregar ancho/alto estructurados al
  catálogo de tamaños (cambio de entidad + ABM de Eventos) queda como mejora futura si el
  best-effort resulta insuficiente en uso real (ver docs/05-notas-abiertas.md).

**Vuelta y vuelta el mismo día sobre CUÁNDO se ve el selector** (front únicamente, el backend no
cambió en ninguna de las dos): primero se probó ocultarlo salvo que NINGÚN botón guiado aplicara
(solo con pedido `Entregado`) — Alberto lo probó con un pedido `Pendiente` y no podía usarlo
("no puedo usar el corregir estado... no funciona ni para salir de pendiente"), y aclaró que la
idea original era la correcta: el botón simple de un click (`Marcar impreso`/`Marcar entregado`) le
gustaba, pero necesitaba la corrección manual disponible SIEMPRE, sin condicionarla al estado —
"la desventaja [del botón simple] era que no podía cambiar el estado por si me equivoqué y quería
volver atrás". **Quedó**: el selector "Corregir a otro estado…" se muestra siempre, en cualquier
estado, junto con los botones guiados (que también se muestran siempre que apliquen) — ambos
caminos conviven, no son mutuamente excluyentes.

**Consecuencias**: no hay ninguna protección de backend contra un estado "ilógico" (p. ej. Entregado
→ Pendiente): es una herramienta admin interna de un solo fotógrafo por tenant, así que se prioriza
poder corregir errores por sobre impedir estados raros. Si en Fase 3 (Mercado Pago) `Pagado` empieza
a fijarse automáticamente al confirmar el webhook, este mismo endpoint sigue sirviendo sin cambios.

## ADR-14 — Migración SQL Server → PostgreSQL: cutover directo, sin capa de compatibilidad dual

**Contexto**: ADR-09 dejaba pendiente migrar de SQL Server a PostgreSQL antes del deploy productivo
(decisión ya acordada, no se re-discute acá — ver docs/06-deploy.md por el contexto de costos/hosting
que la motivó). Se ejecutó completa en una sesión (2026-07-23/24): backend, migraciones, seeds de
test/dev y la suite de integración completa, verificada en verde contra Postgres local.

**Decisión — alcance y forma**:
- **Cutover directo**, no un *switch* de proveedor en runtime: `DatabaseFactory.ConfigureEFProvider`
  pasó de `UseSqlServer` a `UseNpgsql` sin dejar SQL Server como alternativa configurable. Simplifica
  contra la idea original de "proveedor configurable" — no hay caso de uso real para correr con ambos
  proveedores a la vez, y mantenerlo hubiese sido complejidad sin beneficio (YAGNI).
- **`EFCore.NamingConventions` con `UseLowerCaseNamingConvention()`**: decisión clave para no reescribir
  a mano cientos de referencias de SQL crudo heredado (`TestSeed.sql`, seeds de dev, decenas de queries
  de test) que asumen identificadores case-insensitive (comportamiento de SQL Server). Postgres pliega
  a minúscula cualquier identificador SIN comillas; forzando que las tablas/columnas se CREEN en
  minúscula, todo ese SQL crudo (que ya las referencia sin comillas, en PascalCase o no) seguía
  funcionando sin tocarlo. Los nombres de tabla fijados a mano con `ToTable(...)` (EFConfig del código
  base + Identity) no los toca la convention (gana la config explícita) — se normalizan aparte con un
  loop en `AcgFotosDbContext.OnModelCreating` que baja a minúscula `GetTableName()`/`GetViewName()` de
  cada entidad tras registrar el modelo completo.
- **Vista `vw_UsuarioRolesEfectivos`** (SQL crudo, ADR-11): reescrita a mano en la migración regenerada
  (EF no la reconstruye sola al regenerar migraciones — queda documentado igual que antes en el propio
  archivo de migración). Traducción: sin `dbo.`, `CAST(... AS bit)` → `EXISTS(...)` directo (booleano
  nativo), todo en minúscula sin comillas.
- **Respawn** (reseteo de DB entre tests): `DbAdapter.Postgres`, esquema `public` en vez de `dbo`. El
  overload de `Respawner.CreateAsync`/`ResetAsync` por *connection string* sólo soporta SQL Server —
  hay que pasar un `NpgsqlConnection` ya abierto.
- **Serilog → `Serilog.Sinks.PostgreSQL`**: reemplaza `Serilog.Sinks.MSSqlServer`, mismo patrón de
  `columnOptions` apuntado a las columnas reales de `gen_LogInfos` (vía `configurationPath` en el JSON,
  ya que el sink no tiene el mismo autoconfig por convención que traía el de MSSQL).
- **`Npgsql.EnableLegacyTimestampBehavior`** (switch global, `[ModuleInitializer]` en `DatabaseFactory`):
  el código heredado usa `DateTime.Now` (Kind=Local) en decenas de lugares para columnas mapeadas a
  `timestamp with time zone`; Npgsql 6+ rechaza en runtime cualquier `DateTime` que no sea Kind=Utc
  contra ese tipo. Se restaura el comportamiento tolerante anterior en vez de auditar cada call site
  de una sola vez — **deuda documentada**: lo correcto a mediano plazo es migrar a `DateTime.UtcNow`
  gradualmente y eventualmente sacar el switch.
- **`ConfigureWarnings` ignora `PendingModelChangesWarning`**: falso positivo conocido de
  `EFCore.NamingConventions` (el diff modelo-vivo-vs-snapshot no es 100% estable entre el tooling de
  diseño y runtime con este plugin) — no indica una migración real pendiente.
- **`SELECT COUNT(*)` vía ADO crudo**: Postgres devuelve `bigint` (no `int` como SQL Server) — los
  `(int)ExecuteScalar()` de `UsuarioAppService.EmailExists` y `LogInfoAppService.GetForAllTenants`
  tiraban `InvalidCastException` en runtime (no se detecta en compilación, `ExecuteScalar` devuelve
  `object`). Corregido a castear `long` primero.
- **Boolean literal**: SQL Server no distingue `bit` de `int` en comparaciones (`Activo = 1` funciona);
  Postgres sí — cualquier `= 1`/`= 0` contra una columna boolean tira `42804 (la columna es de tipo
  boolean pero la expresión es de tipo integer)`. Se tradujo a `= true`/`= false` en todo el SQL crudo
  encontrado (producción y tests). Relacionado: `CONCAT()`/cast de un boolean a texto en Postgres da
  `'t'`/`'f'` (un carácter), no `'true'`/`'false'` como en SQL Server — algunas aserciones de test que
  comparaban el resultado de un `CONCAT` con un booleano tuvieron que ajustarse.
- **Identity/secuencias**: a diferencia de `SET IDENTITY_INSERT` (SQL Server, que si avanza el contador
  interno tras un insert explícito), la identity `GENERATED BY DEFAULT AS IDENTITY` de Postgres permite
  insertar valores explícitos SIN wrapper especial, pero **no resincroniza la secuencia** — el seed de
  tests (`TestSeed.sql`) inserta filas con Id fijo y bajo, así que sin un `setval(pg_get_serial_sequence(...))`
  explícito al final del seed, el próximo insert "automático" de la app terminaba repitiendo un Id ya
  usado y violando la PK.

**Consecuencias**: la migración quedó verificada end-to-end (suite de integración 508/508 contra
Postgres local — subió de los 419 originales por tests nuevos de fases posteriores). Pendiente y
fuera del alcance de esta sesión: la base de dev (`AcgFotos`, distinta de `AcgFotos_Tests`) quedó con
el esquema aplicado pero sin datos — sembrar root/tenant/fotógrafo es un paso aparte, no específico de
esta migración (la base de SQL Server ya tenía esos datos acumulados de sesiones anteriores). Deuda
documentada: migrar `DateTime.Now` → `DateTime.UtcNow` en el código heredado para poder sacar el switch
legacy de timestamps.

## ADR-15 — Marca de agua configurable: el front diseña y rasteriza, la API sólo compone

**Contexto**: la marca de agua vivía en `appsettings` (`Fotos:TextoWatermark`, `OpacidadWatermark`) y
su dibujo estaba hardcodeado en `ImageSharpImageProcessor` — cambiarla exigía reiniciar la API. El
pedido (Alberto, 2026-07-18, ampliado el 2026-07-26/27) es un ABM que permita customizar imagen,
color, texto, intensidad y opacidad, con vista previa en vivo.

El problema de diseño no fue *qué* configurar sino **dónde vive el dibujo**. Una vista previa fiel
en el front y un horneado en la API son dos implementaciones del mismo algoritmo que deben coincidir
pixel a pixel para siempre; alcanza con que alguien toque una y no la otra para que el fotógrafo
ajuste contra una imagen que no existe y se entere con cientos de fotos ya horneadas. Se descartó el
esquema híbrido (canvas mientras se arrastra, render real al soltar) por eso mismo: pedido explícito
de Alberto de tener la lógica en un solo lado.

**Decisión**:

1. **El front diseña y rasteriza; la API compone.** La marca se define en el navegador y viaja a la
   API como un **PNG con transparencia** más parámetros de colocación. La API nunca dibuja texto:
   coloca, escala y funde un bitmap sobre la foto. Los pixeles que el fotógrafo vio son literalmente
   los que se componen — no "coinciden", son los mismos.
2. **Perfil = de 1 a 3 capas.** Cada capa es una imagen (diseñada o subida) con su colocación:
   repetida en mosaico o en una de 9 posiciones fijas, con escala en % del ancho, margen, ángulo,
   opacidad y modo de fusión. Así el "modo logo" (subir un archivo) y el "modo diseñador" (escribir
   un texto) son el MISMO mecanismo, y se pueden combinar: logo del estudio en la esquina + trama de
   texto encima. El editor de texto es una forma de fabricar la imagen, no parte del contrato.
3. **Los modos de fusión se quedan** (`Normal · Superponer · Diferencia` →
   `GraphicsOptions.ColorBlendingMode`). Resuelven un agujero real de la marca actual: el blanco al
   50% desaparece sobre un vestido claro, o sea que **la foto más valiosa es la peor protegida**. No
   reintroducen el problema de la lógica duplicada porque la fórmula de fusión no es código nuestro:
   canvas e ImageSharp implementan la misma especificación del W3C. Se verifica **una vez** con un
   test que compare ambas salidas sobre la misma muestra; nadie edita nunca una fórmula de fusión.
4. **Resolución de configuración**: evento → perfil default del tenant → `OpcionesFotos` de
   `appsettings`. Sin perfiles cargados, el pipeline se comporta exactamente como hoy.
5. **Las opciones de publicación son una entidad APARTE** (`fot_OpcionesPublicacion`: lado mayor de
   preview/thumb y calidad), con el mismo circuito default-del-tenant → override por evento. Son ejes
   independientes: se puede querer marca sutil con resolución baja o al revés, y una entidad llamada
   "perfil de marca de agua" que además fija la resolución haría dos cosas. `MarcarThumb` sí se queda
   en el perfil de marca: es una decisión sobre la marca, no sobre la resolución.
6. **Aplicar a fotos ya subidas es explícito**, por evento, con el conteo por delante. Guardar un
   perfil nunca toca una foto: un evento de 400 fotos son 400 reprocesos, y dispararlos con cada
   retoque de opacidad congelaría el worker sobre fotos que las familias están mirando.
7. **El asset de la marca se guarda sin pérdida (PNG)**, nunca en WebP con pérdida: comprimirlo y
   después comprimir el derivado compuesto degrada dos veces la misma marca.
8. **El asset se rasteriza al tamaño máximo que podría llegar a usarse, para que la composición sólo
   escale HACIA ABAJO.** Reducir un bitmap da nitidez; agrandarlo no tiene arreglo, y ocurre antes de
   que el encoder toque nada: un logo de 300 px llevado al 70% de una foto de 1600 px son 1120 px
   sacados de 300. Con el tope actual (lado mayor 1600, escala hasta 70%) el piso es ~1200 px de lado
   mayor. Para la capa de texto es gratis, la dibuja el front al tamaño que haga falta; para el logo
   subido no depende de nosotros, así que se valida al subirlo y se avisa con el número concreto
   ("tu logo tiene 300 px de ancho; a esta escala se va a ver borroso — subí uno de al menos 1200 px
   o bajá la escala").

**Principio transversal — las guardas se explican solas** (pedido de Alberto 2026-07-27, aplica también
a la política de retención de originales, docs/05): ninguna validación ni guarda de este vertical
avisa en genérico ni actúa en silencio. Cada una dice **el número concreto y la consecuencia real, en
el momento en que importa**: "tu logo tiene 300 px de ancho; a esta escala se va a ver borroso" en vez
de "imagen inválida"; "se van a regenerar 412 fotos, tarda unos minutos" antes de encolar, no después;
"las familias van a ver estas fotos sin ninguna protección" al guardar un perfil sin marca; y el aviso
de que un logo en una esquina se recorta fácil, ahí donde se elige la posición. Una guarda que el
fotógrafo no entiende es una guarda que va a tratar de saltear.

**Consecuencias**:

- La Etapa "logo" deja de existir como etapa separada: un logo es una capa más, comparte todo aguas
  abajo con el texto.
- **Se cae una fragilidad de producción**: hoy `ResolverFuente` pide Arial al sistema operativo y, si
  no está, agarra la primera familia que encuentre — en un contenedor Linux eso es una bomba de
  tiempo. Con la tipografía horneada en el PNG, el servidor no necesita ninguna fuente instalada.
- **La vista previa debe mostrar la imagen DESPUÉS del encoder WebP.** WebP con pérdida ataca primero
  las transiciones suaves y el bajo contraste, que es exactamente una marca sutil, una sombra difusa o
  el trazo fino de un logo. Previsualizar sin comprimir muestra algo que nunca va a existir. De ahí
  se deriva la guía de diseño: preferir color sólido y opacidad plana; el contorno duro sobrevive bien
  a la compresión, la sombra difusa es la primera víctima.
- El texto dinámico por foto (hornear "Familia {nombre}") NO queda cerrado por esta decisión, pero
  tampoco habilitado: exigiría un derivado por (foto × participante) y eso era caro desde antes. Si
  alguna vez se hace, es una segunda capa aplicada **al servir**, ortogonal a esto. Mientras tanto
  sigue el overlay dinámico del front, que ya funciona.
- Se acepta el riesgo de iOS 13 y anteriores, que no decodifican WebP (decisión ya vigente desde el
  2026-07-15, no la introduce este ADR).
- **El peso de los assets es irrelevante para el storage**: son unos pocos archivos por tenant (uno por
  capa de cada perfil), no uno por foto, y un PNG de texto plano sobre transparencia comprime muy bien.
  El storage lo dominan los ORIGINALES, no los derivados ni los assets: con la medición real del piloto
  (original 10,5 MB, preview 40 KB, thumb ~6 KB), un evento de 400 fotos son ~4,2 GB de originales
  contra ~19 MB de derivados — el 99,5% es original. Donde sí pesan los derivados es en **ancho de
  banda**, porque se sirven una y otra vez: ahí es donde paga el ahorro de WebP. Si alguna vez hace
  falta bajar el storage de verdad, la palanca es la política de retención de originales
  (docs/05-notas-abiertas.md), no la calidad de los derivados.
- Prototipo navegable del diseño y del circuito: `docs/ClaudeDesign/PropuestaMarcaAgua/`.

## ADR-16 — Composición de capas con SkiaSharp, ImageSharp se mantiene para el resto (transitorio)

**Contexto**: al implementar el test de paridad de ADR-15 §3 (docs/04-decisiones.md línea 306, "los
modos de fusión se quedan... `GraphicsOptions.ColorBlendingMode`"), se verificó que
`SixLabors.ImageSharp.PixelFormats.PixelColorBlendingMode` (versión instalada, 3.1.8) sólo tiene
`Normal, Multiply, Add, Subtract, Screen, Darken, Lighten, Overlay, HardLight` — **no existe
`Difference`**. El ADR daba por sentado un mapeo 1 a 1 con canvas que no es cierto del lado
ImageSharp; el hueco es específicamente de la librería, no de la fórmula (canvas sí tiene
`globalCompositeOperation = 'difference'`, estándar y universal).

Al revisar alternativas se encontró que `TechBI.Base.Application` (código base, `UsuarioAppService`,
resize+encode de foto de perfil) ya usa **SkiaSharp 4.150.1**, no ImageSharp, con una razón
documentada en su `.csproj`: licencia MIT sin escalón comercial (a diferencia de Six Labors, la
licencia de ImageSharp, que pasa a paga por encima de cierto umbral de facturación/empleados) y
manejo de nativos multiplataforma ya resuelto (paquete `NativeAssets.Linux.NoDependencies` evita el
requisito de `libfontconfig1`). Se verificó (no se asumió) que `SkiaSharp.SKBlendMode` tiene el set
completo de 29 modos —incluye `Difference`, `SoftLight`, `Exclusion`— y que `SKEncodedImageFormat`
soporta `Webp`.

**Decisión**: la composición de capas de marca de agua (ADR-15) se escribe en **SkiaSharp**. El resto
del pipeline existente (resize, limpieza EXIF, encode WebP en `ImageSharpImageProcessor`) **se queda
en ImageSharp tal como está** — no se toca código ya estable fuera del alcance de esta feature. Es una
decisión **transitoria, explícitamente acotada**: al cerrar toda la feature de marca de agua
(docs/03-fases.md), se evalúa migrar el pipeline completo a SkiaSharp para unificar en un solo motor
de imágenes en toda la plataforma (docs/05-notas-abiertas.md).

Se descartó implementar `Difference` a mano sobre ImageSharp (fórmula W3C de una línea, ya explorada y
viable) porque no resuelve la exposición a la licencia comercial de ImageSharp a largo plazo y deja a
mitad de camino la consolidación con el código base. Se descartó migrar el pipeline completo ahora
porque reescribiría código ya shippeado (resize/EXIF/encode) sin relación con la marca de agua,
disparando el alcance de un grupo de tareas pensado como una verificación puntual (D7 del `design.md`
del change) a un cambio transversal a todo el vertical Fotos.

**Consecuencias**:

- Conviven dos librerías de imágenes en `AcgFotos.Fotos.*` durante la vida de este change: ImageSharp
  (resize/EXIF/encode) y SkiaSharp (composición de capas). El puente entre ambas (decodificar con una,
  componer con la otra, re-codificar con la primera) es responsabilidad de la tarea 3.2.
- SkiaSharp trae binarios nativos por plataforma — mismo tipo de fragilidad de contenedor que ADR-15
  ya eliminó al sacar `ResolverFuente`/fontconfig del lado de texto; a tener en cuenta al definir la
  imagen de contenedor del deploy (`SkiaSharp.NativeAssets.Linux.NoDependencies`, mismo paquete que usa
  el código base).
- El test de paridad (`BlendModeParityTests`) compara `SKBlendMode` contra fixtures reales de
  Chromium/Playwright (no fórmulas W3C derivadas a mano): 3/3 en verde con tolerancia ±2 por canal
  sobre una muestra de 4 cuadrantes (clara/oscura/media/saturada) con una capa blanca al 50%.
- Pendiente explícito para después de cerrar la feature: migrar `ImageSharpImageProcessor` completo a
  SkiaSharp (anotado en docs/05-notas-abiertas.md), momento en el que ImageSharp puede salir del todo
  del vertical Fotos.
