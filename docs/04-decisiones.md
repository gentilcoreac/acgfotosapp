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
- **SQL Server de forma transitoria** (el Core está clavado a `UseSqlServer` y en dev ya está instalado). **Decisión acordada: migrar a PostgreSQL antes del deploy productivo** (era la recomendación original por costo de hosting). La migración implica: proveedor configurable en `DatabaseFactory` (+ paquete Npgsql), regenerar migraciones, adaptar el SQL crudo (vistas), Respawn a PostgresAdapter y el sink de Serilog. Mientras tanto: no escribir SQL crudo con sintaxis exclusiva de SQL Server en el vertical Fotos.
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
y un selector "Corregir a otro estado…" aparte, con confirmación, para el caso excepcional.

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
