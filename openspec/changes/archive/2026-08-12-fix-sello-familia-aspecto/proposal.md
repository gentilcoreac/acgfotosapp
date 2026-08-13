## Why

En la vista de cliente, el sello "Familia de {participante(s)}" (`tbi-foto-familia-img`, segunda marca
de agua en el front) se ve repetido sobre TODO el diálogo del visor, no solo sobre la foto — reportado
por Alberto con capturas (2026-08-12).

Causa: el sello se pinta como `background-image` tileado sobre `inset:0` del host del componente
(`.marca-familia`), y ese host ocupa el ancho completo del contenedor mientras la foto, mostrada con
`object-fit: contain`, queda centrada adentro con bandas vacías a los costados cuando la proporción de
la foto no coincide con la del contenedor (caso típico del visor casi a pantalla completa). El sello
llena también esas bandas vacías en vez de limitarse a donde está la foto.

## What Changes

- El host de `tbi-foto-familia-img` (o el contenedor que aplica el sello) pasa a tener exactamente la
  proporción de la foto en vez del ancho completo disponible, usando el ancho/alto que `FotoFamilia` ya
  trae. Con eso el sello queda acotado a la imagen real, sin invadir las bandas vacías del diálogo.
- Alcance: afecta a los dos usos de `tbi-foto-familia-img` — la grilla de `mi-album` (`fit="cover"`,
  donde hoy no se nota porque la foto llena el tile) y el visor ampliado (`fit="contain"`, donde sí se
  nota porque ahí aparecen las bandas).

## Capabilities

### Modified Capabilities

- `visor-fotos`: el requisito "Acciones propias de cada contexto" (escenario "Contexto de familia") ya
  dice que el visor de familias trae el sello con el nombre — se precisa que el sello SHALL limitarse
  al área real de la foto, no al área del diálogo.

## Impact

- **Frontend**: `frontend/src/app/features/familia/mi-album/ui/foto-familia-img.component.ts` (host y
  hoja de estilos de `.marca-familia`). Usa `FotoFamilia.ancho`/`alto`, ya expuestos por el modelo.
- **Sin cambios de backend**: es un ajuste de layout puramente CSS/template; no toca el watermark
  horneado del derivado (ADR-01 capa 1) ni ningún endpoint.
- **Tests**: spec de `tbi-foto-familia-img` que verifique que el sello queda acotado a la proporción de
  la foto, no al contenedor.
