## Why

Ver una miniatura en grande es una capacidad transversal del vertical Fotos, pero hoy está resuelta
tres veces distintas y de forma desigual:

- `FotoPreviewDialogComponent` (galería admin, reusado por el detalle de pedidos): abre la foto grande
  con toggle vista-cliente/original, pero **no tiene navegación** — para ver la foto siguiente hay que
  cerrar, buscarla en la grilla y volver a abrir.
- `FotoFamiliaPreviewDialogComponent` (mi-álbum, reusado por el carrito): sí navega, con su propia
  implementación de flechas/teclado escrita antes de que existiera un carrusel genérico.
- `tbi-carousel` (`shared/ui`, creado el 2026-08-04): el visualizador genérico que resuelve
  justamente eso, hoy usado sólo por el comparador de tamaños de `/fotos/publicacion`. Se decidió
  explícitamente no migrar los dos diálogos en ese momento por ser pantallas ya en producción.

La consecuencia práctica es que una pantalla nueva que muestre miniaturas no tiene de dónde tomar el
comportamiento: o duplica un diálogo, o se queda sin visor. Y el usuario ve comportamientos distintos
según desde dónde abra la misma foto.

## What Changes

- Se define **un visor de fotos único a nivel base del vertical**, construido sobre `tbi-carousel`,
  que resuelve "grilla de miniaturas → abrir en grande → navegar entre las fotos de esa colección".
- Los cuatro usos existentes pasan a ese visor: galería admin, detalle de pedidos, mi-álbum y carrito.
  Conservan lo que hoy los diferencia legítimamente (el toggle original/vista-cliente y la descarga
  son del admin; el overlay con el nombre de la familia y la fricción anti-copia son de las familias),
  pero dejan de reimplementar la navegación y el chrome del diálogo.
- La galería admin y el detalle de pedidos **ganan navegación anterior/siguiente**, que hoy no tienen
  (cierra el ítem "lightbox con flechas/teclado" del backlog de Fase 4, docs/03-fases.md).
- `FotoPreviewDialogComponent` y `FotoFamiliaPreviewDialogComponent` se retiran como implementaciones
  independientes.

## Capabilities

### New Capabilities

- `visor-fotos`: ver en grande una foto elegida desde una grilla de miniaturas y recorrer la colección
  desde ahí, con el mismo comportamiento en toda la aplicación.

### Modified Capabilities

(ninguna — las capabilities de backend no cambian; esto es comportamiento de UI que hasta ahora no
estaba especificado)

## Impact

- **Frontend, `shared/ui`**: el visor base y su contrato (colección de fotos, índice inicial, cómo
  cada contexto aporta la imagen y sus acciones propias). `tbi-carousel` ya existe y se reusa.
- **Frontend, features**: `fotos/fotos` (galería), `fotos/pedidos` (detalle), `familia/mi-album`,
  `familia/carrito` — pasan al visor base; se eliminan los dos diálogos actuales.
- **Sin cambios de backend**: los endpoints de thumb/preview/original y el alcance por sesión de
  familia (ADR-06/ADR-11) quedan intactos. El visor de familias **no** puede ganar acceso al original
  por este cambio.
- **Tests**: specs de los dos diálogos actuales se reemplazan por los del visor base más los de cada
  contexto; la suite unit del front queda verde.
