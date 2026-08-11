## 1. Visor base

- [x] 1.1 Crear el visor en `shared/ui` sobre `tbi-carousel`: apertura como diálogo casi a pantalla
      completa, colección + índice inicial de entrada, contenido del ítem proyectado por el caller,
      indicador de posición, cierre.
- [x] 1.2 Navegación por teclado (anterior/siguiente/cerrar) y por flechas, con los extremos de la
      colección sin envolver.
- [x] 1.3 Specs del visor base: apertura en el índice pedido, recorrido, extremos, posición informada,
      colección vacía, teclado.

## 2. Migración del contexto administrativo

- [x] 2.1 Galería (`/fotos/galeria`): abrir el visor base con la colección de fotos del grupo/filtro
      vigente en vez de `FotoPreviewDialogComponent`, conservando el toggle vista-cliente/original y la
      descarga del original como acciones del template del caller.
- [x] 2.2 Detalle de pedido: abrir el visor base sobre las fotos del pedido, conservando
      `varianteInicial: 'original'` (el admin de pedidos ve la original primero — decisión ya tomada,
      ADR-12/tercera vuelta).
- [x] 2.3 ~~Eliminar `FotoPreviewDialogComponent`~~ **Se conserva como envoltorio fino sobre el visor
      base**: al implementarlo quedó claro que borrarlo obligaría a que cada pantalla repitiera el
      toggle original/vista-cliente y la descarga. Deja de tener chrome y navegación propios (eso pasó
      al visor) y queda sólo con lo que distingue al contexto admin — que es justo lo que el visor
      delega a propósito. Sus specs se adaptaron al contrato nuevo (colección + índice).
- [x] 2.4 Verificar que la galería y el detalle de pedidos ahora navegan entre fotos, y que el toggle y
      la descarga siguen funcionando en ambos.

## 3. Migración del contexto de familias

- [x] 3.1 Mi-álbum (`/mi-album`): abrir el visor base respetando el filtro "ver solo el carrito"
      (el recorrido usa la colección filtrada, comportamiento ya existente que no debe perderse),
      conservando el layout de 4 esquinas y `tbi-agregar-carrito` inline. **Los llamadores no se
      tocaron**: ya pasaban `{fotos, index, tamanosPrecios}`, así que el contrato del diálogo quedó
      igual y el filtro sigue decidiéndose donde siempre.
- [x] 3.2 Carrito (`/carrito`): abrir el visor base sobre las fotos del carrito desde la cabecera de
      cada tarjeta, como hoy. Sin cambios por el mismo motivo que 3.1.
- [x] 3.3 ~~Eliminar `FotoFamiliaPreviewDialogComponent`~~ **Se conserva como envoltorio fino**, mismo
      criterio que el admin (2.3): es donde viven el sello legal, el tilde de carrito y el selector de
      tamaño+cantidad, que son de este contexto y no del visor.
- [x] 3.4 Verificar explícitamente que sobreviven la fricción anti-copia y el sello con el nombre de la
      familia (`contextmenu` bloqueado, `draggable=false`, `@media print`, sello tileado) y que no
      aparece ninguna vía al original desde el contexto de familia. **Se conservan por construcción**:
      todo eso vive dentro de `tbi-foto-familia-img`, que no se tocó — sólo cambió dónde se ubica ese
      componente. El diálogo sigue pidiendo `variante="preview"` y en ningún punto ofrece el original.
      Las 9 verificaciones de su spec original siguen pasando (se cambió cómo se dispara la
      navegación —clic en la flecha real en vez de llamar a un método— no qué se verifica).

## 4. Cierre

- [x] 4.1 Suite unit del front + lint verdes.
- [ ] 4.2 Verificación manual end-to-end contra la API/front dev: las cuatro pantallas, abriendo y
      recorriendo, en desktop y en mobile.
- [x] 4.3 Documentación: tildar el ítem "lightbox con flechas/teclado" del backlog de Fase 4
      (docs/03-fases.md) y dejar constancia del visor unificado en docs/05-notas-abiertas.md.
