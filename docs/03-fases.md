# Roadmap por fases

Cada fase termina en algo usable. No empezar una fase sin cerrar la anterior.

> **Este documento ES el roadmap vivo del proyecto**: al completar un ítem se tilda acá; esta
> sección de estado se actualiza al cerrar cada bloque de trabajo.

## Estado (2026-07-16) y próximos pasos

**Dónde estamos**: Fase 0 cerrada. **FASE 1 TERMINADA** (2026-07-16) — API (468 tests) y
front admin: ABMs de Eventos y Grupos/Participantes, pantalla única de Fotos (`/fotos/galeria`:
subida masiva + grilla con preview/descarga/borrado) y Tarjetas imprimibles (`/fotos/tarjetas`).
Se prueba en :4200 como `fotografo` (clave dev). **Naming genérico aplicado de punta a punta**
(2026-07-15, ADR-10): `Curso→Grupo`, `Álbum→Participante`, `Colegio→LugarOrganizacion` en
entidades, tablas (migración `RenombrarGrupoParticipante`), API (`api/fotos/grupos`), front
(`/fotos/grupos`) y UI. **Alta real del negocio y evento piloto**: hechos de forma simulada
(tenant básico + evento con pocas fotos, sin pasar por el alta definitiva del ABM) — alcanza para
desarrollar y probar Fase 2; el alta DEFINITIVA del tenant del fotógrafo queda pendiente para
antes de lanzar a producción real (ver Deploy).

**Próximos pasos, en orden**:

1. **Fase 2** (familias): canje de código → sesión de álbum, galería mobile-first con anti-copia, carrito y pedidos, lista de impresión. Al diseñar el token de sesión, dejarlo preparado para MÁS de un participante por sesión (ver nota en docs/05: hermanos / persona en dos grupos).
2. Al final de Fase 2: decisiones de **deploy** (PostgreSQL, hosting, R2, dominio) — incluye el alta definitiva del tenant del fotógrafo.

## Fase 0 — Fundaciones (actual)

Objetivo: plataforma portada del código base compilando y verificada, y el vertical Fotos con su esqueleto.

- [x] Documentación: visión, arquitectura, modelo de datos, decisiones, notas
- [x] Backend portado de CodigoBase sin Budget, renombrado a `AcgFotos.*` (ADR-09), migración inicial regenerada, 419/419 tests de integración
- [x] Frontend portado de CodigoBase (rama rxresource-adoption) sin Budget: build + 325/325 unit tests + lint verdes
- [x] Esqueleto del vertical Fotos: proyectos `AcgFotos.Fotos.{Domain,Application,Infrastructure,Controllers}`, módulo Autofac, alta en `AppModulesName` (con tests de humo del cableado)
- [x] Entidades del vertical (docs/02) con EF config (`fot_`, 8 tablas) y migración `VerticalFotos` aplicada
- [x] `IImageProcessor` (ImageSharp): thumb + preview con watermark en diagonal, con tests de humo (dimensiones, no-upscale, watermark en los 4 cuadrantes, contenido inválido)
- [x] API levantada en dev (:30000) con base `AcgFotos` migrada y seedeada; login de `root` verificado end-to-end; runbook en el README
- [x] Front (:4200) corriendo contra la API dev (verificado: front 200, login con CORS correcto desde el origen 4200)

**FASE 0 TERMINADA** (2026-07-12).

## Fase 0b — Backports de CodigoBase (post-fork)

El fork (ADR-09) se tomó el 2026-07-12 y CodigoBase siguió recibiendo mejoras. Ítems aplicables a
AcgFotos (los específicos de Budget/DAP/CGV no aplican), verificados contra el código actual el
2026-07-15; se referencia el commit de CodigoBase para portar mirando el diff:

