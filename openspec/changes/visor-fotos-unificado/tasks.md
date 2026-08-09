## 1. Visor base

- [ ] 1.1 Crear el visor en `shared/ui` sobre `tbi-carousel`: apertura como diálogo casi a pantalla
      completa, colección + índice inicial de entrada, contenido del ítem proyectado por el caller,
      indicador de posición, cierre.
- [ ] 1.2 Navegación por teclado (anterior/siguiente/cerrar) y por flechas, con los extremos de la
      colección sin envolver.
- [ ] 1.3 Specs del visor base: apertura en el índice pedido, recorrido, extremos, posición informada,
      colección vacía, teclado.

## 2. Migración del contexto administrativo

- [ ] 2.1 Galería (`/fotos/galeria`): abrir el visor base con la colección de fotos del grupo/filtro
      vigente en vez de `FotoPreviewDialogComponent`, conservando el toggle vista-cliente/original y la
      descarga del original como acciones del template del caller.
- [ ] 2.2 Detalle de pedido: abrir el visor base sobre las fotos del pedido, conservando
      `varianteInicial: 'original'` (el admin de pedidos ve la original primero — decisión ya tomada,
      ADR-12/tercera vuelta).
- [ ] 2.3 Eliminar `FotoPreviewDialogComponent` y adaptar/reemplazar sus specs.
- [ ] 2.4 Verificar que la galería y el detalle de pedidos ahora navegan entre fotos, y que el toggle y
      la descarga siguen funcionando en ambos.

## 3. Migración del contexto de familias

- [ ] 3.1 Mi-álbum (`/mi-album`): abrir el visor base respetando el filtro "ver solo el carrito"
      (el recorrido usa la colección filtrada, comportamiento ya existente que no debe perderse),
      conservando el layout de 4 esquinas y `tbi-agregar-carrito` inline.
- [ ] 3.2 Carrito (`/carrito`): abrir el visor base sobre las fotos del carrito desde la cabecera de
      cada tarjeta, como hoy.
- [ ] 3.3 Eliminar `FotoFamiliaPreviewDialogComponent` y adaptar/reemplazar sus specs.
- [ ] 3.4 Verificar explícitamente que sobreviven la fricción anti-copia y el sello con el nombre de la
      familia (`contextmenu` bloqueado, `draggable=false`, `@media print`, sello tileado) y que no
      aparece ninguna vía al original desde el contexto de familia.

## 4. Cierre

- [ ] 4.1 Suite unit del front + lint verdes.
- [ ] 4.2 Verificación manual end-to-end contra la API/front dev: las cuatro pantallas, abriendo y
      recorriendo, en desktop y en mobile.
- [ ] 4.3 Documentación: tildar el ítem "lightbox con flechas/teclado" del backlog de Fase 4
      (docs/03-fases.md) y dejar constancia del visor unificado en docs/05-notas-abiertas.md.
