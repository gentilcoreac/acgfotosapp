import { test, expect } from '@playwright/test';
import { loginAs } from '../fixtures/auth';
import { TablePage } from '../pages/table.page';
import { UsuarioEditPage } from '../pages/usuario-edit.page';
import { ROOT } from '../fixtures/test-data';

/**
 * Catálogo: `Docs/e2e/casos-e2e.md` → grupo E (CRUD canónico = Usuarios). Contra `AcgFotos_TestE2E`.
 * Los casos que MUTAN son **auto-contenidos**: crean su usuario con nombre único y lo borran en el
 * mismo flujo (no dejan residuo → no hace falta reset global). Actor: root (ABM de su tenant).
 */
test.beforeEach(async ({ page }) => {
  await loginAs(page, ROOT);
  await page.goto('/usuarios');
  await expect(page.getByRole('heading', { name: 'Usuarios' })).toBeVisible();
});

test('E2E-CRUD-01/04 alta → aparece en el listado → borrar → desaparece', async ({ page }) => {
  const userName = `e2e${Date.now()}`;
  const table = new TablePage(page);

  // Alta
  await page.getByRole('button', { name: 'Nuevo' }).click();
  const edit = new UsuarioEditPage(page);
  await edit.fillGeneral({
    userName,
    nombre: 'E2E',
    apellido: 'Test',
    email: `${userName}@e2e.test`,
  });
  await edit.save();
  await expect(edit.dialog).toBeHidden();

  // Aparece en el listado
  await table.search(userName);
  await expect(table.row(userName)).toBeVisible();

  // Borrar (acción de fila + confirmación)
  await table.rowAction(userName, 'Eliminar');
  await page.getByRole('button', { name: 'Confirmar' }).click();

  // Desaparece
  await expect(table.row(userName)).toHaveCount(0);
});

test('E2E-CRUD-02 editar un usuario → el cambio persiste tras F5', async ({ page }) => {
  const userName = `e2e${Date.now()}`;
  const table = new TablePage(page);
  const edit = new UsuarioEditPage(page);

  // Alta con nombre "Orig"
  await page.getByRole('button', { name: 'Nuevo' }).click();
  await edit.fillGeneral({ userName, nombre: 'Orig', apellido: 'Test', email: `${userName}@e2e.test` });
  await edit.save();
  await expect(edit.dialog).toBeHidden();

  // Editar el nombre → "Editado"
  await table.search(userName);
  await table.rowAction(userName, 'Editar');
  await expect(edit.nombre).toHaveValue('Orig'); // el diálogo cargó el usuario
  await edit.nombre.fill('Editado');
  await edit.save();
  await expect(edit.dialog).toBeHidden();

  // F5 → el cambio persistió (se guardó en la API)
  await page.reload();
  await table.search(userName);
  await expect(table.row(userName)).toContainText('Editado');

  // Cleanup
  await table.rowAction(userName, 'Eliminar');
  await page.getByRole('button', { name: 'Confirmar' }).click();
  await expect(table.row(userName)).toHaveCount(0);
});

test('E2E-CRUD-03 editar en la página 2 conserva la página', async ({ page }) => {
  const table = new TablePage(page);
  await table.nextPage(); // ir a la página 2
  const rangeBefore = (await table.rangeLabel.innerText()).trim();
  const userOnPage2 = await table.firstRowUserName();

  await table.rowAction(userOnPage2, 'Editar');
  const edit = new UsuarioEditPage(page);
  await expect(edit.nombre).not.toHaveValue(''); // cargó
  await edit.save(); // guardar sin cambios

  await expect(edit.dialog).toBeHidden();
  // Sigue en la página 2 (no se reseteó a la 1): el usuario sigue visible y el rango no cambió.
  await expect(table.row(userOnPage2)).toBeVisible();
  await expect(table.rangeLabel).toHaveText(rangeBefore);
});

test('E2E-CRUD-07 la paginación trae otra página de datos', async ({ page }) => {
  const table = new TablePage(page);
  const userOnPage1 = await table.firstRowUserName();
  const rangeStartBefore = await table.rangeStart();

  await table.nextPage();

  await expect(table.row(userOnPage1)).toHaveCount(0); // el de la página 1 ya no está
  // El rango avanzó a una página posterior (sin acoplar al pageSize: solo que el inicio creció).
  expect(await table.rangeStart()).toBeGreaterThan(rangeStartBefore);
});

test('E2E-CRUD-05 el buscador filtra el listado (server-side)', async ({ page }) => {
  const table = new TablePage(page);
  await table.search('root');
  await expect(table.row('root')).toBeVisible();

  await table.search('zzz-no-existe-xyz');
  await expect(table.emptyState).toBeVisible();
});

test('E2E-CRUD-06 el selector de columnas muestra/oculta una columna', async ({ page }) => {
  const table = new TablePage(page);
  await expect(table.headerCell('Email')).toBeVisible();

  await table.toggleColumn('Email');
  await expect(table.headerCell('Email')).toHaveCount(0);

  await table.toggleColumn('Email');
  await expect(table.headerCell('Email')).toBeVisible();
});

test('E2E-CRUD-08 validación bloquea guardar con el form vacío', async ({ page }) => {
  await page.getByRole('button', { name: 'Nuevo' }).click();
  const edit = new UsuarioEditPage(page);

  await edit.save(); // sin completar nada

  await expect(page.getByText('Requerido').first()).toBeVisible(); // feedback de requerido
  await expect(edit.dialog).toBeVisible(); // no cierra el diálogo
});