- [x] **Datos de alta de tenant: rol default** (hecho 2026-07-15). El `seed.sql` de CodigoBase siembra un rol "Administrador Cliente" con `EsDefaultParaNuevoTenant=1`; el fork perdió ese dato → todo tenant creado desde el ABM quedaba "sin administradores" y su usuario admin sin rol ni menús. Corregido en `dev-alta-fotografo.sql` y aplicado a la DB dev: el rol `Fotógrafo` pasa a ser el default (habilitado también para la licencia `adminCliente`, como en CodigoBase) y un bloque de reparación asigna el rol a los admins de tenants ya creados (ACG y Fotografo2 ya listan administrador).
- [ ] **Seed de instalación versionado + setup.md** (patrón `Docs/seeds` de CodigoBase, commits `db61f3d`/`c9fd00c`): AcgFotos no tiene seed para levantar una DB nueva (`TestSeed.sql` es de tests; `dev-alta-fotografo.sql` asume la base dev ya seedeada). Portar el patrón: un `seed.sql` idempotente (tenant root + usuario root + roles/licencias con sus flags default + menús del vertical) y un doc de setup que lo marque como **no opcional**, en dos partes (antes de levantar el host, y después — `GET /api/general/discover` + permisos por endpoint). Insumo directo del ítem "alta real del negocio" y del deploy.
- [ ] **Validación de credenciales SMTP en `Correo.cs`** (commit `a4c613e`): la validación de credenciales/puerto/server pasa a vivir detrás de `EmailEnabled` y el login anónimo requiere flag explícito; hoy en AcgFotos un estado a medias (usuario sin password) falla recién en el servidor con un `SmtpException` poco claro (verificado: el fix no está).
- [ ] **Docs de credenciales/secrets** (commits `d0bd3d6`, `e55f6cd`, `8437e9a`): inventario de user-secrets locales (sin valores) y cómo forzar secrets por sobre `gen_Parametros` para el email.
- [ ] **CONTRIBUTING.md** (API `0a5fd3d`, Cliente `381f3cf`): no existe en AcgFotos; adaptar (política de comentarios, troubleshooting).
- [ ] **Front: `lookupResource` + `hasValue()` nativo** (Cliente `99a597a`): reemplaza ~19 rxResource de lookup duplicados por un helper único; `shared/util/lookup-resource.ts` no existe en AcgFotos (verificado). De paso confirmar que el fork (rama rxresource-adoption) incluye el fix crítico de resource en error (`d82e83c`).

## Fase 1 — MVP admin

Objetivo: el fotógrafo puede dejar un evento listo para publicar.

- [x] Login admin (JWT) — heredado de la plataforma, verificado
- [x] CRUD Evento con catálogo TamanoPrecio como colección hija (API `api/fotos/eventos`, 10 tests de integración). Nota: el fotógrafo opera como usuario NO-root de su propio tenant (guard multi-tenant de la plataforma); root + header `SimulatedTenant` en dev/tests. **ABM admin en el front** (`/fotos/eventos`: listado + edit-dialog con la grilla de tamaños; requiere seed de menú, ver docs/05)
- [x] CRUD Grupo / Participante (API `api/fotos/grupos`, participantes como colección hija; al crear un participante se le genera su código de acceso `XXXX-XXXX`, 11 tests de integración). Guards: el EventoId del input debe existir en el tenant, y no se borran grupos/participantes con fotos. **ABM admin en el front** (`/fotos/grupos`: listado con filtro por evento + edit-dialog con la grilla de participantes y su código). *(Nombres previos al ADR-10: Curso/Álbum.)*
- [x] Upload masivo de fotos (multi-archivo a grupo o participante: `POST api/fotos/fotos/upload`) con procesamiento en background (cola en memoria + `FotoProcesamientoWorker`, `Pendiente → Lista/Error`): originales al storage privado, thumb + preview con watermark; 8 tests de integración. Config del watermark en `Fotos:TextoWatermark`. **Front**: integrado en la pantalla única `/fotos/galeria` (subida por tandas de 10 con progreso, al destino del selector de participante)
- [x] Galería admin (API): entrega de thumb/preview con watermark (`GET api/fotos/fotos/{id}/thumb|preview`, 404 hasta estar Lista), descarga del original limpio (`{id}/original`, único camino al original — ADR-06) y borrado completo de foto (fila + storage); 5 tests de integración. **Pantalla en el front** (`/fotos/galeria`, pantalla ÚNICA de fotos — unificada con la subida el 2026-07-14: grilla de thumbs por grupo con filtro por participante que es a la vez destino de subida, preview ampliado en diálogo, descarga del original y borrado con confirmación; las imágenes van por blob autenticado — `tbi-foto-img`)
- [x] Tarjetas con código y QR por participante (API): `GET api/fotos/grupos/{id}/tarjetas` devuelve una tarjeta por participante con código activo, URL de canje (molde `Fotos:UrlCanjeTemplate`) y QR PNG en base64 (QRCoder); 2 tests de integración. **Pantalla en el front** (`/fotos/tarjetas`: previsualización por grupo e "Imprimir" que abre una ventana limpia solo con las tarjetas — A4, 2 columnas — y dispara el print del browser; avisa si hay participantes sin código activo)

