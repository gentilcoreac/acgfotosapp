# AcgFotos — Frontend

SPA Angular 22 (zoneless, signal forms, standalone). Shell de plataforma heredado del código base;
el vertical Fotos se construye encima. Guía de trabajo: [CLAUDE.md](CLAUDE.md) · convenciones de
código y checklist: [CONTRIBUTING.md](CONTRIBUTING.md) · plan del producto: [../docs](../docs).

## Requisitos

- Node ≥ 22.22.3 (ver `.nvmrc`). Hay un Node portátil en `..\.tools\node-v22.22.3-win-x64` si el
  global es menor: `$env:Path = "c:\PROYECTOS\AcgFotosApp\.tools\node-v22.22.3-win-x64;$env:Path"`.

## Comandos

```bash
npm start        # dev server en http://localhost:4200 (API en :30000)
npm test         # unit tests (Vitest)
npm run lint     # ESLint
npm run e2e      # Playwright (requiere front + API E2E levantados, ver e2e/README.md)
npm run build    # build de producción
```
