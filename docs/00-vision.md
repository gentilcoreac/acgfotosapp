# Visión del producto

## Problema

El fotógrafo (papá de Alberto) saca fotos en graduaciones escolares. Hoy el flujo de venta es manual:

1. Saca las fotos en el colegio.
2. Va **casa por casa** mostrando las fotos impresas o en carpeta para que cada familia elija.
3. Vuelve **casa por casa** a entregar las fotos impresas y cobrar.

Es lento, costoso en tiempo y traslados, y limita cuántas familias puede atender por evento.

## Solución

Una webapp donde:

- El **fotógrafo (admin)** crea el evento, sube las fotos organizadas por curso y alumno, y define tamaños de impresión y precios.
- Cada **familia** recibe un código/QR, entra sin registrarse, ve **solo su álbum**, elige fotos, tamaño y cantidad, ve el total y confirma el pedido.
- El fotógrafo recibe los pedidos consolidados: sabe exactamente qué imprimir, para quién y por cuánto. Solo queda una visita: la entrega (o ninguna, si se coordina entrega en el colegio).

## Usuarios

| Usuario | Acceso | Qué hace |
|---|---|---|
| **Admin** (fotógrafo) | Login con usuario/contraseña | Crea eventos, cursos y álbumes; sube fotos; define tamaños y precios; gestiona pedidos (pendiente → impreso → entregado) |
| **Familia** | Código de acceso o link con QR, sin registro | Ve su álbum (fotos individuales del alumno + grupales del curso), arma el pedido, deja nombre y teléfono de contacto |

## Premisas de producto

1. **Protección de imágenes por capas** (detalle en ADR-01): la base es que **lo capturable no tenga valor comercial** — toda foto visible lleva **marca de agua** (aplicada automáticamente por el sistema al subir; el original queda limpio para imprimir), los previews son de **baja resolución** (~1200px) y los **originales nunca llegan al navegador**. Sobre esa base: fricción en la web (sin clic derecho/arrastre/impresión) y, post-MVP, **app móvil (Capacitor) que bloquea la captura de pantalla en Android** (`FLAG_SECURE`, capturas en negro) y la detecta en iOS. En web pura la captura no es bloqueable (limitación del navegador).
2. **Cero fricción para las familias**: sin registro, sin contraseñas, sin app que instalar. Código → galería → pedido. Público objetivo usando celulares → diseño mobile-first.
3. **El admin es una sola persona no técnica**: la UI de administración debe ser simple; la carga de fotos debe ser masiva (arrastrar una carpeta) y el watermark/resize automático.
4. **Producto chico y mantenible**: monolito, dependencias mínimas, hosting barato. Optimizar por terminarlo y usarlo, no por escala.

## Referencias de mercado

Servicios de "photo proofing" para fotógrafos que validan este modelo (mirar para inspiración de UX): Pixieset, ShootProof, Pic-Time. AcgFotos es una versión mínima y en español de ese concepto, adaptada al flujo escolar argentino (códigos por alumno, pago con Mercado Pago, entrega física).