**Terminada cuando**: tu papá (o vos simulándolo) puede crear un evento real completo sin tocar la base de datos.

## Fase 2 — MVP familias + pedidos

Objetivo: una familia real puede hacer un pedido de punta a punta. **Con esto ya se reemplaza el ir casa por casa.**

- [x] Ingreso por código / link QR → token de sesión del álbum (API: `POST api/fotos/canje`, ADR-11; rate-limiteado, rechaza evento no publicado/expirado)
- [x] **Galería mobile-first** (hecho 2026-07-18): consumo real del token de familia. `OnTokenValidated` (Core) ya reconoce `sessionType=familia` y saltea el chequeo de SecurityStamp; `EndpointAuthoritation` suma un allowlist explícito (`[AllowFamiliaSession]`, ver ADR-11) — una sesión de familia solo pisa endpoints marcados, todo lo demás 403. Nuevo `api/fotos/familia/fotos` (listado + thumb/preview) scopeado ÚNICAMENTE por los `participanteId` firmados en el JWT (individuales + grupales del grupo), nunca por parámetro; sin `/original` (ADR-06). Front: `/mi-album` pasa de stub a grilla mobile-first + preview ampliado (`tbi-foto-familia-img`, mismo patrón blob-autenticado que el admin), y el `authInterceptor` aprendió a usar el token de `FamiliaSessionStore` en vez del de plataforma para esas rutas. Tests: 18 nuevos de integración (alcance de datos + el allowlist con `AuthorizationEnabled=true`) + verificación manual end-to-end contra la API dev real. **Hallazgo del smoke test, RESUELTO el mismo día (ver docs/05 y ADR-11)**: `AuthorizationEnabled=false` por defecto en dev/prod dejaba que cualquier JWT válido (incluida una sesión de familia) llamara cualquier endpoint admin — confirmado con un token de familia borrando la foto de otro participante. Se descartó el rollout completo de permisos de plataforma (fuera de alcance de Fase 2, decisión de Alberto) y en su lugar se agregó `FamiliaSessionGuard` en los AppServices admin del vertical (Evento/Grupo/Foto): rechaza la sesión de familia con 403 sin depender de ese flag. 4 tests de regresión + suite completa (487/487).
- [x] ~~Fricción anti-copia en la galería de familias~~ (capa 2 de ADR-01; detectado 2026-07-16 en la galería admin: clic derecho → "Guardar imagen como" funciona). **RESUELTO (2026-07-18)**: en `tbi-foto-familia-img` — `(contextmenu)="$event.preventDefault()"` + `draggable="false"` en el `<img>`, `user-select: none`, y `@media print { :host { display: none } }`. No se agregó el div transparente (alternativa descartada: bloquear `contextmenu` directo en el `<img>` ya suprime el menú nativo en Chrome/Firefox sin nodo extra). **Veredicto de robustez (no re-discutir)**: en web NO existe bloqueo real — la imagen siempre llega al navegador y se extrae por dev tools/Network, y el screenshot es imbloqueable (ADR-01). La defensa real ya está hecha por diseño: lo único descargable es un WebP de 900px con watermark horneado (valor comercial ≈ 0) + overlay con nombre (Fase 2) + app con FLAG_SECURE (capa 3). Aplicar la misma fricción en la galería admin queda opcional/cosmético, sin hacer: el admin tiene el botón de descargar el original al lado.
- [ ] Carrito: foto + tamaño + cantidad; total en vivo
- [ ] Confirmación de pedido con nombre y teléfono
- [ ] Admin: listado de pedidos por evento, detalle, cambio de estado (Pendiente → Impreso → Entregado)
- [ ] Lista de impresión exportable (agregado por foto/tamaño/cantidad, agrupada por álbum)

