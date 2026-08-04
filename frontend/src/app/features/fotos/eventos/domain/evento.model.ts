import { EditableEntity } from '../../../../shared/forms/edit-component-base';

/** Estados del ciclo de vida del evento (espeja el enum `EstadoEvento` de la API). */
export const ESTADO_EVENTO = {
  /** El fotógrafo lo está armando: las familias todavía no pueden entrar. */
  Borrador: 0,
  /** Visible para las familias con código. */
  Publicado: 1,
  /** Terminado: no se aceptan más pedidos. */
  Cerrado: 2,
} as const;

export type EstadoEvento = (typeof ESTADO_EVENTO)[keyof typeof ESTADO_EVENTO];

/** Etiquetas visibles de cada estado (listado y select del ABM). */
export const ESTADO_EVENTO_LABEL: Record<EstadoEvento, string> = {
  [ESTADO_EVENTO.Borrador]: 'Borrador',
  [ESTADO_EVENTO.Publicado]: 'Publicado',
  [ESTADO_EVENTO.Cerrado]: 'Cerrado',
};

/**
 * Fila del catálogo de tamaños del evento (`TamanoPrecioDto`). En el update viajan las filas
 * completas y la API reconcilia por id: `id 0` = alta; fila ausente = baja.
 */
export interface TamanoPrecio {
  id: number;
  nombre: string;
  precioUnitario: number;
  orden: number;
  activo: boolean;
}

/**
 * Evento (ABM de `api/fotos/eventos`). El listado (`EventoHeaderDto`) trae solo la cabecera;
 * el detalle (`getById`) agrega `tamanosPrecios`. Las fechas son ISO de la API; el form las
 * edita como `yyyy-MM-dd` (el shape de `tbi-date-picker`).
 */
export interface Evento extends EditableEntity {
  id?: number;
  nombre: string;
  lugarOrganizacion?: string | null;
  fecha?: string | null;
  fechaExpiracion?: string | null;
  estado: EstadoEvento;
  /** Null = usa el perfil/opciones default del estudio (ADR-15 §5). */
  perfilMarcaAguaId?: number | null;
  opcionesPublicacionId?: number | null;
  tamanosPrecios?: TamanoPrecio[];
}