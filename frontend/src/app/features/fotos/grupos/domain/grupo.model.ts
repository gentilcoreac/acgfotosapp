import { EditableEntity } from '../../../../shared/forms/edit-component-base';

/**
 * Fila de participante (participante) del grupo (`ParticipanteDto`). En el update viajan las filas completas y la API
 * reconcilia por id: `id 0` = alta (el sistema le genera su código de acceso); fila ausente = baja
 * (bloqueada por la API si el participante tiene fotos). `codigoAcceso` es solo de salida: lo que se
 * mande en el input se ignora.
 */
export interface Participante {
  id: number;
  nombre: string;
  codigoAcceso?: string | null;
}

/**
 * Tarjeta imprimible de un participante (`TarjetaParticipanteDto` de `GET api/fotos/grupos/{id}/tarjetas`):
 * el código activo, la URL de canje que codifica el QR y el QR ya generado como PNG base64
 * (listo para `<img src="data:image/png;base64,...">`). Campos null si el participante no tiene
 * ningún código activo.
 */
export interface TarjetaParticipante {
  participanteId: number;
  nombre: string;
  codigo: string | null;
  urlCanje: string | null;
  qrPngBase64: string | null;
}

/** Tarjetas de un grupo completo (una por participante) con los nombres para el encabezado. */
export interface TarjetasGrupo {
  grupoId: number;
  nombreGrupo: string;
  nombreEvento: string;
  tarjetas: TarjetaParticipante[];
}

/**
 * Grupo (ABM de `api/fotos/grupos`). El listado (`GrupoHeaderDto`) trae la cabecera con
 * `cantidadParticipantes`; el detalle (`getById`) trae `participantes` con el código activo de cada uno.
 * El evento al que pertenece debe existir en el tenant (guard de la API).
 */
export interface Grupo extends EditableEntity {
  id?: number;
  eventoId: number;
  nombre: string;
  /** Solo en el listado (header). */
  cantidadParticipantes?: number;
  /** Solo en el detalle (getById). */
  participantes?: Participante[];
}
