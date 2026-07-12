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

- **Backend**: ASP.NET Core .NET 10, monolito modular (plataforma Base heredada del código base propio + vertical Fotos) + EF Core + **SQL Server** — ver ADR-09
- **Frontend**: Angular 22 (zoneless, signal forms), shell de administración heredado + zona de familias mobile-first (a construir)
- **Storage de fotos**: `IStorageProvider` — FileSystem en dev, S3-compatible privado (Cloudflare R2) con URLs firmadas en prod
- **Imágenes**: ImageSharp (thumbnails + marca de agua en el upload)
- **Pagos** (fase 3): Mercado Pago Checkout Pro

## Estructura del repo

```
/backend    → solución AcgFotos.slnx (Core + Base.* + Api + tests de integración)
/frontend   → app Angular 22 (ver frontend/README.md)
/docs       → documentación del proyecto
```

## Entorno de desarrollo

- .NET SDK 10, SQL Server local (la DB dev es `AcgFotos`; los tests usan `AcgFotos_Tests`)
- Node ≥ 22.22.3 (portátil incluido en `.tools/`, ver frontend/README.md)

**Setup inicial de la base dev** (una sola vez, desde `backend/`):

```powershell
dotnet ef database update --project AcgFotos.Base.SqlMigrations --startup-project AcgFotos.Api
sqlcmd -S localhost -d AcgFotos -I -i AcgFotos.Api.IntegrationTests\Infrastructure\TestSeed.sql
```

(El `-I` es obligatorio: el seed necesita `QUOTED_IDENTIFIER ON`.)

**Correr:**

- API dev: `dotnet run --project backend/AcgFotos.Api --launch-profile http` → http://localhost:30000 (Swagger en `/swagger`)
- Front dev: `npm start` en `frontend/` → http://localhost:4200
- Login dev: `root` / `Root@AcgFotos2026!` (más datos en [docs/credenciales-dev.md](docs/credenciales-dev.md))
