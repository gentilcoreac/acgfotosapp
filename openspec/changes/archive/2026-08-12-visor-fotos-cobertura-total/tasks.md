## 1. Visor — modo imagen aislada

- [x] 1.1 Ya resuelto sin tocar código: `hayVarias = computed(() => this.items().length > 1)` en
      `tbi-visor-fotos.component.ts` ya oculta flechas y contador con un solo ítem — comportamiento
      existente desde `visor-fotos-unificado`, no una capacidad nueva.
- [x] 1.2 Ya resuelto sin tocar código: `TbiVisorFotosComponent<T>` ya es genérico y el contenido lo
      proyecta el caller vía `<ng-template let-item>` — no hay ningún supuesto de `FotoDto`/`fotoId` en
      el propio visor. Se creó `ImagenAmpliadaDialogComponent` (`shared/ui/imagen-ampliada-dialog`) como
      envoltorio reusable para los casos de imagen suelta (QR, preview de archivo) que abre
      `tbi-visor-fotos` con `items=[data]`.
- [x] 1.3 Specs: ya existía "con una sola foto no ofrece recorrido ni contador" en
      `tbi-visor-fotos.component.spec.ts`. Se sumó `imagen-ampliada-dialog.component.spec.ts`
      (verifica que el envoltorio no renderiza `.nav` ni `.contador`).

## 2. `/fotos/tarjetas` — QR ampliable

- [x] 2.1 `TarjetasComponent.verQrAmpliado()` abre `ImagenAmpliadaDialogComponent` con el QR en base64
      como `src`, desde un botón que envuelve `<img class="tarjeta__qr">`.
- [x] 2.2 La impresión sigue intacta: `imprimir()` no se tocó, sigue armando su propio HTML con los QR
      embebidos aparte.
- [x] 2.3 Specs nuevos en `tarjetas.component.spec.ts`: click en el QR abre el diálogo con `src`/`alt`
      correctos; sin código no se ofrece el botón.

## 3. `/fotos/publicacion` — comparador migrado al visor único (D1)

- [x] 3.1 **Desviación de diseño**: no hizo falta armar una colección "sintética" con shape propio — el
      `ResultadoVista[]` que el comparador ya calculaba (`ladoMayorPedido`, `ancho`, `alto`, `dpi`,
      `pesoBytes`, `previewUrl`) se pasa tal cual a `tbi-visor-fotos`, el mismo dato que ya consumía el
      `<ng-template>` de `tbi-carousel`.
- [x] 3.2 `ComparadorTamanosComponent` reemplaza `TbiCarouselComponent` por `TbiVisorFotosComponent`
      (`[mostrarCerrar]="false"`: sin diálogo alrededor no hay `MatDialogRef` para el botón de cerrar,
      que usa la directiva `mat-dialog-close`).
- [x] 3.3 Los datos por muestra (título, dimensiones, dpi, peso, aviso de dpi bajo) siguen en el mismo
      `<ng-template>`, sin cambios.
- [x] 3.4 Confirmado: tras la migración `tbi-carousel` quedó sin otros callers reales (solo su propio
      spec). Anotado en docs/05-notas-abiertas.md como candidato a retirarse — no se borró en este
      change.
- [x] 3.5 **Sin spec de componente nuevo para `ComparadorTamanosComponent`**: no tenía spec antes de
      este change y levantar cobertura nueva de cero excedía el alcance. `ComparadorAmpliadoDialogComponent`
      (agregado en la corrección de abajo) sí tiene spec propio.
- [x] 3.6 **Corrección tras verificación manual (Alberto, 2026-08-12)**: 3.1-3.5 migraron el carrusel
      INLINE de `tbi-carousel` a `tbi-visor-fotos`, pero eso no agregaba ningún "click para ampliar" —
      la muestra ya se mostraba inline a ese mismo tamaño, sin ninguna miniatura de la que partir. Faltaba
      la pieza real: nuevo `ComparadorAmpliadoDialogComponent` (mismo `<ng-template>`, sin duplicar lógica
      de instancia porque `formatearPeso`/`DPI_ACEPTABLE` son plano/constante) que se abre con
      `ComparadorTamanosComponent.ampliar(i)` al hacer click sobre la muestra grande — recorrido completo
      de las 5 en un diálogo casi a pantalla completa, mismo patrón que el resto de las pantallas. La
      vista inline se conserva (sigue siendo útil para comparar sin salir de la pantalla) y ahora AMBAS
      cosas conviven. Verificado en vivo con Playwright contra el front/API dev reales.

