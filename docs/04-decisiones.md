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

*(Actualizado por ADR-09: la base de datos es SQL Server, no PostgreSQL como decía la versión original de este ADR.)*

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
- **SQL Server** en lugar de PostgreSQL (el Core está clavado a `UseSqlServer`): en dev ya está; para prod evaluar SQL Server Express en VPS o SQL Azure. Actualiza ADR-04.
- Sin MinIO/Docker en dev: el Core trae `IStorageProvider` con FileSystem. Actualiza ADR-05.
- Se hereda complejidad que AcgFotos no usa hoy (multi-tenant, licencias, grupos): se ACEPTA y no se poda — multi-tenant mapea al futuro "otros fotógrafos" y podar la plataforma rompería la posibilidad de traer fixes del código base.
- Verificación del fork: suite de integración 419/419, Vitest 325/325, lint OK.
