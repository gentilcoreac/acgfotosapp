# AcgFotos

Plataforma de venta de fotos escolares. Reemplaza el proceso de ir casa por casa a mostrar y entregar fotos de graduaciones: el fotógrafo sube las fotos a álbumes por alumno/curso, las familias entran con un código, eligen fotos, tamaños y cantidades, y el fotógrafo recibe los pedidos listos para imprimir y entregar.

## Documentación

| Documento | Contenido |
|---|---|
| [docs/00-vision.md](docs/00-vision.md) | Problema, usuarios, premisas de producto |
| [docs/01-arquitectura.md](docs/01-arquitectura.md) | Stack, componentes, storage, seguridad de imágenes |
| [docs/02-modelo-datos.md](docs/02-modelo-datos.md) | Entidades y relaciones |
| [docs/03-fases.md](docs/03-fases.md) | Roadmap por fases con criterios de terminado |
| [docs/04-decisiones.md](docs/04-decisiones.md) | Decisiones de diseño y su justificación (ADRs) |
| [docs/05-notas-abiertas.md](docs/05-notas-abiertas.md) | Preguntas pendientes y notas para fases siguientes |

## Stack (resumen)

- **Backend**: ASP.NET Core (monolito) + EF Core + PostgreSQL
- **Frontend**: Angular (SPA, mobile-first)
- **Storage de fotos**: bucket S3-compatible privado (Cloudflare R2), URLs firmadas
- **Imágenes**: ImageSharp (thumbnails + marca de agua en el upload)
- **Pagos** (fase 2): Mercado Pago Checkout Pro

## Estructura prevista del repo

```
/backend    → solución ASP.NET Core (API + dominio + infraestructura)
/frontend   → app Angular
/docs       → documentación del proyecto
```

## Entorno de desarrollo verificado

- .NET SDK 10.0.300
- Node v22.14.0
- Angular CLI instalado
