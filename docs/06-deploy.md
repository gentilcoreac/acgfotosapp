# Deploy e infraestructura (documento de trabajo, en iteración)

Este documento junta las piezas del deploy productivo (storage, base de datos, hosting, dominio,
costos) mientras las vamos decidiendo con Alberto. Todavía **no es un ADR**: cuando el circuito
completo quede decidido, se resume como un ADR nuevo en [04-decisiones.md](04-decisiones.md) y los
pendientes sueltos de [05-notas-abiertas.md](05-notas-abiertas.md) (líneas "Deploy") se tachan.

## 1. Storage de fotos — Cloudflare R2 (ya decidido, ADR-05)

"R2 es barato" mezclaba dos cosas distintas — separado:

| Concepto | Costo | Qué lo genera |
|---|---|---|
| **Storage** (cuántos GB tenés guardados) | $0,015/GB/mes | Los originales + derivados que están en el bucket AHORA, sumen o no tráfico |
| **Operaciones Class A** (escrituras: subir/borrar/listar) | $4,50 por millón | Cada foto que el fotógrafo sube, cada derivado que genera el worker |
| **Operaciones Class B** (lecturas) | $0,36 por millón | Cada vez que una familia ve una miniatura/preview |
| **Egreso** (bytes que salen del bucket hacia el navegador) | **$0, siempre** | Esto es lo que en S3/Azure Blob SÍ se cobra y es la razón real de elegir R2 (ADR-05) |

Free tier **mensual, se renueva cada mes**: 10 GB de storage + 1.000.000 de operaciones Class A +
10.000.000 de operaciones Class B — no es "10 GB una vez", es 10 GB de piso gratis todos los meses.

Con el volumen estimado (500–800 fotos, 3–8 GB de originales por evento, más derivados livianos en
WebP) unos pocos eventos activos por mes entran cómodos dentro del free tier; si se pasa, son
centavos por mes (ver escenarios abajo). Lo que de verdad importa acá es el egreso gratis: es el
costo que en otro proveedor SÍ escala con el uso real de las familias mirando fotos.

## 2. ¿Qué es un VPS?

**VPS (Virtual Private Server)** es una porción de una máquina física en un datacenter, alquilada
por hora/mes, que se comporta como si fuera tu propia PC Linux (o Windows) dedicada:

- Tenés acceso root/administrador — instalás lo que quieras (Docker, PostgreSQL, el runtime de
  .NET, lo que sea).
- Está siempre encendida, con una IP pública fija.
- Vos sos responsable de todo lo que corre ahí: actualizaciones del sistema operativo, parches de
  seguridad, backups, monitoreo de espacio en disco/memoria.

Se diferencia de:

- **PaaS** (Platform as a Service — Railway, Render, Fly.io): no administrás el sistema operativo,
  solo subís código/un contenedor y ellos lo corren. Menos control, pero también menos trabajo de
  infraestructura. Suele costar más por el mismo recurso (pagás esa comodidad).
- **Serverless/edge** (Cloudflare Workers, ver punto 4): ni siquiera hay "un servidor" que
  administrar — es código que se ejecuta a demanda. Mucha menos flexibilidad de runtime a cambio.

## 3. Self-host vs. managed — qué cambia para vos en la práctica

Aplica tanto para la base de datos como, en menor medida, para el backend.

**Self-host** (ej. Postgres corriendo en tu propia VPS, junto a la API):
- Más barato (no pagás la capa de gestión de un proveedor).
- Backups: los armás vos (ej. `pg_dump` por cron, subido a R2) — es el ítem pendiente anotado en
  notas abiertas ("Backups automáticos de PostgreSQL").
- Actualizaciones de versión de Postgres, parches de seguridad del SO: responsabilidad tuya.
- Si la API y la base compiten por CPU/memoria en la misma máquina chica, un pico de una afecta a
  la otra.

**Managed** (ej. Neon, Supabase, Railway Postgres):
- El proveedor corre Postgres por vos: backups automáticos y point-in-time restore ya vienen
  incluidos (resuelve el pendiente de arriba sin escribir nada).
- No tenés acceso SSH a "la máquina de la base" — solo un connection string.
- Algunos tienen particularidades: Neon en el free tier **suspende el compute cuando está
  inactivo** (la primera consulta después de estar inactiva tarda un poco más en "despertar" —
  irrelevante para el volumen de este proyecto, pero vale saberlo).
