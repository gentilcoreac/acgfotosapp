/** Formato de precio de la zona de familias (`$ 1234,50`); sin `Intl` para no arrastrar configuración de locale acá. */
export function formatearPrecio(monto: number): string {
  return `$ ${monto.toFixed(2).replace('.', ',')}`;
}
