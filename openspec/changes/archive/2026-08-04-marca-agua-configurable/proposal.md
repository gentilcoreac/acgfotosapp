## Why

Hoy la marca de agua vive en `appsettings` (`Fotos:TextoWatermark`, `Fotos:OpacidadWatermark`) y su
dibujo está hardcodeado en `ImageSharpImageProcessor`: cambiar el texto, el color o la intensidad
exige editar configuración y reiniciar la API, y no hay forma de subir un logo. El fotógrafo necesita
customizar su marca y verla antes de aplicarla.

Además la marca actual tiene dos agujeros reales: el blanco al 50% **desaparece sobre un vestido
claro** (o sea que la foto más valiosa queda peor protegida), y `ResolverFuente` pide Arial al
sistema operativo — en un contenedor Linux sin fuentes eso revienta o cae en una tipografía
arbitraria.

Diseño ya cerrado y acordado en **ADR-15** (`docs/04-decisiones.md`), con prototipo navegable en
`docs/ClaudeDesign/PropuestaMarcaAgua/`. Esta propuesta lo implementa; no reabre esas decisiones.

## What Changes

- **El front diseña y rasteriza, la API sólo compone.** La marca se define en el navegador y viaja a
  la API como un **PNG con transparencia** más parámetros de colocación. La API nunca vuelve a
  dibujar texto: coloca, escala, rota y funde un bitmap sobre la foto. **BREAKING** para
  `IImageProcessor`: `OpcionesDerivados.TextoWatermark`/`Opacidad` dejan de existir como contrato de
  dibujo y se reemplazan por una lista de capas ya rasterizadas.
- **Perfil de marca de agua = de 1 a 3 capas.** Cada capa es una imagen (diseñada o subida) con su
  colocación: repetida en mosaico o en una de 9 posiciones fijas, con escala en % del ancho, margen,
  ángulo, opacidad y modo de fusión (`Normal · Superponer · Diferencia`). "Subir mi logo" y "diseñar
  un texto" pasan a ser el mismo mecanismo y se pueden combinar.
- **Opciones de publicación como entidad aparte** (lado mayor de preview/thumb y calidad), con su
  propio circuito default-del-tenant → override por evento. `MarcarThumb` sí queda en el perfil de
  marca.
- **Resolución de configuración en cascada**: evento → default del tenant → `OpcionesFotos` de
  `appsettings`. Sin perfiles cargados el pipeline se comporta **exactamente como hoy**.
- **Aplicar a fotos ya subidas es explícito**, por evento y con el conteo por delante. Guardar un
  perfil nunca toca una foto.
- **Dos pantallas nuevas de admin**: `/fotos/marca-agua` (listado + editor de capas con verificación
  sobre tres muestras simultáneas) y `/fotos/publicacion` (comparador de resoluciones). La vista
  previa muestra la imagen **ya pasada por el encoder WebP**.
- **Asignación en el ABM de Eventos**: campos "Marca de agua" y "Opciones de publicación" (default
  "Usar la del estudio") y acción de regenerar desde la galería del evento.
- **Las guardas se explican solas**: ninguna validación avisa en genérico ni actúa en silencio; cada
  una dice el número concreto y la consecuencia real ("tu logo tiene 300 px de ancho; a esta escala
  se va a ver borroso", "se van a regenerar 412 fotos").
- **Se cae una fragilidad de producción**: con la tipografía horneada en el PNG, el servidor deja de
  necesitar fuentes instaladas (`ResolverFuente` desaparece).

## Capabilities

### New Capabilities

- `marca-agua`: perfiles de marca de agua del estudio — modelo de perfil + capas, CRUD, subida y
  validación de los PNG de capa, y la cascada de resolución evento → tenant → appsettings.
- `opciones-publicacion`: resolución (lado mayor de preview/thumb) y calidad de los derivados como
  configuración administrable, con la misma cascada.
- `regeneracion-derivados`: rehacer los derivados de las fotos ya subidas de un evento bajo demanda,
  con el conteo por delante, reutilizando el worker de procesamiento existente.

### Modified Capabilities

<!-- Ninguna: `openspec/specs/` está vacío (OpenSpec se inicializó en este repo el 2026-08-01 y
     todavía no hay specs archivadas). El pipeline de derivados existente no tiene spec previa, así
     que su cambio de comportamiento se describe dentro de la capability nueva `marca-agua`. -->

## Impact

**Backend**

- `AcgFotos.Fotos.Domain/Entities`: entidades nuevas `PerfilMarcaAgua`, `CapaMarcaAgua`,
  `OpcionesPublicacion`; FKs nullable `PerfilMarcaAguaId` / `OpcionesPublicacionId` en `Evento`.
- `AcgFotos.Fotos.Infrastructure/Persistence/Ef`: configuraciones EF nuevas (`fot_`), migración EF y
  seed de un perfil "Estándar" equivalente a la marca actual.
- `AcgFotos.Fotos.Infrastructure/Imaging/ImageSharpImageProcessor`: deja de dibujar texto y pasa a
  componer capas; se elimina `ResolverFuente` y la dependencia de `SixLabors.Fonts`.
- `AcgFotos.Fotos.Application`: `IImageProcessor`/`OpcionesDerivados` (contrato), AppServices nuevos
  con `FamiliaSessionGuard`, `IFotoStorage` gana las keys de los assets de capa,
  `FotoProcesadorAppService` pasa a resolver la config en cascada en vez de leer `OpcionesFotos`
  directo.
- `AcgFotos.Fotos.Controllers/Api`: controllers nuevos + endpoint de regeneración por evento.
- Seeds: `TestSeed.sql` y `scripts/dev-alta-fotografo.sql` (menús nuevos, igual que `FotosPedidos`).
- Tests de integración (hoy 508) — incluye el test que compara una fusión de canvas contra la de
  ImageSharp sobre la misma muestra.

**Frontend**

- `features/fotos/marca-agua/` y `features/fotos/publicacion/` (nuevas), rutas en `app.routes.ts`.
- `features/fotos/eventos/`: dos campos nuevos en el ABM.
- `features/fotos/fotos/`: acción de regenerar en la galería del evento.
- Unit tests (hoy 325).

**Docs**

- `docs/03-fases.md` (tildar los 8 ítems), `docs/05-notas-abiertas.md` (cerrar el pendiente del ABM).

**Sin impacto**: la zona de familias (`/mi-album`, `/carrito`) no cambia — sigue consumiendo
derivados con marca por URL firmada. Los originales siguen sin exponerse (ADR-01/ADR-06).
