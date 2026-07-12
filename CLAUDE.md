# AcgFotos — guía para el asistente

App de venta de fotos escolares (graduaciones). Un fotógrafo (admin) sube fotos; las familias entran con un código, eligen fotos/tamaños/cantidades y generan pedidos.

**Antes de trabajar, leer los docs**: [docs/03-fases.md](docs/03-fases.md) dice en qué fase estamos y qué sigue; [docs/04-decisiones.md](docs/04-decisiones.md) tiene las decisiones tomadas (no re-discutirlas sin motivo nuevo); [docs/05-notas-abiertas.md](docs/05-notas-abiertas.md) tiene los pendientes.

## Reglas del proyecto

- **Idioma**: documentación, UI y mensajes en **español**. Código (clases, variables) en inglés o español consistente con lo existente; nombres de entidades del dominio en español (Evento, Album, Pedido...) como en docs/02-modelo-datos.md.
- **Premisa de seguridad de imágenes**: los originales NUNCA se sirven por endpoints públicos; todo lo visible por familias lleva watermark y baja resolución; lectura del bucket solo con URLs firmadas. Ver ADR-01 y ADR-06.
- **Simplicidad ante todo**: monolito, sin colas ni servicios extra (ADR-04). Si una feature pide infraestructura nueva, cuestionarlo primero.
- **Al tomar una decisión de diseño relevante**, agregarla como ADR en docs/04-decisiones.md. Al completar ítems de fase, tildar en docs/03-fases.md.
- Mantener docs/05-notas-abiertas.md al día: resolver ⇒ mover la respuesta al doc correspondiente.

## Stack

.NET 10 (ASP.NET Core, EF Core, ImageSharp) · Angular · PostgreSQL · storage S3-compatible (R2 en prod, MinIO en dev vía Docker Compose) · Mercado Pago (fase 3).

## Estructura

```
backend/   → solución .NET (Api / Domain / Infrastructure / Tests)
frontend/  → Angular SPA (/admin y zona pública /a/:codigo)
docs/      → fuente de verdad del plan
```

## Comandos (completar cuando exista el esqueleto)

- Backend: `dotnet build` / `dotnet test` desde `backend/`
- Frontend: `npm start` / `ng build` desde `frontend/`
- Infra dev: `docker compose up -d` (PostgreSQL + MinIO)
