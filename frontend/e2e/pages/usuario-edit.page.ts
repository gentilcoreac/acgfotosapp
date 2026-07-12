import { type Page, type Locator } from '@playwright/test';

/** Datos mínimos (tab General) para dar de alta un usuario. */
export interface UsuarioGeneral {
  userName: string;
  nombre: string;
  apellido: string;
  email: string;
}

/**
 * Page Object del diálogo de alta/edición de usuario (`UsuarioEditComponent`). Sólo cubre el tab
 * General (campos requeridos: userName/nombre/apellido/email); licencia/roles/apps son opcionales.
 */
export class UsuarioEditPage {
  readonly dialog: Locator;
  readonly userName: Locator;
  readonly nombre: Locator;
  readonly apellido: Locator;
  readonly email: Locator;
  readonly saveButton: Locator;
  readonly cancelButton: Locator;

  constructor(private readonly page: Page) {
    this.dialog = page.getByRole('dialog');
    this.userName = this.dialog.getByLabel('Usuario');
    this.nombre = this.dialog.getByLabel('Nombre');
    this.apellido = this.dialog.getByLabel('Apellido');
    this.email = this.dialog.getByLabel('Email');
    this.saveButton = this.dialog.getByRole('button', { name: 'Guardar' });
    this.cancelButton = this.dialog.getByRole('button', { name: 'Cancelar' });
  }

  async fillGeneral(data: UsuarioGeneral): Promise<void> {
    await this.userName.fill(data.userName);
    await this.nombre.fill(data.nombre);
    await this.apellido.fill(data.apellido);
    await this.email.fill(data.email);
  }

  async save(): Promise<void> {
    await this.saveButton.click();
  }
}
