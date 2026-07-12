# Arquitectura

## Vista general

```
[Familia móvil]──┐
                 ├──▶ Angular SPA ──▶ ASP.NET Core API ──▶ PostgreSQL
[Admin desktop]──┘                        │
                                          ├──▶ Bucket S3-compatible (privado)
                                          │      ├─ originals/   (nunca expuesto al público)
                                          │      └─ derived/     (previews con watermark, thumbnails)
                                          └──▶ Mercado Pago (fase 2)
```

Un solo backend monolítico, una sola SPA, una base de datos. Sin colas, sin microservicios, sin funciones serverless: el volumen (decenas de familias por evento, algunos eventos por mes) no lo justifica.

## Componentes

### Backend — ASP.NET Core (.NET 10)

- **API REST** con dos áreas: `admin` (autenticada con JWT, un solo usuario admin al inicio) y `public` (autenticada por código de acceso).
- **EF Core + PostgreSQL** para datos.
- **ImageSharp** para procesamiento en el upload: por cada original se generan
  - `thumb` (~300px, con watermark) para la grilla,
  - `preview` (~1200px lado mayor, con watermark de patrón repetido sobre toda la imagen) para la vista ampliada.
  El procesamiento corre en el request de upload (o en un `BackgroundService` con canal en memoria si la carga masiva lo pide); no hace falta infraestructura de colas.
- Estructura de solución sugerida:
  ```
  backend/
    AcgFotos.Api/            → controllers/endpoints, auth, DI
    AcgFotos.Domain/         → entidades, lógica de dominio
    AcgFotos.Infrastructure/ → EF Core, storage S3, ImageSharp, Mercado Pago
    AcgFotos.Tests/          → tests
  ```

### Frontend — Angular

- SPA única con dos zonas por ruta: `/admin/**` y la zona pública (`/a/{codigo}` para entrar a un álbum).
- **Mobile-first**: las familias entran desde el celular vía QR/WhatsApp.
- La galería consume URLs firmadas de los derivados; nunca conoce rutas de originales.

### Storage de fotos

- Bucket **privado** S3-compatible. Elección: **Cloudflare R2** (egreso gratis — las fotos son casi todo el tráfico). Alternativa equivalente: Backblaze B2. El código usa la API S3 (`AWSSDK.S3`), así que el proveedor es intercambiable.
- Layout de claves:
  ```
  originals/{eventoId}/{fotoId}.jpg
  derived/{eventoId}/{fotoId}_thumb.jpg
  derived/{eventoId}/{fotoId}_preview.jpg
  ```
- Acceso de lectura SOLO por **URLs firmadas con expiración corta** (~15 min), generadas por la API tras validar el código de acceso. Nada tiene ACL pública.
- En **desarrollo local**: MinIO en Docker (misma API S3) o un `IPhotoStorage` con implementación en disco.

## Seguridad de las imágenes (premisa central)

| Amenaza | Mitigación |
|---|---|
| Captura de pantalla / foto a la pantalla | No evitable en web. Se neutraliza el valor: watermark sobre toda la imagen + baja resolución |
| Descarga directa de la imagen mostrada | Es el mismo preview con watermark; sin valor. Igual se puede añadir fricción (sin clic derecho) pero es cosmético |
| Adivinar URLs del bucket | Bucket privado + URLs firmadas con expiración + IDs no secuenciales (GUID/ULID) |
| Compartir el link firmado | Expira en minutos; regenerarlo requiere el código de acceso |
| Acceso a álbum ajeno | El código de acceso mapea a UN álbum; la API filtra todo por ese álbum. Códigos con suficiente entropía y rate-limiting en el endpoint de acceso |
| Robo del original | Los originales jamás se sirven por API pública; solo el admin autenticado puede descargarlos (para imprimir) |

## Autenticación

- **Admin**: usuario/contraseña → JWT. Un solo admin al inicio; el modelo no impide agregar más después.
- **Familia**: ingresa código (o llega por link con el código embebido) → la API valida y emite un **token de sesión acotado al álbum** (JWT de corta duración con claim `albumId`). Todos los endpoints públicos exigen ese token. Sin contraseñas, sin registro.

## Pagos (fase 2)

**Mercado Pago Checkout Pro**: la API crea una preferencia de pago con el total del pedido, redirige, y un webhook confirma el pago (`Pedido.Estado = Pagado`). Modo alternativo siempre disponible: pedido "a pagar en la entrega" (efectivo), que es como opera hoy.

## Hosting (a decidir en fase de deploy, ver notas abiertas)

Criterio: barato y simple. Candidatos: Railway / Fly.io (contenedores administrados) o VPS Hetzner con Docker Compose (API + Postgres; las fotos siempre en R2, nunca en disco del servidor). HTTPS obligatorio.
