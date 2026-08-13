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

## 3. La marca no se comprime junto con la foto (el problema que originó todo)

El derivado se comprime UNA vez, con la marca ya pegada: la compresión con pérdida degrada por igual
la foto y la marca, y se ensaña justo con lo que la marca tiene (bordes finos, texto chico). El nombre
del fotógrafo es lo que NO puede perder calidad — es la marca de autoría, no un adorno.

- [x] 3.1 Separar la compresión en dos pasos cuando hay capas que componer: comprimir la foto sola a
      la calidad de publicación, volver a decodificarla ya degradada, componer las capas sobre ESA
      imagen y recién entonces codificar el derivado final a la calidad de marcado. Sin capas, el
      camino sigue siendo una sola pasada.
- [x] 3.2 La calidad de marcado viaja como parámetro del procesamiento (no fija en el código del
      processor) y se resuelve desde configuración, de modo que mañana pueda venir del front —
      p. ej. como campo de `OpcionesPublicacion` — sin tocar el pipeline.
- [x] 3.3 Tests: con la misma foto y la misma marca, el derivado de dos pasos conserva la marca más
      nítida que el de una sola pasada; la foto sigue degradada a la calidad de publicación; sin capas
      no cambia nada respecto de hoy.
- [x] 3.4 Verificación visual: la marca sobre una foto real, antes y después, mirando el texto al
      100% y ampliado.
- [x] 3.5 Medir el peso resultante del preview y dejarlo anotado (el fotógrafo eligió priorizar la
      marca impecable por sobre el peso, con la perilla de calidad para ajustarlo en desarrollo).

## 4. Fotos verticales que salen acostadas

La orientación de una foto vertical viaja como una nota en el EXIF ("mostrame rotada"), no en los
píxeles. El pipeline borra el EXIF —a propósito, por GPS y datos del equipo— pero nunca aplica esa
rotación antes de borrarla, así que la foto queda acostada para siempre.

- [x] 4.1 Aplicar la orientación declarada a los píxeles ANTES de limpiar los metadatos.
- [x] 4.2 Test de regresión con una foto que declare orientación rotada: el derivado sale con el alto
      y el ancho intercambiados respecto de la imagen cruda, y sin EXIF.
- [x] 4.3 Verificar con una foto vertical real del fotógrafo (queda en sus manos: subir una vertical
      tomada con celular/cámara y confirmar que se ve derecha en la galería y en el álbum).

## 5. "Demasiadas solicitudes" al usar el editor de marca de agua

Con dos clicks saltaba el tope global de pedidos (100 por minuto). Cada ajuste del editor volvía a
bajar del servidor el PNG de todas las capas, por cada una de las cinco vistas previas en pantalla:
la vista previa se refrescaba comparando el perfil ENTERO, y ajustar un slider crea objetos nuevos.

- [x] 5.1 La vista previa decide si tiene que bajar assets mirando qué assets necesita (perfil +
      claves de storage), no el perfil completo con su colocación.
- [x] 5.2 Las imágenes ya decodificadas se reusan entre vistas previas: el PNG de una capa es
      inmutable, cambiar su contenido significa subir otra capa con otra clave.
- [x] 5.3 La colocación del momento se combina con la imagen cacheada al renderizar, para que ajustar
      un slider siga actualizando la vista previa sin volver a pedir nada.
- [x] 5.4 Verificar en la pantalla real que se puede trabajar sin toparse con el tope.

---

**Siguientes en la fila, fuera de este change** (orden acordado con Alberto):
1. Visor de fotos como estándar del sistema (`visor-fotos-unificado`, replantear como pieza base:
   CUALQUIER imagen de la app se abre grande y se navega igual, no sólo las 4 pantallas actuales).
2. **ÚLTIMA**: tormenta de ideas anti-robo de fotos, mirando FullFoto como referencia y buscando
   estrategias que hoy no estén contempladas.
