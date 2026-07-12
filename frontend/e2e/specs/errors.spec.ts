import { test, expect } from '@playwright/test';
import { loginAs } from '../fixtures/auth';
import { LoginPage } from '../pages/login.page';
import { ShellPage } from '../pages/shell.page';
import { TablePage } from '../pages/table.page';
import { UsuarioEditPage } from '../pages/usuario-edit.page';
import { ErrorSnackPage } from '../pages/error-snack.page';
import { ROOT } from '../fixtures/test-data';

/**
 * Catálogo: `Docs/e2e/casos-e2e.md` → grupo F (Errores y feedback). El `errorInterceptor` es la única
 * fuente de toasts de error (forma estándar `{ message, errors, traceId }`); `NotificationService`
 * coalesce los errores en una ventana de 3s. Estos casos inyectan respuestas con `page.route` para
 * forzar el error de forma determinista (no dependen de datos del seed).
 */

test('E2E-ERR-01 una ráfaga de errores 500 muestra un solo toast (coalescencia)', async ({
  page,
}) => {
  await loginAs(page, ROOT);
  await page.goto('/usuarios');
  await expect(page.getByRole('heading', { name: 'Usuarios' })).toBeVisible();
  const table = new TablePage(page);

  // El listado ya cargó OK; a partir de acá cada búsqueda al server falla con un mensaje distinto,
  // así distinguimos el 1er cartel del 2º. La coalescencia (3s) debe descartar el segundo.
  // El listado pega a `/api/general/usuarios` (el CrudClient usa el módulo `general`); el matcher
  // excluye `/update` y subrecursos (`/{id}/...`).
  let calls = 0;
  await page.route(/\/api\/general\/usuarios(\?|$)/, async (route) => {
    calls += 1;
    await route.fulfill({
      status: 500,
      contentType: 'application/json',
      body: JSON.stringify({ message: `Fallo de servidor #${calls}` }),
    });
  });

  const snack = new ErrorSnackPage(page);

  // 1ª falla → toast #1
  await table.search('aaa');
  await expect.poll(() => calls).toBeGreaterThanOrEqual(1);
  await expect(page.getByText('Fallo de servidor #1')).toBeVisible();

  // 2ª falla, dentro de la ventana de 3s → coalesce (el 2º no abre un nuevo cartel)
  const callsAfterFirst = calls;
  await table.search('bbb');
  await expect.poll(() => calls).toBeGreaterThan(callsAfterFirst);

  await expect(page.getByText('Fallo de servidor #2')).toHaveCount(0); // descartado
  await expect(page.getByText('Fallo de servidor #1')).toBeVisible(); // queda el primero
  await expect(snack.root).toHaveCount(1); // un solo cartel
});

test('E2E-ERR-02 toast con detalle: "Ver detalle" despliega las reglas + "Copiar"', async ({
  page,
}) => {
  await loginAs(page, ROOT);
  await page.goto('/usuarios');

  // Guardar devuelve una validación de negocio con varias reglas (forma estándar con `errors`).
  // El save pega a `/api/general/usuarios/update` (CrudClient con módulo `general`).
  await page.route('**/api/general/usuarios/update', (route) =>
    route.fulfill({
      status: 400,
      contentType: 'application/json',
      body: JSON.stringify({
        message: 'No se pudo guardar el usuario',
        errors: ['El email ya está registrado', 'El nombre de usuario es inválido'],
        traceId: 'e2e-trace-detalle',
      }),
    }),
  );

  await page.getByRole('button', { name: 'Nuevo' }).click();
  const edit = new UsuarioEditPage(page);
  const userName = `e2e${Date.now()}`;
  await edit.fillGeneral({
    userName,
    nombre: 'E2E',
    apellido: 'Detalle',
    email: `${userName}@e2e.test`,
  });
  await edit.save();

  const snack = new ErrorSnackPage(page);
  await expect(snack.text('No se pudo guardar el usuario')).toBeVisible();

  // El detalle viene plegado: aparece "Ver detalle" y, al abrirlo, la lista completa de reglas.
  await expect(snack.toggle).toBeVisible();
  await expect(snack.toggle).toHaveText('Ver detalle');
  await snack.toggle.click();
  await expect(snack.text('El email ya está registrado')).toBeVisible();
  await expect(snack.text('El nombre de usuario es inválido')).toBeVisible();
  await expect(snack.copy).toBeVisible();
});

test('E2E-ERR-02 (ref) error técnico muestra "ref" copiable (sin "Ver detalle")', async ({
  page,
}) => {
  await loginAs(page, ROOT);
  await page.goto('/usuarios');

  // Error técnico (500) con traceId y SIN detalle accionable → el toast muestra "ref" copiable.
  // Por diseño de NotificationService, detalle y ref son mutuamente excluyentes (ver error-snack).
  await page.route('**/api/general/usuarios/update', (route) =>
    route.fulfill({
      status: 500,
      contentType: 'application/json',
      body: JSON.stringify({ message: 'Error interno del servidor', traceId: 'e2e-trace-500' }),
    }),
  );

  await page.getByRole('button', { name: 'Nuevo' }).click();
  const edit = new UsuarioEditPage(page);
  const userName = `e2e${Date.now()}`;
  await edit.fillGeneral({
    userName,
    nombre: 'E2E',
    apellido: 'Ref',
    email: `${userName}@e2e.test`,
  });
  await edit.save();

  const snack = new ErrorSnackPage(page);
  await expect(snack.ref).toHaveText('ref: e2e-trace-500');
  await expect(snack.copy).toBeVisible();
  await expect(snack.toggle).toHaveCount(0); // sin detalle → no hay "Ver detalle"
});

test('E2E-ERR-03 el botón de login muestra spinner y bloquea el doble submit', async ({ page }) => {
  const login = new LoginPage(page);
  await login.goto();

  // Retiene la respuesta del login para poder observar el estado de carga del botón.
  let tokenCalls = 0;
  await page.route('**/auth/token', async (route) => {
    tokenCalls += 1;
    await new Promise((resolve) => setTimeout(resolve, 1200));
    await route.continue();
  });

  await login.userName.fill(ROOT.username);
  await login.password.fill(ROOT.password);
  await login.submit.click();

  // En vuelo: el botón pasa a "Ingresando…", deshabilitado y con spinner → no se puede re-disparar.
  const loadingButton = page.getByRole('button', { name: 'Ingresando…' });
  await expect(loadingButton).toBeVisible();
  await expect(loadingButton).toBeDisabled();
  await expect(page.getByTestId('button-spinner')).toBeVisible();

  // Completa el login con un único request pese al estado de carga.
  await new ShellPage(page).expectLoggedIn();
  expect(tokenCalls).toBe(1);
});

test('E2E-ERR-04 una ruta inexistente muestra la página 404 (** → /404)', async ({ page }) => {
  await loginAs(page, ROOT);

  await page.goto('/esta-ruta-no-existe-xyz');

  // El wildcard `{ path: '**', redirectTo: '404' }` lleva a la página 404 dentro del layout: el
  // usuario sigue logueado (conserva el menú) y ve el mensaje de "Página no encontrada".
  const shell = new ShellPage(page);
  await expect(shell.logoutButton).toBeVisible();
  await expect(page.getByTestId('not-found-page')).toBeVisible();
  await expect.poll(() => new URL(page.url()).pathname).toBe('/404');
});
