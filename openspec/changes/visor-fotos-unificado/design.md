## Context

Ver proposal.md - Why para la motivación. Estado del código relevante para el enfoque:

- `tbi-carousel` (`shared/ui/tbi-carousel/`, ~80 líneas) ya resuelve el recorrido: recibe `items`,
  proyecta un `<ng-template>` del caller para renderizar el ítem actual, y maneja navegación y el
  contador de posición. Nació genérico a propósito (no conoce el dominio de fotos) y ya protege el
  caso `items` vacío.
- `FotoPreviewDialogComponent` (~120 líneas, admin): `MatDialog` con toggle vista-cliente/original,
  descarga del original, y la foto vía `tbi-foto-img` (blob autenticado). **Sin navegación.**
- `FotoFamiliaPreviewDialogComponent` (~240 líneas, familias): `MatDialog` con navegación propia
  (flechas + teclado), layout de 4 esquinas (posición, nombre de la familia, nombre de archivo,
  cerrar), `tbi-agregar-carrito` inline, y la foto vía `tbi-foto-familia-img` (que aporta el sello
  tileado con el nombre de la familia y la fricción anti-copia).

Lo que difiere entre los dos NO es el visor: es qué componente pinta la imagen y qué acciones se
ofrecen. Eso es exactamente lo que `tbi-carousel` ya sabe delegar al caller vía template proyectado.

## Goals / Non-Goals

**Goals:**
- Un solo lugar donde vive el comportamiento de "abrir en grande y recorrer", reusable por cualquier
  pantalla futura sin copiar un diálogo.
- Que las cuatro pantallas actuales queden con el mismo comportamiento, sin perder nada de lo que hoy
  las diferencia legítimamente.
- Que agregar el visor a una pantalla nueva sea declarar la colección y el template del ítem.

**Non-Goals:**
- No se rediseña visualmente ninguna de las cuatro pantallas: el objetivo es unificar el mecanismo, no
  cambiar cómo se ven. El layout de 4 esquinas de familias y el toggle del admin se conservan tal cual.
- No se toca backend, ni el alcance por sesión de familia, ni los endpoints de derivados.
- No se generaliza a "cualquier imagen de la app" fuera del vertical Fotos (p. ej. la foto de perfil de
  usuario de la plataforma heredada queda afuera).

## Decisions

**Un componente de visor en `shared/ui` construido sobre `tbi-carousel`, con el contenido del ítem
proyectado por el caller.**
El visor aporta el chrome común: apertura como diálogo casi a pantalla completa, cierre, indicador de
posición, navegación por flechas y teclado. El caller aporta (a) la colección, (b) el índice inicial y
(c) el template que pinta la foto y sus acciones. Así `tbi-foto-img` sigue en el admin y
`tbi-foto-familia-img` en familias, sin que el visor conozca ninguno de los dos ni la diferencia entre
derivado y original.

Alternativas consideradas:
- **Un único componente con banderas (`esAdmin`, `permiteOriginal`, `muestraCarrito`)**: menos archivos,
  pero mete la distinción admin/familia adentro de una pieza compartida. Es justo el tipo de rama que
  un día se filtra al flujo de las familias — el mismo razonamiento por el que "modo fotógrafo" se
  decidió como pantalla aparte y no como bandera de `/mi-album` (docs/05-notas-abiertas.md). Descartada.
- **Migrar sólo el diálogo de familias y dejar el admin como está**: menos trabajo, pero deja la galería
  y pedidos sin navegación (lo que el usuario pidió) y no crea la pieza reusable.
- **Una directiva sobre la grilla que abra el visor**: más ergonómico para el caller, pero acopla el
  visor a una estructura de grilla concreta; las cuatro pantallas tienen grillas distintas (galería
  admin, tarjetas de carrito agrupadas por foto, grilla de densidad variable de mi-álbum).

**El orden de migración es admin primero, familias después.**
El diálogo admin es el más chico y el que hoy no tiene navegación, así que sirve de validación del
contrato con el menor riesgo. Familias es el que más comportamiento propio tiene (sello, anti-copia,
carrito, layout de 4 esquinas) y el que está en manos de usuarios reales.

## Risks / Trade-offs

- [Regresión en la zona de familias, que ya está en uso] → Migrar último, con la suite unit del front
  como red y verificación manual del flujo completo (abrir desde la grilla, recorrer con filtro
  aplicado, agregar al carrito desde el visor).
- [Perder alguna sutileza del layout de 4 esquinas o de la fricción anti-copia al mover código] → Esos
  elementos viven en `tbi-foto-familia-img` y en el template del caller, no en el visor: la migración no
  los reescribe, los reubica. Revisar explícitamente que `@media print` y `contextmenu` sigan aplicando.
- [El contrato del visor queda corto para un caso futuro] → Se acepta: se diseña para los cuatro usos
  reales que existen, no para casos hipotéticos. Ampliarlo después es barato; adivinar ahora, no.

## Migration Plan

Sin migración de datos. Cambio sólo de frontend, desplegable de una vez. Los dos diálogos actuales se
eliminan recién cuando sus usos ya pasaron al visor, de modo que en ningún commit intermedio quede una
pantalla sin visor.
