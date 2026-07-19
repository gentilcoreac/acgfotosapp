# Handoff: App de venta de fotografías (Photo Store)

## Overview
Prototipo de la experiencia completa de compra de fotos para fotógrafos que venden a clientes finales (colegios, casamientos, graduaciones, etc.): galería/carrusel con marca de agua antirrobo, selección de fotos (individual y masiva), visor de producto con elección de tamaño/formato, carrito editable, checkout y confirmación. Pensado para Angular + Angular Material, theming Material Design 3 (M3) explícito.

## About the Design Files
Los archivos de este paquete son **referencias de diseño hechas en HTML** (Design Component runtime propio), no código para copiar tal cual. La tarea es **recrear este diseño en Angular** usando Angular Material 3 (M3 theming), siguiendo los patrones ya establecidos en el codebase real (servicios, módulos, tokens de tema, etc.).

## Fidelidad
**Alta fidelidad (hifi)**: colores, tipografía (Roboto, roles M3: primary / primary-container / on-primary-container, superficies tonales), espaciado, y micro-interacciones están definidos y deben respetarse. Recrear pixel-perfect con los componentes de Angular Material (mat-chip, mat-fab, mat-stepper de cantidad, mat-dialog o bottom-sheet para el visor, mat-button-toggle para segmented controls).

## Pantallas / Vistas

### 1. Galería (`view: 'gallery'`)
- **Propósito**: explorar y pre-seleccionar fotos de un evento antes de comprar.
- **Layout**: header sticky con logo, chips de álbum (scroll horizontal), grid responsive (`repeat(auto-fill, minmax(180px,1fr))` desktop, 2 columnas mobile), gap 14px/8px.
- **Componentes**:
  - Chips de álbum (Todas / Ceremonia / Fiesta / Egresados) — estilo M3 filter chip, esquina 8dp.
  - Toggle "Mis fotos (reconocimiento facial)" — filtra por rostro detectado (placeholder de una integración real de face-match).
  - Toggle "Seleccionar varias" — activa modo selección múltiple: cada thumbnail muestra un check circular; aparece barra con contador y botón "Agregar seleccionadas".
  - Thumbnail: aspect-ratio 1:1, marca de agua superpuesta (ver sección Marca de Agua), badge ✓ si ya está en el carrito, nombre de archivo en overlay monospace.
  - FAB extendido flotante inferior "Ver carrito" con contador y total, visible apenas hay 1+ ítems.

### 2. Visor / Selector de producto (modal, `viewerOpen`)
- **Propósito**: elegir tamaño/formato y cantidad de UNA foto.
- **Layout**: desktop = split horizontal (imagen 1/ panel 340px derecha), mobile = imagen arriba / panel bottom-sheet abajo.
- **Componentes**:
  - Navegación prev/next entre fotos del filtro actual, contador "3 / 21".
  - Segmented button Impresión / Digital.
  - Si ya hay líneas de esta misma foto en el carrito, se muestra aviso "Ya en tu carrito: 15x20 ×1 · Digital ×2" — permite agregar OTRO tamaño de la misma foto sin perder el anterior (cada tamaño es una línea de carrito distinta; si coincide tamaño+tipo, suma cantidad en vez de duplicar línea).
  - Lista de tamaños con precio (10x15, 15x20, 20x30) o mensaje de descarga digital sin marca de agua.
  - Aviso de upsell: "Llevate el 20x30 por solo $1.000 más".
  - Stepper de cantidad, botón "Agregar al carrito" (cambia a "Agregar este tamaño también" / "Sumar otra unidad" según contexto).

### 3. Carrito (`view: 'cart'`)
- **Propósito**: revisar y editar la selección antes de pagar.
- **Componentes**:
  - Banner de progreso de descuento por cantidad ("Llevá 3 fotos más y obtené 20%"), barra de progreso.
  - Banner de upsell de pack digital ("Sumá 4 fotos digitales más y desbloqueá el Pack x10").
  - Línea de carrito por foto+tamaño+tipo: thumbnail, chips de tamaño (editable inline), stepper de cantidad, subtotal, botón quitar.
  - Resumen: subtotal, descuento aplicado, total. Botón "Continuar compra".

