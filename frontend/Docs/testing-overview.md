# Panorama de testing — AcgFotos (índice maestro)

> Estado: 2026-06-23. Índice **cross-repo** de todas las suites de test del producto: qué hay, qué
> cubre cada una, dónde vive, cómo se corre y qué se decide. Nace de la necesidad de **organizar y
> documentar** todos los tipos de test (API, Postman, Angular, E2E, …) en un solo lugar.

## Pirámide — quién prueba qué (sin solaparse)

```
        ╱ E2E (Playwright) ╲          jornadas críticas cross-feature por la UI real
      ╱  componente/servicio ╲        lógica de UI + HTTP mockeado (Angular)
    ╱   integración de API     ╲      contrato + multitenant + seguridad contra DB real (xUnit)
  ╱      unit puro              ╲     crypto / cookie / jwt / mappers / validators
```

Regla: **un caso se prueba en la capa más barata que lo cubra.** El E2E NO re-prueba validaciones de
campo (eso es componente/API); la API NO re-prueba renders (eso es componente/E2E).

## Suites

| #   | Suite                       | Tech                                      | Repo / ubicación                                                               | Cubre                                                                                                                | Estado                                                                                                                                    | Cómo se corre                                             |
| --- | --------------------------- | ----------------------------------------- | ------------------------------------------------------------------------------ | -------------------------------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------- | --------------------------------------------------------- |
| 1   | **Integración de API**      | xUnit + `WebApplicationFactory` + Respawn | **API** · `AcgFotos.Api.IntegrationTests` (rama `feature/api-integration-tests`) | contrato HTTP, multitenant/aislamiento, authz, impersonación, mass-assignment, audit — contra DB real `AcgFotos_Tests` | ✅ activa (~376 tests). Catálogo: `Api/Docs/casos-de-prueba/`                                                                             | `dotnet test` el proyecto                                 |
| 2   | **Unit/componente Angular** | Jasmine + Karma                           | **Cliente** · `src/**/*.spec.ts`                                               | servicios (HTTP mockeado), stores, guards, algunos componentes                                                       | ✅ activa (~47 specs). ⚠️ Karma **deprecado** → migrar a **Vitest** (builder oficial Angular 20) — ver memoria `testing-vitest-pendiente` | `npm test` (Karma, abre Chrome)                           |
| 3   | **E2E**                     | Playwright (nativo)                       | **Cliente** · `e2e/` · docs `Docs/e2e/`                                        | jornadas críticas por la UI real (login/ruteo/sesión/impersonación/CRUD/errores/theming) contra API+front levantados | 🟢 Fases 0-4 completas (37 casos verde) — ver `Docs/e2e/`                                                                                 | `npm run e2e` (requiere Node 22.14.0 + API + `npm start`) |
| 4   | **Postman / Newman**        | Postman collection + Newman               | **API** · `Api/Docs/postman/`                                                  | smoke manual/CI del API por HTTP                                                                                     | 🗑️ **A BORRAR** (decidido — la reemplaza la suite xUnit)                                                                                  | Postman app / `newman run`                                |

**Quality gates (no son tests, pero validan):** ESLint + `angular-eslint` (`npm run lint`), Stylelint
(`npm run lint:styles`), Prettier (`npm run format:check`), commitlint (conventional commits, hook),
husky + lint-staged (pre-commit). El build (`npm run build`) también caza errores de tipos/templates.

## Decisiones / tareas abiertas

1. **Postman/Newman — ✅ DECIDIDO: BORRAR.** La suite de **integración de API (xUnit)** cubre el
   contrato del API mejor y de forma versionada/automatizada → la colección Postman ya no es necesaria.
   **Tarea:** borrar `Api/Docs/postman/` (`AcgFotos.postman_collection.json` +
   `AcgFotos.dev.postman_environment.json`) en el repo API (rama + PR; ese repo está en
   `feature/api-integration-tests`). La suite xUnit es la **fuente de verdad del contrato**.
2. **Karma → Vitest** (suite 2): tech-debt; migrar cuando duela Karma. Ver `testing-vitest-pendiente`.
3. **Base de tests dedicada para E2E** (suite 3): habilita los casos por rol (no-root) y los mutantes
   (CRUD/THEME). Análisis y plan en [`e2e/base-de-tests.md`](./e2e/base-de-tests.md).
4. **CI**: hoy no hay qa/prd ni pipeline. Cuando lo haya: correr suites 1 y 3 headless con DB efímera
   (Testcontainers / DB dedicada), 2 con Vitest.

## Enlaces

- Catálogo de casos de API + convenciones: `Api/Docs/casos-de-prueba/` (`casos-de-prueba.md`,
  `testing-convenciones-api.md`).
- Plan y catálogo E2E: [`e2e/README.md`](./e2e/README.md), [`e2e/casos-e2e.md`](./e2e/casos-e2e.md).
- Base de tests E2E (análisis): [`e2e/base-de-tests.md`](./e2e/base-de-tests.md).
- Memoria del proyecto: `plan-testing`, `testing-vitest-pendiente`.
