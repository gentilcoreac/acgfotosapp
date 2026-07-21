import { TbiChipCell } from '../../../../shared/ui/tbi-table/tbi-table.component';

/** Estados del pedido (espeja el enum `EstadoPedido` de la API). */
export const ESTADO_PEDIDO = {
  /** Confirmado por la familia; pendiente de impresión. */
  Pendiente: 0,
  /** Pagado online (Fase 3). Con pago en efectivo el pedido salta directo a Impreso. */
  Pagado: 1,
  /** Copias impresas, listas para entregar. */
  Impreso: 2,
  /** Entregado a la familia. */
  Entregado: 3,
  Cancelado: 4,
} as const;

export type EstadoPedido = (typeof ESTADO_PEDIDO)[keyof typeof ESTADO_PEDIDO];

/** Etiquetas visibles de cada estado (listado y filtro). */
export const ESTADO_PEDIDO_LABEL: Record<EstadoPedido, string> = {
  [ESTADO_PEDIDO.Pendiente]: 'Pendiente',
  [ESTADO_PEDIDO.Pagado]: 'Pagado',
  [ESTADO_PEDIDO.Impreso]: 'Impreso',
  [ESTADO_PEDIDO.Entregado]: 'Entregado',
  [ESTADO_PEDIDO.Cancelado]: 'Cancelado',
};

/** Chip de estado para la columna de la tabla (mismo criterio que `activoChip`). */
export function estadoPedidoChip(estado: EstadoPedido): TbiChipCell {
  switch (estado) {
    case ESTADO_PEDIDO.Pendiente:
      return { label: 'Pendiente', tone: 'warning', icon: 'schedule' };
    case ESTADO_PEDIDO.Pagado:
      return { label: 'Pagado', tone: 'neutral', icon: 'payments' };
    case ESTADO_PEDIDO.Impreso:
      return { label: 'Impreso', tone: 'neutral', icon: 'print' };
    case ESTADO_PEDIDO.Entregado:
      return { label: 'Entregado', tone: 'success', icon: 'check_circle' };
    case ESTADO_PEDIDO.Cancelado:
      return { label: 'Cancelado', tone: 'error', icon: 'cancel' };
  }
}

/** Línea de un pedido (solo lectura: el precio quedó congelado al confirmar, ADR-07). */
export interface PedidoItem {
  fotoId: number;
  nombreArchivoOriginal: string;
  tamanoPrecioNombre: string;
  cantidad: number;
  precioUnitarioSnapshot: number;
}

/**
 * Pedido (admin de `api/fotos/pedidos`, Fase 2). No se crea/edita/borra desde acá — lo confirma
 * la familia; el admin solo lista, consulta el detalle y cambia el estado. El listado
 * (`PedidoHeaderDto`) trae la cabecera con `cantidadItems`; el detalle (`getById`) agrega `items`.
 */
export interface Pedido {
  id: number;
  participanteId: number;
  participanteNombre: string;
  nombreContacto: string;
  telefonoContacto: string;
  estado: EstadoPedido;
  total: number;
  creadoEn: string;
  cantidadItems: number;
  /** Solo en el detalle (getById). */
  items?: PedidoItem[];
}