**Terminada cuando**: se corre un evento piloto real con al menos una familia de prueba.

## Fase 3 — Pagos y comunicación

- [ ] Mercado Pago Checkout Pro: preferencia al confirmar pedido, webhook de confirmación, estado `Pagado`. Contemplar **pago total o seña/parcial** (el negocio cobra de ambas formas — respuesta del fotógrafo 2026-07-15)
- [ ] Opción "pagar en la entrega" (efectivo) como alternativa siempre disponible
- [ ] Paquetes/promos (ej. "2× 13x18 + 4× 10x15 a $X") — adelantado de Fase 4 (2026-07-15: los usa rara vez pero suman)
- [ ] Links de WhatsApp prearmados (enviar código a la familia, avisar "pedido listo")
- [ ] Expiración de álbumes (aviso de "disponible hasta el X" + cierre automático del evento)

## Fase 4 — Mejoras según uso real (backlog, priorizar con feedback)

- **App móvil híbrida (Capacitor sobre el mismo Angular)** con `FLAG_SECURE` en Android (capturas de pantalla en negro) y detección de captura en iOS — capa 3 de ADR-01. **Prioridad alta del backlog**; adelantable a Fase 3 si el bloqueo de capturas pesa más que el pago online
- ~~**Eventos genéricos, no solo colegios**~~ **HECHO** (2026-07-15, adelantado de Fase 4 — ADR-10): naming genérico `Curso→Grupo`, `Álbum→Participante`, `Colegio→LugarOrganizacion` aplicado de punta a punta (entidades, tablas con migración, API, front, menú y textos). Queda para el uso real: validar que ninguna pantalla/flujo asuma "colegio".
- **UX de la pantalla de Fotos** (ideas 2026-07-14, priorizar con uso real):
  - Secciones por foto (etiquetar cada foto con una sección del evento — ceremonia, diplomas, grupales informales — y filtrar/agrupar por eso)
  - Alternar vista lista ↔ miniaturas en la galería
  - Selección múltiple de fotos (borrar/mover en lote)
  - Drag & drop de archivos al cargar (además del botón)
  - Filtrar eventos por fecha con un calendario que resalte los días con eventos
  - Ver una foto en pantalla completa y pasar a la anterior/siguiente sin salir (lightbox con flechas/teclado). *Avance 2026-07-16: el diálogo de preview ya abre casi a pantalla completa (96vw×94vh, foto entera sin scroll); faltan las flechas anterior/siguiente*
- Foto en varios álbumes (etiquetar una misma foto en más de un participante — ej. la foto donde salen dos chicos juntos — sin duplicar el archivo; hoy se resuelve subiéndola a grupales o duplicándola)
- Copiar el catálogo de tamaños/precios del evento anterior al crear un evento nuevo (los tamaños se repiten entre eventos; hoy se recargan a mano)
- ~~Paquetes/promos~~ → adelantado a Fase 3 (2026-07-15)
- Venta de foto digital (entrega del archivo limpio tras el pago, p. ej. **por email** — el pedido pediría correo; idea 2026-07-15 — reabre la conversación de protección)
- Selección de favoritas / comparar fotos
- Múltiples fotógrafos (multi-tenant) si otros colegas lo quieren usar
- Estadísticas por evento (fotos más vendidas, ticket promedio)
- Limpieza automática de storage de eventos cerrados

## Deploy (transversal, decidir al final de Fase 2)

- **Prender `AuthorizationEnabled=true` globalmente + sembrar el catálogo de permisos de plataforma** (decisión confirmada por Alberto 2026-07-18, ver docs/05 y ADR-11 en docs/04). Es un rollout de la plataforma heredada completa (no solo Fotos): diseñar y cargar permiso→endpoint→rol para TODOS los endpoints admin (Eventos/Grupos/Fotos del vertical + lo heredado de CodigoBase), y reparar los tests que hoy asumen el host con authz off. La defensa puntual (`FamiliaSessionGuard`) queda como capa adicional, no se saca al hacer esto.
- Elegir hosting (Railway / Fly.io / VPS Hetzner) y crear el bucket R2 productivo
- CI mínimo (build + tests en push)
- Backups de PostgreSQL
- Dominio + HTTPS
