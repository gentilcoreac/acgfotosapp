## 1. Fix de nitidez en la composición de capas

- [x] 1.1 En `ImageSharpImageProcessor.ComponerCapa`, generar el `SKImage` (con mipmaps) de cada capa
      una sola vez por llamada en vez de dibujar el `SKBitmap` decodificado directo, para no pagar el
      costo de generación de mip por cada tile del modo Repetida.
- [x] 1.2 Setear `IsAntialias = true` en el `SKPaint` usado para dibujar la capa.
- [x] 1.3 Pasar un `SKSamplingOptions` con filtro lineal + mipmap lineal al `DrawImage`/`DrawBitmap`
      (posición fija y repetida, ambos casos).
- [x] 1.4 Verificar manualmente con una foto real: comparar el derivado antes/después del fix con una
      capa repetida (el caso con más tiles rotados/escalados) y con una capa en posición fija.
      Confirmado con un test temporal (no commiteado) que regenera el watermark legado repetido
      (`marca-agua-default.png`, rotado -26.565°) antes/después vía `git stash`: el recorte 4x sin
      filtro muestra el dentado real "antes" y bordes lisos "después" — mismo texto, mismo ángulo,
      sólo cambia el fix.
- [x] 1.5 Verificar que la suite de integración de Fotos sigue verde, en particular
      `ImageProcessorTests` y `BlendModeParityTests` (no deberían verse afectados — no tocan rotación
      ni escalado, pero confirman que no se rompió nada del resto del pipeline). 551/551 verde.
- [x] 1.6 Actualizar `docs/04-decisiones.md` (ADR-16) si el fix implica una decisión nueva sobre el uso
      de SkiaSharp (sampling/mipmaps), o dejar constancia en el propio código si es un detalle de
      implementación sin impacto en la decisión ya tomada. Agregada addenda fechada (2026-08-06) al
      final de ADR-16.

## 2. Separación de la trama repetida, configurable e independiente de la escala

- [x] 2.1 Agregar `SeparacionPorcentaje` a `CapaMarcaAgua` (dominio) y a su EF config, con el default
      que reproduce la densidad vigente; migración EF con ese mismo default para las filas existentes.
- [x] 2.2 Propagar el campo por la capa de aplicación: `CapaComposicion`, DTOs de entrada/salida,
      `MarcaAguaMapper` y el validator (rango admitido, mensaje que diga qué significa el número).
- [x] 2.3 Reemplazar `PitchXFactor`/`PitchYFactor` en `ImageSharpImageProcessor.ComponerCapa` por el
      cálculo derivado de `SeparacionPorcentaje`, conservando la relación de aspecto del patrón
      ladrillo.
- [x] 2.4 Tests de integración: achicar la escala con la misma separación no cambia la cantidad de
      repeticiones; aumentar la separación con la misma escala las reduce; una capa sin valor explícito
      (perfil viejo) mantiene la densidad previa.
- [x] 2.5 Front — editor de perfiles: control de separación en la capa activa (sólo con modo Repetida),
      con la etiqueta y ayuda que expliquen la relación con la escala.
- [x] 2.6 Front — `marca-agua-canvas.util.ts`: aplicar la misma fórmula de separación que el backend,
      para que la vista previa siga prediciendo el resultado real (ADR-15 D1). Spec de front que fije
      esa correspondencia.
- [x] 2.7 Verificación manual end-to-end contra la API/front dev: partir del perfil real que motivó el
      reporte (capa a 20% de escala, opacidad 1.0), subir la separación, regenerar y confirmar que la
      foto vuelve a verse.
- [x] 2.8 Suites verdes (integración backend + unit front + lint) y documentación al día
      (docs/04-decisiones.md ADR-15/16 y docs/05-notas-abiertas.md).
