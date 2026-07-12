import { EditableEntity } from '../../../../shared/forms/edit-component-base';

/**
 * Fila de álbum (alumno) del curso (`AlbumDto`). En el update viajan las filas completas y la API
 * reconcilia por id: `id 0` = alta (el sistema le genera su código de acceso); fila ausente = baja
 * (bloqueada por la API si el álbum tiene fotos). `codigoAcceso` es solo de salida: lo que se
 * mande en el input se ignora.
 */
export interface Album {
  id: number;
  nombreAlumno: string;
  codigoAcceso?: string | null;
}

/**
 * Curso (ABM de `api/fotos/cursos`). El listado (`CursoHeaderDto`) trae la cabecera con
 * `cantidadAlbumes`; el detalle (`getById`) trae `albumes` con el código activo de cada uno.
 * El evento al que pertenece debe existir en el tenant (guard de la API).
 */
export interface Curso extends EditableEntity {
  id?: number;
  eventoId: number;
  nombre: string;
  /** Solo en el listado (header). */
  cantidadAlbumes?: number;
  /** Solo en el detalle (getById). */
  albumes?: Album[];
}
