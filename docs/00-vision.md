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

1. **Protección de imágenes por capas** (detalle en ADR-01): la base es que **lo capturable no tenga valor comercial** — toda foto visible lleva **marca de agua** (aplicada automáticamente por el sistema al subir; el original queda limpio para imprimir), los previews son de **muy baja resolución** (900px y calidad WebP baja — "rozando lo pobre", pedido 2026-07-15; watermark al 50% de opacidad; todo configurable. Referencia real: un original de 40 MP/10,5 MB queda en 0,6 MP/40 KB) y los **originales nunca llegan al navegador**. Sobre esa base: fricción en la web (sin clic derecho/arrastre/impresión) y, post-MVP, **app móvil (Capacitor) que bloquea la captura de pantalla en Android** (`FLAG_SECURE`, capturas en negro) y la detecta en iOS. En web pura la captura no es bloqueable (limitación del navegador).
2. **Cero fricción para las familias**: sin registro, sin contraseñas, sin app que instalar. Código → galería → pedido. Público objetivo usando celulares → diseño mobile-first.
3. **El admin es una sola persona no técnica**: la UI de administración debe ser simple; la carga de fotos debe ser masiva (arrastrar una carpeta) y el watermark/resize automático.
4. **Producto chico y mantenible**: monolito, dependencias mínimas, hosting barato. Optimizar por terminarlo y usarlo, no por escala.

## Datos del negocio (respuestas del fotógrafo, 2026-07-15)

- **Volumen típico**: una colación clásica ≈ 3 cursos × 30 alumnos, con 4–5 fotos mínimo por persona más las grupales (banderas, familia, informales). Estimación de trabajo: **500–800 fotos por evento (~3–8 GB de originales)**.
- **Impresión**: laboratorio externo. Pide los archivos en una **medida específica** pero sin nomenclatura especial → la lista de impresión puede ser CSV/PDF + los originales agrupados; el export debería validar/avisar proporciones según el tamaño pedido.
- **Retoque**: las fotos pasan por Photoshop ANTES de subirse — al sistema entran ya editadas. No hace falta versionado: reemplazar = borrar y volver a subir.
- **Entrega**: hoy varía entre casa por casa y en el colegio (a veces entrega el propio colegio). El pedido no necesita dirección de envío: nombre + teléfono alcanzan, y el punto de entrega se coordina por evento.
- **Cobro**: a veces cobra seña, a veces le pagan el total por adelantado → el flujo de pagos (Fase 3) debe soportar **pago total o parcial**.
- **Promos/paquetes**: rara vez, pero útiles → adelantados a Fase 3.
- **Tamaños de impresión**: varios y cambiantes → ya cubierto: el catálogo `TamanoPrecio` es **libre y por evento** (el ABM de Eventos permite agregar cualquier tamaño con su precio; no hay lista fija que limite).

## Referencias de mercado

Servicios de "photo proofing" para fotógrafos que validan este modelo (mirar para inspiración de UX): Pixieset, ShootProof, Pic-Time. AcgFotos es una versión mínima y en español de ese concepto, adaptada al flujo escolar argentino (códigos por alumno, pago con Mercado Pago, entrega física).
