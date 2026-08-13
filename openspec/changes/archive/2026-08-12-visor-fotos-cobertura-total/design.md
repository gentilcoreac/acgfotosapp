## Context

`tbi-visor-fotos` (`shared/ui`, `visor-fotos-unificado`) ya resuelve "grilla → abrir en grande →
recorrer la colección" sobre `tbi-carousel`, con el contrato `{fotos, index, ...}` proyectando
contenido y acciones por contexto (ver `openspec/specs/visor-fotos/spec.md`). Ese contrato asume
siempre una **colección** de `FotoDto`/`FotoFamiliaDto` con `fotoId`. Las cinco pantallas de
`proposal.md` no encajan todas ahí: dos son colecciones reales de fotos de evento, tres son imágenes
sueltas que no tienen `fotoId` ni backing de ningún repositorio de fotos.

## Goals / Non-Goals

**Goals:**
- Definir, por cada uno de los 5 casos, si usa `tbi-visor-fotos` (con colección real o de 1) o un
  visor mínimo aparte, y por qué.
- Que "foto aislada" sea un modo de primera clase del visor único, no un caso especial fuera de él si
  el costo de generalizarlo es bajo.

**Non-Goals:**
- No se rediseña `tbi-carousel` ni el contrato de acciones por contexto ya definido en
  `visor-fotos-unificado`.
- No se toca ningún endpoint ni el alcance por sesión de familia (ver proposal.md — Impact).

## Decisions

**D1 — `/fotos/publicacion` (comparador de tamaños): usa `tbi-visor-fotos` con colección real.**
Las 5 muestras (300/600/900/1200/1600) son variantes de LA MISMA foto, generadas por el propio
comparador (no fotos con `fotoId` propio) — se arma una colección sintética en memoria (mismo shape que
espera el visor: título/subtítulo por ítem en vez de nombre de archivo) y se recorre igual que
cualquier otra. Reemplaza el uso directo de `tbi-carousel` en esa pantalla.

**D2 — `tbi-agregar-carrito`: no abre un visor propio.** Vive siempre embebido en un contexto que YA
tiene su visor (la grilla de `mi-album` o el propio `tbi-visor-fotos` cuando el selector está inline en
el preview ampliado). La miniatura de `tbi-agregar-carrito` dispara la apertura del visor del contexto
padre posicionado en esa foto — no un visor aparte para una miniatura que ya está dentro de otro
visor. Evita anidar diálogos.

**D3 — QR de `/fotos/tarjetas`, canvas de `/fotos/marca-agua` y preview de `tbi-file-upload`: usan
`tbi-visor-fotos` en modo imagen aislada (D4), no un lightbox aparte.** Alternativa descartada: un
componente liviano nuevo solo para "ampliar una imagen suelta" — se rechaza porque tripilcaría chrome
(cerrar, teclado, backdrop) que el visor ya resuelve, por evitar pasarle una colección de longitud 1.
El costo real es que estos tres casos NO tienen `fotoId`/`FotoDto`: el visor necesita aceptar contenido
arbitrario (una imagen ya renderizada — data URL de QR/canvas, o un blob local de archivo elegido) en
vez de sólo fotos del dominio. Se generaliza el input del visor a "colección de ítems que el caller
sabe pintar" (ya es así vía proyección de contenido) con longitud 1 y sin fotoId real.

**D4 — Modo "imagen aislada" = colección de longitud 1.** No se agrega un modo/flag nuevo al visor: se
reusa la colección existente con un solo elemento, y las flechas/contador se ocultan cuando
`total === 1` (comportamiento ya parcialmente cubierto por "extremos de la colección sin envolver" de
`visor-fotos-unificado" — con 1 solo elemento ambos extremos coinciden). Alternativa descartada: un
componente/modo aparte del visor — más superficie para mantener por una diferencia puramente visual
(ocultar 2 controles).

**D5 — Logo del tenant: fuera de alcance, sin cambios.** Confirmado en proposal.md — no es contenido de
un evento, no aplica la regla "toda imagen se amplía".

## Risks / Trade-offs

- [Riesgo] Generalizar el visor a "cualquier imagen, no solo `FotoDto`" puede tentar a meter contenido
  no-imagen más adelante (abrir un PDF, por ejemplo) → Mitigación: el contrato sigue siendo
  "colección de imágenes para ampliar", documentado así en el spec; no se abre la puerta a otros tipos
  de contenido en este change.
- [Riesgo] El QR y el canvas de marca de agua son imágenes GENERADAS en el momento (data URL), no
  bajadas de un endpoint — abrirlas en el visor no debe intentar aplicar lógica pensada para blobs
  autenticados (`tbi-foto-img`/`tbi-foto-familia-img`) → Mitigación: el contenido de cada ítem lo
  sigue proyectando el caller (ya es el patrón del visor), así que un `<img [src]="dataUrl">` simple
  no rompe nada del contrato.

## Open Questions

Ninguna — las tres decisiones de "a qué visor entra cada caso" (D1-D3) quedan resueltas arriba.
