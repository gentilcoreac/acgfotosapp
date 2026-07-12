import { defineConfig } from 'vitest/config';

// `@material/material-color-utilities` es un paquete ESM ("type": "module") con imports relativos
// SIN extensión (p. ej. `from '../dynamiccolor/dynamic_scheme'`), inválido para el resolver ESM
// estricto de Node. Bajo el build de producción (esbuild) resuelve sin problema, pero Vitest carga
// las dependencias externalizadas vía el loader nativo de Node -> falla. `deps.inline` fuerza a que
// Vite transforme el paquete (resolviendo la extensión como lo hace esbuild) en vez de externalizarlo.
export default defineConfig({
  test: {
    server: {
      deps: {
        inline: ['@material/material-color-utilities'],
      },
    },
  },
});
