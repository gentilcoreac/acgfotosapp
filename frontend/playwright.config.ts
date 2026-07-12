import { defineConfig, devices } from '@playwright/test';

/**
 * Config de la suite E2E (Playwright nativo). Plan y convenciones: `Docs/e2e/README.md`.
 *
 * - El front corre en `E2E_BASE_URL` (ng serve, por defecto :4200) y pega a la API real
 *   (la `apiUrl` del `app.config.json` que sirva el front).
 * - **Login fresco por test** (helper `loginAs`, no storageState compartido): el refresh token
 *   ROTA con detección de replay (ventana de gracia 10s, por ADR), así que un `storageState`
 *   reutilizado sería single-use → flaky. Cada test que necesita sesión loguea en su `beforeEach`.
 * - Serial (workers: 1): los E2E comparten estado en la API/DB. Subir si se aísla por dato.
 */
const BASE_URL = process.env['E2E_BASE_URL'] ?? 'http://localhost:4200';
const isCI = !!process.env['CI'];

/** Navegador compartido por todos los proyectos: Edge del sistema (channel msedge, sin descargar Chromium). */
const EDGE = { ...devices['Desktop Edge'], channel: 'msedge' } as const;

export default defineConfig({
  testDir: './e2e',
  // Reseed determinista de AcgFotos_TestE2E antes de la corrida (degrada elegante si no hay sqlcmd;
  // se omite con E2E_RESEED=0). Ver e2e/global-setup.ts.
  globalSetup: './e2e/global-setup.ts',
  fullyParallel: false,
  forbidOnly: isCI,
  // Con el login del perfil de tests acelerado (PBKDF2 a 1000 iter vía Security__PasswordHasherIterations
  // + seed con hashes de 1000 iter, ver Docs/e2e/base-de-tests.md) los flakes de latencia desaparecen,
  // así que en local alcanza 1 reintento (transitorios). En CI se deja 2 por la variabilidad del runner.
  // Playwright marca el test como "flaky" si pasa en el reintento (no se oculta).
  retries: isCI ? 2 : 1,
  workers: 1,
  reporter: [['html', { open: 'never' }], ['list']],
  timeout: 60_000,
  expect: { timeout: 15_000 }, // margen para la latencia de la API de tests bajo carga de la suite
  use: {
    baseURL: BASE_URL,
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
    video: 'retain-on-failure',
    actionTimeout: 15_000,
    navigationTimeout: 30_000,
  },
  projects: [{ name: 'e2e', testMatch: /specs[\\/].*\.spec\.ts/, use: EDGE }],
  // Levanta automáticamente el front (e2e config → apiUrl :30000) y la API E2E (Kestrel :30000).
  // reuseExistingServer: true → si ya están corriendo los reutiliza sin relanzar.
  // Setup one-time de credenciales: ver Docs/e2e/base-de-tests.md §Setup.
  // webServer: ver README (e2e/README.md) — levantar API y front manualmente antes de correr.
  // La API debe apuntar a AcgFotos_TestE2E (Windows auth). El front con npm run start.
});