- Con volumen bajo, suele ser gratis o casi gratis igual (ver escenarios).

## 4. ¿Qué son los Workers? ¿Quién puede correr ahí?

**Cloudflare Workers** son funciones *serverless* que corren en el borde de la red de Cloudflare
(cientos de ciudades a la vez, no un datacenter fijo). NO son una VPS: no hay sistema operativo
completo, no hay disco persistente tradicional, y sobre todo — **el runtime es limitado**: solo
corren JavaScript/TypeScript nativamente, o lenguajes compilados a WebAssembly (Rust, C/C++,
Python en beta). **No soportan contenedores Docker arbitrarios ni el runtime completo de .NET
(CLR)** — un backend ASP.NET Core no puede vivir ahí, punto.

Por eso, en este proyecto:

- El **backend** (.NET) necesita sí o sí un VPS o un PaaS tradicional (punto 2) — Workers queda
  descartado para él, no es una opción real.
- El **frontend** (Angular ya compilado a HTML/CSS/JS estático) SÍ puede vivir en la red de
  Cloudflare, vía **Cloudflare Pages** — que por debajo usa la misma infraestructura de Workers para
  servir archivos estáticos, pero no hace falta escribir código de Workers para usarlo: es "subís
  la carpeta `dist/`, listo".

**Repaso de Hyperdrive con esto ya explicado**: Hyperdrive solo tiene sentido si tu *backend*
corriera en Workers (para poolear las conexiones cortas y frecuentes que generaría cada invocación
de un Worker contra una base tradicional). Como nuestro backend es un proceso .NET tradicional
(long-running, con su propio pool de conexiones de Npgsql), Hyperdrive no resuelve nada acá — se
descarta.

## 5. Escenarios de costo mensual

| Escenario | Backend | Base de datos | Fotos | Frontend | Dominio | **Total/mes** |
|---|---|---|---|---|---|---|
| **A — todo self-managed** | VPS Hetzner CX22, 2vCPU/4GB (~$4,60) | Postgres en la misma VPS (Docker) | R2 (~free tier) | Cloudflare Pages (gratis) | ~$1 prorrateado | **~$6** |
| **B — mínima ops (recomendado)** | VPS Hetzner CX22 (~$4,60) | **Neon free tier** ($0) | R2 (~free tier) | Cloudflare Pages (gratis) | ~$1 | **~$5–6** |
| **C — todo gestionado (PaaS)** | Railway Hobby ($5 base + consumo, real $6–12) | Railway Postgres addon o Neon aparte | R2 (~free tier) | Cloudflare Pages (gratis) | ~$1 | **~$12–20** |

B iguala el costo de A pero sin el trabajo de mantener/respaldar Postgres a mano (Neon lo hace),
sin acoplar API y base en la misma máquina. Encaja con ADR-04 ("hosting simple y barato").

## 6. Decisiones pendientes (a marcar acá mientras se cierran)

- [ ] Elegir escenario (A/B/C) o una variante.
- [ ] Si el escenario usa VPS: elegir proveedor (Hetzner vs. DigitalOcean vs. otro) y región (cerca
  de Argentina si importa la latencia con las familias).
- [ ] Dominio: elegir registrador y nombre.
- [x] ~~Migración SQL Server → PostgreSQL~~ **RESUELTA (2026-07-24, ADR-14)** — condición previa
  para cualquier escenario de arriba, ya lista: el backend corre sobre Npgsql, suite de integración
  508/508 en verde contra Postgres local.
- [ ] Política de retención de fotos tras `FechaExpiracion` (pendiente ya anotado en notas
  abiertas) — impacta directamente cuántos GB de R2 se acumulan con el tiempo.
- [ ] Backups: si se elige self-host de la base (escenario A), definir el mecanismo concreto
  (`pg_dump` + cron + subida a R2) y probarlo con un restore real, no solo confiar en que "corre".

## 7. Entorno local de desarrollo

PostgreSQL 17 instalado y en uso (2026-07-24, ver ADR-14) — el backend corre sobre Npgsql en dev,
tests y (cuando se elija hosting) producción. Credenciales y connection strings de dev en
[credenciales-dev.md](credenciales-dev.md). Pendiente aparte: la base de dev `AcgFotos` tiene el
esquema migrado pero sin datos sembrados (root/tenant/fotógrafo) — no es parte de la migración en
sí, es el mismo bootstrap que haría falta con cualquier motor de base de datos nuevo.
