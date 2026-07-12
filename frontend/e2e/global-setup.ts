import { execFileSync } from 'node:child_process';
import * as path from 'node:path';

/**
 * Reseed determinista antes de la corrida E2E (gap §9 / "Reset entre corridas" en
 * `Docs/e2e/base-de-tests.md`). Lleva `AcgFotos_TestE2E` al estado base conocido:
 *
 *  1. `seed/e2e-reset.sql`  → purga los datos transitorios que dejan los tests que mutan
 *     (usuarios `e2e*` del CRUD que hubieran quedado por una corrida interrumpida).
 *  2. `seed/e2e-extras.sql` → re-aplica (idempotente) los datos sintéticos del seed por si faltaran.
 *
 * **Reset directo de DB** (no endpoint): el dev box ya corre SQL Server local con auth Windows y así
 * se creó la base, así que reusamos `sqlcmd` (cero código nuevo en la API). El catálogo (constante) NO
 * se re-siembra acá.
 *
 * **Degradación elegante:** si `E2E_RESEED=0` se omite; si `sqlcmd` no está o falla, se **avisa y se
 * continúa** (los tests que mutan son auto-contenidos → la suite corre igual). Config por env:
 * `E2E_DB_SERVER` (default `localhost`), `E2E_DB_NAME` (default `AcgFotos_TestE2E`).
 */
export default function globalSetup(): void {
  if (['0', 'false', 'no'].includes((process.env['E2E_RESEED'] ?? '').toLowerCase())) {
    console.log('[e2e] reseed omitido (E2E_RESEED=0).');
    return;
  }

  const server = process.env['E2E_DB_SERVER'] ?? 'localhost';
  const database = process.env['E2E_DB_NAME'] ?? 'AcgFotos_TestE2E';
  const seedDir = path.join(__dirname, 'seed');
  const scripts = ['e2e-reset.sql', 'e2e-extras.sql'];

  try {
    for (const script of scripts) {
      execFileSync('sqlcmd', ['-S', server, '-d', database, '-E', '-b', '-i', path.join(seedDir, script)], {
        stdio: 'inherit',
      });
    }
    console.log(`[e2e] reseed OK (${database} @ ${server}).`);
  } catch (err) {
    const msg = err instanceof Error ? err.message : String(err);
    console.warn(
      `[e2e] reseed OMITIDO (no se pudo correr sqlcmd: ${msg}).\n` +
        `      La suite continúa: los tests que mutan son auto-contenidos. Para activarlo, ` +
        `tener sqlcmd en el PATH y ${database} accesible (o E2E_RESEED=0 para silenciar este aviso).`,
    );
  }
}
