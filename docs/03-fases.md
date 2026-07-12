# Roadmap por fases

Cada fase termina en algo usable. No empezar una fase sin cerrar la anterior.

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

## Fase 1 — MVP admin

Objetivo: el fotógrafo puede dejar un evento listo para publicar.

- [x] Login admin (JWT) — heredado de la plataforma, verificado
- [x] CRUD Evento con catálogo TamanoPrecio como colección hija (API `api/fotos/eventos`, 10 tests de integración). Nota: el fotógrafo opera como usuario NO-root de su propio tenant (guard multi-tenant de la plataforma); root + header `SimulatedTenant` en dev/tests
- [ ] CRUD Curso / Álbum (generación de código de acceso al crear álbum)
- [ ] Upload masivo de fotos (multi-archivo, asignando a curso o álbum) con procesamiento automático (thumb + preview con watermark)
- [ ] Vista de galería admin para verificar lo subido
- [ ] Impresión/descarga de tarjetas con código y QR por álbum (para repartir a las familias)

**Terminada cuando**: tu papá (o vos simulándolo) puede crear un evento real completo sin tocar la base de datos.

## Fase 2 — MVP familias + pedidos

Objetivo: una familia real puede hacer un pedido de punta a punta. **Con esto ya se reemplaza el ir casa por casa.**

- [ ] Ingreso por código / link QR → token de sesión del álbum
- [ ] Galería mobile-first (grilla de thumbs, vista ampliada del preview)
- [ ] Fricción anti-copia en la galería: bloquear clic derecho, arrastre e impresión (capa 2 de ADR-01)
- [ ] Carrito: foto + tamaño + cantidad; total en vivo
- [ ] Confirmación de pedido con nombre y teléfono
- [ ] Admin: listado de pedidos por evento, detalle, cambio de estado (Pendiente → Impreso → Entregado)
- [ ] Lista de impresión exportable (agregado por foto/tamaño/cantidad, agrupada por álbum)

**Terminada cuando**: se corre un evento piloto real con al menos una familia de prueba.

## Fase 3 — Pagos y comunicación

- [ ] Mercado Pago Checkout Pro: preferencia al confirmar pedido, webhook de confirmación, estado `Pagado`
- [ ] Opción "pagar en la entrega" (efectivo) como alternativa siempre disponible
- [ ] Links de WhatsApp prearmados (enviar código a la familia, avisar "pedido listo")
- [ ] Expiración de álbumes (aviso de "disponible hasta el X" + cierre automático del evento)

## Fase 4 — Mejoras según uso real (backlog, priorizar con feedback)

- **App móvil híbrida (Capacitor sobre el mismo Angular)** con `FLAG_SECURE` en Android (capturas de pantalla en negro) y detección de captura en iOS — capa 3 de ADR-01. **Prioridad alta del backlog**; adelantable a Fase 3 si el bloqueo de capturas pesa más que el pago online
- Paquetes/promos (ej. "2× 13x18 + 4× 10x15 a $X")
- Venta de foto digital (entrega del archivo limpio tras el pago — reabre la conversación de protección)
- Selección de favoritas / comparar fotos
- Múltiples fotógrafos (multi-tenant) si otros colegas lo quieren usar
- Estadísticas por evento (fotos más vendidas, ticket promedio)
- Limpieza automática de storage de eventos cerrados

## Deploy (transversal, decidir al final de Fase 2)

- Elegir hosting (Railway / Fly.io / VPS Hetzner) y crear el bucket R2 productivo
- CI mínimo (build + tests en push)
- Backups de PostgreSQL
- Dominio + HTTPS
