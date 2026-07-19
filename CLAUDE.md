# AcgFotos — guía para el asistente

App de venta de fotos escolares (graduaciones). Un fotógrafo (admin) sube fotos; las familias entran con un código, eligen fotos/tamaños/cantidades y generan pedidos.

**Antes de trabajar, leer los docs**: [docs/03-fases.md](docs/03-fases.md) dice en qué fase estamos y qué sigue; [docs/04-decisiones.md](docs/04-decisiones.md) tiene las decisiones tomadas (no re-discutirlas sin motivo nuevo); [docs/05-notas-abiertas.md](docs/05-notas-abiertas.md) tiene los pendientes.

## Origen del código (ADR-09)

Fork del código base propio `C:\PROYECTOS\CodigoBase` (API + Cliente), renombrado `TechBI → AcgFotos`, sin el vertical Budget. La plataforma (auth, usuarios/roles/permisos, menús, multi-tenant, auditoría) ya viene resuelta; **AcgFotos se construye como el vertical Fotos** con el mismo patrón modular que tenía Budget. Los patrones obligatorios del backend (capas, multi-tenant, FluentValidation, repos) están resumidos en [docs/01-arquitectura.md](docs/01-arquitectura.md); el frontend tiene su propia guía en [frontend/CLAUDE.md](frontend/CLAUDE.md).

## Reglas del proyecto

- **Idioma**: documentación, UI, comentarios y mensajes de commit en **español** (commits en minúscula, estilo `feat:`/`fix:`). Código en el estilo del código base: clases/métodos en inglés, entidades de dominio en español (`Evento`, `Grupo`, `Participante`, `Pedido`... — naming genérico del ADR-10).
- **Comentarios — regla dura**: el código se escribe para que se entienda solo; el comentario es la excepción, no la norma. Comentar SOLO lo que el código no puede decir por sí mismo (una restricción externa no obvia, una divergencia deliberada del código base, un workaround puntual, un catch que silencia a propósito). NO comentar lo que el nombre ya dice, la justificación de por qué el cambio es correcto (eso va en el mensaje de commit), ni referencias a tareas/fases de trabajo internas o efímeras de una sesión puntual (`[T-ALGO]`, IDs de sesión) — quedan sin sentido para quien lea el archivo después. Detalle y el incidente real que motivó la regla: [backend/CONTRIBUTING.md](backend/CONTRIBUTING.md#comentarios) / [frontend/CONTRIBUTING.md](frontend/CONTRIBUTING.md#comentarios).
- **Premisa de seguridad de imágenes**: los originales NUNCA se sirven por endpoints públicos; todo lo visible por familias lleva watermark y baja resolución; storage privado. Ver ADR-01 y ADR-06.
- **Vertical Fotos**: proyectos `AcgFotos.Fotos.*`, tablas con prefijo `fot_`, módulo Autofac propio + alta en `AppModulesName`. `Base` nunca referencia al vertical (hay test de arquitectura).
- **Tests parte del alcance**: la suite de integración (419 tests) y la unit del front (325) quedan verdes en cada cambio. Migraciones EF: el SQL crudo (vistas) se mantiene a mano.
- **Al tomar una decisión de diseño relevante**, agregar ADR en docs/04-decisiones.md. Al completar ítems, tildar en docs/03-fases.md. Mantener docs/05-notas-abiertas.md al día.
- **Convenciones detalladas y checklists**: [backend/CONTRIBUTING.md](backend/CONTRIBUTING.md) (naming, patrones obligatorios, checklist de endpoint/entidad nueva) y [frontend/CONTRIBUTING.md](frontend/CONTRIBUTING.md) (naming, Signal Forms, `rxResource`, checklist de feature nueva) — traídos y adaptados del código base.

## Comandos

- **Backend** (`backend/`): `dotnet build AcgFotos.slnx` · `dotnet test AcgFotos.Api.IntegrationTests` (necesita SQL Server localhost; usa/crea `AcgFotos_Tests`) · host: `dotnet run --project AcgFotos.Api --launch-profile http` (→ :30000, env Development, DB `AcgFotos`).
- **Migraciones**: `dotnet ef migrations add <Nombre> --project AcgFotos.Base.SqlMigrations --startup-project AcgFotos.Api`.
- **Frontend** (`frontend/`): Node ≥22.22.3 — si el global es menor: `$env:Path = "c:\PROYECTOS\AcgFotosApp\.tools\node-v22.22.3-win-x64;$env:Path"`. Luego `npm start` (:4200) · `npm test` · `npm run lint`.
- **Creds dev**: `root` / `Root@AcgFotos2026!` (la clave dev del seed; hash regenerado en TestSeed.sql y seeds e2e).

## Estructura

```
backend/   → AcgFotos.Core + AcgFotos.Base.* (plataforma) + AcgFotos.Api (host) + tests
frontend/  → Angular 22 (zoneless, signal forms); ver frontend/CLAUDE.md
docs/      → fuente de verdad del plan (visión, arquitectura, modelo, fases, ADRs, notas)
.tools/    → Node portátil 22.22.3 (gitignored)
```
