## Why

Al usar `marca-agua-configurable` (ADR-15/16) en la práctica aparecieron dos problemas distintos que
juntos hacen que la marca de agua quede ilegible sobre una foto real.

**(a) Nitidez.** El texto y los logos salen con bordes dentados/con moiré, notablemente peor que el
watermark de texto anterior. La migración a SkiaSharp (ADR-16) para poder componer capas cambió el
pipeline de "dibujar el texto directo a la resolución final" a "rotar y escalar un PNG ya rasterizado",
y ese paso de composición no pide antialiasing ni un filtro de muestreo de calidad, así que degrada lo
que antes salía liso.

**(b) Densidad atada a la escala.** El espaciado de la trama repetida está fijo en código como múltiplo
del tamaño del propio tile (1.25× ancho, 2.2× alto). La consecuencia es que **achicar la marca
multiplica la cantidad de repeticiones**: es exactamente lo contrario de lo que se busca al bajar la
escala, y no existe ninguna forma de pedir "marcas más chicas pero igual de separadas". Verificado
contra un caso real (perfil "Estándar" del tenant de producción de dev, 2026-08-06): una capa a 20% de
escala y opacidad 1.0 genera más de 120 repeticiones opacas que tapan la foto por completo. El
fotógrafo no puede corregirlo desde el editor porque la separación no existe como parámetro.

(Nota: las otras dos mejoras que se habían pedido para el editor de `/fotos/marca-agua` — ver las 3
muestras simultáneas y "probar con una foto mía" — ya estaban resueltas en el commit `e03e10c`
(grupo 11) antes de abrir este change; `docs/05-notas-abiertas.md` sólo estaba desactualizado. No
forman parte de este change.)

## What Changes

- La composición de capas (`ImageSharpImageProcessor.ComponerCapa`) pasa a antialiasar y a usar un
  filtro de muestreo de calidad al rotar/escalar el asset de cada capa, en vez de la configuración por
  defecto de SkiaSharp (sin antialiasing, sin filtro de calidad).
- Una capa repetida gana **separación** como parámetro propio, expresada en % del tamaño del propio
  tile, independiente de la escala: 100% deja las marcas pegadas, 220% deja un hueco de más de un tile
  entre marcas. Reemplaza los multiplicadores fijos en código; el default reproduce exactamente la
  densidad actual, así que ningún perfil existente cambia de aspecto sin que el fotógrafo lo toque.

## Capabilities

### New Capabilities

(ninguna — todo lo de acá extiende la capacidad existente)

### Modified Capabilities

- `marca-agua`: agrega el requisito de que la composición de capas no degrade la nitidez del asset al
  rotar/escalar, y el de que la separación de una trama repetida sea configurable e independiente de la
  escala.

## Impact

- **Backend**: `AcgFotos.Fotos.Infrastructure/Imaging/ImageSharpImageProcessor.cs` (método
  `ComponerCapa`) para ambos ítems. La separación configurable además toca la entidad `CapaMarcaAgua`,
  su EF config, el DTO/validator, el mapper y `CapaComposicion` — **requiere migración EF** (columna
  nueva con default retrocompatible).
- **Frontend**: el editor de perfiles (`perfil-marca-agua-edit.*`) suma el control de separación, y el
  render de vista previa en canvas (`marca-agua-canvas.util.ts`) tiene que aplicar la misma fórmula que
  el backend — si no, la vista previa deja de predecir el resultado real (premisa de ADR-15).
- **Tests**: `BlendModeParityTests` no cubre la nitidez a propósito (usa bloques planos sin rotación ni
  escalado — verifica la fórmula de fusión); esa parte se verifica visualmente. La separación sí es
  verificable automáticamente (conteo/posición de repeticiones para una separación dada).
