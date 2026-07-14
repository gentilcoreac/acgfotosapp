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
- [x] CRUD Evento con catálogo TamanoPrecio como colección hija (API `api/fotos/eventos`, 10 tests de integración). Nota: el fotógrafo opera como usuario NO-root de su propio tenant (guard multi-tenant de la plataforma); root + header `SimulatedTenant` en dev/tests. **ABM admin en el front** (`/fotos/eventos`: listado + edit-dialog con la grilla de tamaños; requiere seed de menú, ver docs/05)
- [x] CRUD Curso / Álbum (API `api/fotos/cursos`, álbumes como colección hija; al crear un álbum se le genera su código de acceso `XXXX-XXXX`, 11 tests de integración). Guards: el EventoId del input debe existir en el tenant, y no se borran cursos/álbumes con fotos. **ABM admin en el front** (`/fotos/cursos`: listado con filtro por evento + edit-dialog con la grilla de álbumes y su código)
- [x] Upload masivo de fotos (multi-archivo a curso o álbum: `POST api/fotos/fotos/upload`) con procesamiento en background (cola en memoria + `FotoProcesamientoWorker`, `Pendiente → Lista/Error`): originales al storage privado, thumb + preview con watermark; 8 tests de integración. Config del watermark en `Fotos:TextoWatermark`. **Pantalla en el front** (`/fotos/subir`: cascada evento→curso→grupales/álbum, subida por tandas de 10 con progreso, tabla de estado con auto-refresco mientras procesa)
- [x] Galería admin (API): entrega de thumb/preview con watermark (`GET api/fotos/fotos/{id}/thumb|preview`, 404 hasta estar Lista), descarga del original limpio (`{id}/original`, único camino al original — ADR-06) y borrado completo de foto (fila + storage); 5 tests de integración. **Pantalla en el front** (`/fotos/galeria`: grilla de thumbs por curso con filtro por álbum, preview ampliado en diálogo, descarga del original y borrado con confirmación; las imágenes van por blob autenticado — `tbi-foto-img`)
- [x] Tarjetas con código y QR por álbum (API): `GET api/fotos/cursos/{id}/tarjetas` devuelve una tarjeta por alumno con código activo, URL de canje (molde `Fotos:UrlCanjeTemplate`) y QR PNG en base64 (QRCoder); el front las renderiza e imprime; 2 tests de integración

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
- **Eventos genéricos, no solo colegios** (pedido 2026-07-13): cumpleaños, deportes, bautismos, etc. El modelo ya casi lo banca (`Colegio` es opcional); revisar naming en UI y modelo — `Colegio` → algo como "Lugar/Organización", y si `Curso`/`Álbum (alumno)` deberían mostrarse como "Grupo"/"Participante" según el tipo de evento
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
