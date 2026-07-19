/** Línea del carrito que se manda a confirmar (`PedidoItemInputDto`). Sin precio: lo pone el backend. */
export interface PedidoItemInput {
  fotoId: number;
  tamanoPrecioId: number;
  cantidad: number;
}

/** Body de `POST fotos/familia/pedidos` (`PedidoConfirmarInputDto`). */
export interface PedidoConfirmarInput {
  nombreContacto: string;
  telefonoContacto: string;
  items: PedidoItemInput[];
}

/** Línea ya confirmada, con el precio que quedó congelado (`PedidoItemConfirmadoDto`). */
export interface PedidoItemConfirmado {
  fotoId: number;
  tamanoPrecioId: number;
  cantidad: number;
  precioUnitarioSnapshot: number;
}

/** Estados del pedido (espeja el enum `EstadoPedido` de la API, ADR-07). */
export const ESTADO_PEDIDO = {
  Pendiente: 0,
  Pagado: 1,
  Impreso: 2,
  Entregado: 3,
  Cancelado: 4,
} as const;

export type EstadoPedido = (typeof ESTADO_PEDIDO)[keyof typeof ESTADO_PEDIDO];

/** Respuesta de confirmar (`PedidoConfirmadoDto`). */
export interface PedidoConfirmado {
  id: number;
  estado: EstadoPedido;
  total: number;
  creadoEn: string;
  items: PedidoItemConfirmado[];
}