## 4. `/fotos/marca-agua` — marca renderizada ampliable

- [x] 4.1 `MarcaAguaListComponent.ampliar()` abre `MarcaAguaPreviewAmpliadaDialogComponent` (nuevo,
      envoltorio sobre `tbi-visor-fotos` + `tbi-perfil-marca-agua-canvas`, re-renderiza a 960×640) desde
      un botón que envuelve el canvas de cada fila.
- [x] 4.2 `PerfilMarcaAguaEditComponent.ampliarMuestra()`: mismo diálogo, una vez por tile de
      `muestras-grilla` (clara/oscura/mixta/foto propia) — cada botón abre SU propia muestra, sin
      colección conjunta entre ellas, como estaba previsto.
- [~] 4.3 Specs: cubierto el listado (`marca-agua-list.component.spec.ts`, nuevo — no existía spec de
      este componente antes) y el diálogo nuevo (`marca-agua-preview-ampliada-dialog.component.spec.ts`).
      **No se agregó spec para el editor** (`perfil-marca-agua-edit.component.ts`): no tenía spec previo
      y levantar el harness completo (Signal Forms, `EditComponentBase`, `lookupResource`,
      `OpcionesPublicacionService`) desde cero para probar un solo botón excedía el alcance de este
      change — el código sigue el mismo patrón ya probado en el listado.
- [x] 4.4 **Bug real encontrado en la verificación manual (Alberto, 2026-08-12): el diálogo ampliado se
      veía en blanco.** No era un problema de layout/CSS (eso ya funcionaba) sino una condición de
      carrera real en `PerfilMarcaAguaCanvasComponent`: el `<canvas>` tenía `[width]="ancho()"
      [height]="alto()"` como bindings de TEMPLATE, y el `effect()` que dibuja podía disparar de forma
      SINCRÓNICA en su primera pasada cuando los assets ya estaban en caché — exactamente el caso nuevo
      que este change introdujo (`MarcaAguaPreviewAmpliadaDialogComponent` reusa un perfil que el listado
      ya cacheó, así que no hay round-trip de red que le dé tiempo a Angular a aplicar el binding antes).
      El efecto dibujaba sobre un canvas todavía en su tamaño default (300×150); cuando Angular aplicaba
      el binding `[width]`/`[height]` después, redimensionar el canvas limpia su bitmap por spec del
      elemento — aunque el valor final fuera el mismo — borrando todo lo recién pintado. Diagnosticado en
      vivo con Playwright (instrumentando `fillRect`/`getContext` y leyendo píxeles del canvas real, no
      adivinado) contra el front/API dev. Fix: `render()` fija `canvas.width`/`canvas.height` a mano,
      ANTES de dibujar, y el template dejó de tener esos bindings (una sola fuente de verdad para el
      tamaño, sin carrera posible). Afecta a TODOS los usos de `tbi-perfil-marca-agua-canvas`, no sólo al
      diálogo ampliado — el bug ya existía, este change sólo fue el primero en exponerlo (los demás casos
      siempre pasan por al menos un round-trip de red antes del primer dibujo real, que le daba tiempo a
      Angular a aplicar el binding). Verificado con lectura de píxeles reales (`getImageData`) antes y
      después del fix, no sólo visualmente.

## 5. `tbi-agregar-carrito` y `tbi-file-upload`

