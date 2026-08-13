## Why

`visor-fotos-unificado` llevó el visor único (`tbi-visor-fotos`) a las cuatro pantallas que ya
ofrecían un preview ampliado (galería admin, detalle de pedido, mi-álbum, carrito), pero el pedido
original de Alberto era más amplio: **"TODAS las imágenes de la app, al hacer click, se abren en
grande"**. Ese change dejó relevado explícitamente qué falta (2026-08-11, ver
`openspec/changes/archive/2026-08-12-visor-fotos-unificado/tasks.md`, sección "Alcance NO cubierto"):
cinco lugares muestran una imagen y hoy no ofrecen ampliarla.

De esos cinco, dos son colecciones navegables de fotos de evento (encajan derecho en `tbi-visor-fotos`
tal cual existe) y tres son imágenes sueltas ajenas al concepto de "foto de evento" (un QR generado, un
canvas de vista previa, un archivo recién elegido para subir) — para esas, el change tiene que decidir
si entran al mismo visor (colección de 1) o si un tratamiento más liviano es más apropiado, y dejarlo
razonado en `design.md` en vez de asumirlo.

## What Changes

- `/fotos/tarjetas`: el QR de cada participante se puede ampliar al hacer click. Es una imagen aislada,
  sin colección — se abre sola, sin flechas de navegación ni contador de posición.
- `/fotos/publicacion` (comparador): las 5 muestras de tamaño (300/600/900/1200/1600) pasan del
  carrusel propio (`tbi-carousel`, standalone en esa pantalla) a `tbi-visor-fotos`, recorribles igual
  que cualquier otra colección — cierra la duplicación que `visor-fotos-unificado` ya había señalado
  como pendiente.
- `/fotos/marca-agua` (listado de perfiles y editor): la marca renderizada en canvas se puede ampliar.
  Es una imagen aislada (una muestra sintética o la foto propia elegida para probar, no una foto de
  evento), sin colección.
- `tbi-agregar-carrito`: la miniatura de la foto se puede ampliar. Vive siempre dentro de un contexto
  que ya tiene su propia colección de fotos (la grilla o el visor que lo contiene) — se define en
  `design.md` si abre el visor de ese contexto padre o uno propio de 1 sola foto.
- `tbi-file-upload`: la vista previa del archivo recién elegido (antes de subirlo) se puede ampliar. Es
  una imagen aislada, todavía no es una foto del dominio (no tiene `fotoId`).
- **Fuera de alcance, explícito**: el logo del tenant en el layout/login. Es branding, no contenido de
  un evento — no aplica la regla.

## Capabilities

### Modified Capabilities

- `visor-fotos`: extiende el contrato para cubrir también la apertura de una **foto aislada** (colección
  de un solo elemento, sin flechas ni contador) y fija que el comparador de tamaños de
  `/fotos/publicacion` pasa a ser un caso más del visor único, no una implementación aparte.

## Impact

- **Frontend, `fotos/tarjetas`**: agrega apertura ampliada del QR (imagen generada en el momento, no
  bajada de un endpoint de fotos).
- **Frontend, `fotos/publicacion`**: el comparador deja de usar `tbi-carousel` directo y pasa a
  `tbi-visor-fotos`; evaluar si `tbi-carousel` queda sin otros usos y qué hacer con eso (ver `design.md`,
  no se decide en la propuesta).
- **Frontend, `fotos/marca-agua`**: listado (marca renderizada por fila) y editor (las muestras
  clara/oscura/mixta + "probar con mi foto") ganan apertura ampliada.
- **Frontend, `shared/ui`**: `tbi-agregar-carrito` y `tbi-file-upload` ganan apertura ampliada de su
  miniatura/preview.
- **Sin cambios de backend**: todo lo nuevo es superficie administrativa o assets generados en el
  cliente (QR, canvas, preview de archivo local); no toca `IFotoRepository`, endpoints ni el alcance por
  sesión de familia (ADR-06/ADR-11).
- **Tests**: specs nuevos por pantalla/componente tocado; la suite unit del front queda verde.