### 4. Checkout (`view: 'checkout'`)
- **Propósito**: capturar datos de contacto/entrega/pago y confirmar.
- **Componentes**:
  - Datos de contacto (nombre, teléfono).
  - Entrega: segmented button Retiro en local / Envío a domicilio (muestra input de dirección si aplica).
  - Método de pago: Mercado Pago (nota de redirección) / Transferencia bancaria (muestra CBU/alias/titular y aviso de reserva 24hs).
  - Resumen del pedido: lista itemizada foto por foto (thumbnail, tipo+tamaño, cantidad, subtotal), luego subtotal/descuento/total.
  - Botón "Confirmar pedido".

### 5. Confirmación (`view: 'confirmation'`)
- Número de pedido, mensaje de próximos pasos, aviso de que las fotos digitales sin marca de agua estarán disponibles en la cuenta del cliente. Botón volver a galería.

## Marca de agua — diseño y notas de producto (pedido explícito del usuario)

El prototipo incluye 3 intensidades de ejemplo (`subtle`, `balanced`, `dense`) controladas por un prop/tweak, pero el usuario quiere que esto sea **configurable dentro de la app real**, no solo un valor fijo de diseño. Notas para el backlog:

1. **ABM de marca de agua por tenant (fotógrafo/estudio)**: cada tenant debería poder definir su config default de marca de agua:
   - Texto (nombre del estudio, nombre del cliente, ambos).
   - Intensidad/repetición (sutil, balanceada, densa) y opacidad.
   - Color / contraste según fondo.
   - Si se aplica también un tag de esquina (ej. "Copia no autorizada") además del patrón diagonal.
2. **Override por evento**: dentro de un tenant, cada evento (ej. una boda puntual) debería poder pisar la config default — algunos clientes quieren marca de agua más agresiva (fotos de alto valor / alto riesgo de robo) y otros más sutil (evento chico, bajo riesgo).
3. **Modo fotógrafo / presencial (sin seguridad)**: cuando el usuario logueado es el admin del tenant (el fotógrafo mismo, ej. mostrando el álbum en persona a un cliente en su estudio o en el evento), la app debería poder desactivar TODA la capa de "seguridad" (marca de agua, deshabilitar clic derecho/guardar, etc.) y funcionar como un visualizador simple de fotos originales en alta calidad. Esto ya está prototipado como toggle "Modo fotógrafo" en el header (banner naranja "Modo fotógrafo activo"): al activarlo, `watermarkLines` se vacía y no se aplica ningún dato identificador sobre la imagen. En la implementación real esto debe ir atado a permisos/rol (admin de tenant), no a un toggle público — es decir, un botón visible al cliente final NUNCA debe poder desactivar la marca de agua; el control real debe venir del backend según el rol del usuario autenticado.

## Interacciones y comportamiento
- Selección múltiple: tap en modo selección tilda/destilda; "Agregar seleccionadas" crea una línea de carrito por foto (tamaño default 15x20, se puede ajustar después desde el carrito).
- Carrito soporta múltiples tamaños/formatos de la MISMA foto como líneas independientes; si se agrega el mismo tamaño+tipo dos veces, se suma la cantidad en la línea existente en vez de duplicar.
- Descuento por cantidad: 5+ fotos = 10%, 10+ fotos = 20% (tiers configurables).
- Pack digital: umbral de 10 fotos digitales a precio fijo, con mensaje de cuánto falta y cuánto se ahorra.
- Toggle Mobile/Desktop en el header del prototipo es solo para previsualizar ambos breakpoints — no es parte del producto final.

## Design Tokens (M3, valores usados en el prototipo)
- Tipografía: Roboto (400/500/700), sin uppercase salvo overlays técnicos.
- Color primary: `oklch(42% .13 175)` (verde azulado), primary-container: `oklch(90% .06 175)`, on-primary-container: `oklch(18% .09 175)`.
- Superficie: `oklch(98% .004 175)` / `oklch(97% .005 175)` (surface-container-low).
- Acento de descuento/urgencia: tono cálido `oklch(*% .1 55)`.
- Radios: chips/botones 8px, cards 12–16px, FAB/botones primarios 999px (pill) o 16px según Material 3 extended FAB.
- Elevaciones: sombras suaves de 2 niveles (`0 1px 2px … , 0 1px 3px 1px …`), sin glow ni gradientes decorativos.

## Assets
No hay imágenes reales: las fotos son placeholders de color sólido + textura diagonal sutil, ya que el prototipo es de flujo/UX, no de contenido. Reemplazar por imágenes reales del servicio de fotos del fotógrafo.

## Archivos
- `Photo Store.dc.html` — prototipo completo (galería, visor, carrito, checkout, confirmación, toggle modo fotógrafo, tweaks de marca de agua y upsell).
