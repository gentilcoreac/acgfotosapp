/** Aplicación seleccionable, normalizada desde `aplicaciones-permitidas` (no-root) o `aplicaciones-tenant` (root). */
export interface AplicacionPermitida {
  aplicacionId: number;
  aplicacionNombre: string;
  default: boolean;
}
