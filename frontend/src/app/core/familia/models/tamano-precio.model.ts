/** Tamaño del catálogo del evento (`TamanoPrecioDto` de `GET fotos/familia/tamanos-precios`). Solo activos. */
export interface TamanoPrecio {
  id: number;
  nombre: string;
  precioUnitario: number;
  orden: number;
  activo: boolean;
}
