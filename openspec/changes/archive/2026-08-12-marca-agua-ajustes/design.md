## Context

`ImageSharpImageProcessor.ComponerCapa` (ver proposal.md - Why) decodifica el asset de la capa una vez
por llamada (`SKBitmap.Decode`) y lo dibuja repetidas veces sobre un `SKCanvas` con
`canvas.DrawBitmap(asset, destRect, paint)`:
- **Posición fija**: una sola vez, con `canvas.RotateDegrees` alrededor del centro del asset.
- **Repetida**: decenas de veces en mosaico, con `canvas.RotateDegrees` una sola vez para todo el
  canvas (no por tile).

El `SKPaint` de esa llamada sólo setea `BlendMode` y `Color` (para la opacidad) — no toca
`IsAntialias` (default `false` en SkiaSharp) ni pide un `SKSamplingOptions` de calidad, así que la
combinación rotación+escalado queda con el peor caso: sin antialiasing en los bordes rotados y con el
muestreo por defecto (equivalente a nearest-neighbor) en el escalado.

El código anterior (pre-ADR-15, `AplicarWatermark` con `ImageSharp.DrawText`) no tenía este problema
porque dibujaba el glyph vectorial directo a la resolución final de cada derivado — no había un paso de
"rasterizar en una resolución, después escalar/rotar ese raster" como ahora.

## Goals / Non-Goals

**Goals:**
- Que una capa rotada y/o escalada salga con los mismos bordes lisos que el resto de la imagen (WebP
  ya introduce su propia compresión con pérdida — no hace falta sumarle aliasing de composición encima).
- No introducir una regresión de performance perceptible en el modo Repetida (decenas de `DrawBitmap`
  por foto, en el worker de procesamiento en background).
- Que el fotógrafo pueda regular tamaño y densidad de la trama como dos ejes independientes, y que
  ningún perfil ya cargado cambie de aspecto por este cambio.

**Non-Goals:**
- No se toca el resto del pipeline (`GenerarDerivado`, resize, encode WebP) — eso queda para la deuda
  técnica ya anotada de migrar todo a SkiaSharp (docs/05-notas-abiertas.md).
- No se rediseña el editor de perfiles más allá de sumar el control nuevo.
- No se cambian los valores del perfil "Estándar" sembrado ni se corrigen perfiles ya cargados por el
  fotógrafo: el fix da la perilla que falta, no decide por él.

## Decisions

**Antialiasing + sampling de calidad en el `SKPaint`/`DrawBitmap` de `ComponerCapa`.**
`paint.IsAntialias = true` (suaviza los bordes del bitmap al quedar en un ángulo no ortogonal tras
`RotateDegrees`) y un `SKSamplingOptions` con filtro lineal + mipmap (`new SKSamplingOptions(SKFilterMode.Linear,
SKMipmapMode.Linear)`) para el escalado, pasado al overload de `DrawBitmap`/`DrawImage` que lo acepta.

Alternativas consideradas:
- **Sólo `IsAntialias`, sin tocar el sampling**: más simple, pero no resuelve el escalado (el caso más
  visible: el tile del watermark por defecto se reduce de 1516px a ~850px en un preview de 900px —
  ratio ~0.56, terreno propenso a aliasing sin un filtro de calidad).
- **Resampler cúbico (`SKCubicResampler.Mitchell`)** en vez de lineal+mipmap: mejor nitidez percibida
  en downscales grandes, pero sin mipmap es más caro por tile — en el modo Repetida (decenas de tiles
  por foto) el costo se paga una vez por tile en vez de reusar niveles de mip. Se descarta por ahora;
  si la calidad con lineal+mipmap no alcanza, es el siguiente paso a probar.
- **Generar el `SKImage` con mipmaps una sola vez por capa** (en vez de dibujar el `SKBitmap` crudo
  repetidas veces): más eficiente en el modo Repetida porque Skia reusa los niveles de mip entre draws.
  Se adopta como parte de la implementación (no cambia el comportamiento observable, es un detalle de
  cómo se logra el sampling de calidad sin pagar el costo N veces).

**Separación como % del tamaño del tile, un solo valor por capa.**
`CapaMarcaAgua` gana `SeparacionPorcentaje` (float, columna nueva con default). El cálculo del pitch
pasa de `anchoDestino * 1.25f` / `altoDestino * 2.2f` a derivarse de ese valor, manteniendo la relación
de aspecto entre paso horizontal y vertical que ya tenía el patrón ladrillo. El default se elige para
reproducir exactamente la densidad vigente, de modo que la migración no altere ningún perfil existente
(requisito del spec) — los multiplicadores actuales dejan de estar hardcodeados pero siguen siendo el
punto de partida.

La misma fórmula tiene que vivir en `marca-agua-canvas.util.ts` (front): ADR-15 D1 puso la vista previa
como garantía de "esto es lo que va a salir", y una fórmula de colocación distinta a cada lado rompe esa
garantía tanto como lo haría un motor de texto distinto.

Alternativas consideradas:
- **Separación en % del ancho de la foto**: más predecible en abstracto ("una marca cada 25% de la
  foto"), pero desacopla la separación del tamaño de la marca de una forma que obliga a re-tocar la
  separación cada vez que se cambia la escala para que las marcas no se pisen. Descartada: cambia un
  acoplamiento molesto por otro.
- **Un slider de "densidad" (pocas ↔ muchas)** que calcule el espaciado: más simple de entender, pero
  esconde el número real y no le sirve a quien quiere una trama puntual; además obliga a definir una
  curva arbitraria entre "pocas" y "muchas".
- **Dos valores separados (horizontal y vertical)**: más control, pero duplica la perilla para un caso
  que no apareció — se puede agregar después sin romper nada si el uso real lo pide.

## Risks / Trade-offs

- [Mipmap añade un costo único de generación por capa] → Mitigado por reusarlo entre los N draws de un
  mosaico (se genera una vez, no por tile) — el worker de procesamiento ya es background, no bloquea
  request.
- [No hay test automatizado de calidad de imagen en el repo — `BlendModeParityTests` verifica la
  fórmula de blend sobre bloques planos sin rotación a propósito] → Verificación manual: regenerar una
  foto de referencia con una capa repetida (el caso más exigente, muchos tiles rotados y escalados) y
  comparar visualmente antes/después del fix. No se agrega un test de píxeles (sería frágil: cualquier
  cambio de versión de Skia correría el riesgo de romperlo sin que la calidad realmente empeore).

## Migration Plan

El fix de nitidez no necesita migración: es interno a `ComponerCapa`.

La separación configurable sí agrega una columna a `fot_capasmarcaagua`, con default igual a la
densidad vigente — las filas existentes quedan con el valor que reproduce su aspecto actual, así que la
migración es segura de aplicar sin coordinar con nadie y no requiere regenerar nada.

En ambos casos, regenerar las fotos de un evento (acción ya existente, `regeneracion-derivados`) es lo
que hace que los eventos ya procesados adopten los cambios; un evento nuevo los tiene desde el primer
procesamiento.