- [x] 5.1 **Desviación de diseño real vs. D2**: `tbi-agregar-carrito` en sí NUNCA tuvo una miniatura
      propia (se verificó leyendo el componente) — el gap real estaba en
      `AgregarCarritoBottomSheetComponent` (el bottom sheet que abre el botón "+" de la grilla), que no
      mostraba ninguna foto, solo el selector de tamaño/cantidad. Se le agregó una miniatura
      (`tbi-foto-familia-img`, 56px) que al hacer click cierra el sheet y delega en
      `MiAlbumComponent.verPreview(foto)` — el visor de la grilla, con recorrido completo — vía un
      callback `onAmpliar` en `AgregarCarritoBottomSheetData`. Mismo espíritu que D2 (sin visor propio
      para el sheet), ajustado a lo que el código realmente tenía.
- [x] 5.2 `TbiFileUploadComponent`: la vista previa (`.tbi-file__preview`) se envolvió en un botón que
      abre `ImagenAmpliadaDialogComponent` con el `previewUrl()` actual (data URL del archivo elegido, o
      `currentUrl()` en modo edición) — imagen aislada, sin recorrido.
- [x] 5.3 Specs nuevos: `agregar-carrito-bottom-sheet.component.spec.ts` (no existía; miniatura visible,
      ampliar cierra el sheet y delega), 2 casos nuevos en `mi-album.component.spec.ts` (el sheet recibe
      `onAmpliar` y dispara el diálogo con el índice correcto), y en
      `tbi-file-upload.component.spec.ts` (ampliar abre el diálogo con el `alt` correcto; sin archivo no
      se ofrece el botón).

## 6. Cierre

- [x] 6.1 Suite unit del front + lint verdes: **564/565 tests + lint sin errores** (1 fallo,
      `usuario-edit.component.spec.ts`, timeout de aislamiento — no toca nada de este change; pasa
      10/10 corrido solo. Mismo patrón de flake ya documentado en docs/05-notas-abiertas.md).
- [x] 6.2 Verificación manual end-to-end contra la API/front dev, con Playwright dirigido en vivo (no
      screenshots ciegos): **primera vuelta encontró 2 bugs reales que la suite unit no detectaba**
      (Alberto probó a mano y los reportó) —
      - `/fotos/marca-agua`: diálogo en blanco → condición de carrera real en
        `tbi-perfil-marca-agua-canvas` (ver 4.4), NO un problema de layout. Corregido y verificado con
        lectura de píxeles del canvas real.
      - `/fotos/publicacion`: el click no hacía nada → 3.1-3.5 habían migrado el carrusel INLINE pero
        nunca agregaron un "click para ampliar" real (ver 3.6). Corregido con
        `ComparadorAmpliadoDialogComponent` y verificado (el diálogo abre, navega, muestra los datos).
      - `tbi-file-upload`: Alberto no encontraba dónde probarlo — verificado que SÍ funciona
        (`/tenants` → ✏️ Editar → tab "Estilos" → elegir archivo → click en la miniatura). No era un bug,
        sino falta de indicación de dónde mirar (aviso para la próxima).
      - `/fotos/tarjetas`: confirmado por Alberto, funciona.
      **Segunda vuelta (2026-08-12), con los 2 fixes puestos**: Alberto confirmó los 4 casos
      funcionando ("ok funcionan bien"). Sin verificar puntualmente en mobile — no bloqueante, mismo
      criterio responsive que el resto de la app, nada en este change depende del viewport.
- [x] 6.3 Documentación: `docs/03-fases.md` ya tenía la referencia al change (agregada al proponerlo);
      `docs/05-notas-abiertas.md` tiene la nota de `tbi-carousel` candidato a retirarse (3.4). Sin nota
      pendiente que limpiar.
- [x] 6.4 `ImagenAmpliadaDialogComponent` y `MarcaAguaPreviewAmpliadaDialogComponent` cambiaron
      `[items]="[data]"` (array nuevo en cada chequeo de CD) por una referencia estable (`items = [this.data]`
      como campo de clase) — no era la causa del bug de 4.4 (confirmado: seguía en blanco incluso con la
      referencia estable), pero es una corrección de buena práctica que se deja hecha igual, evita un
      riesgo latente parecido en el futuro.
