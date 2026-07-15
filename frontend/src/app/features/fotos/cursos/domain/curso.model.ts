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
 * Tarjeta imprimible de un álbum (`TarjetaAlbumDto` de `GET api/fotos/cursos/{id}/tarjetas`):
 * el código activo, la URL de canje que codifica el QR y el QR ya generado como PNG base64
 * (listo para `<img src="data:image/png;base64,...">`). Campos null si el álbum no tiene
 * ningún código activo.
 */
export interface TarjetaAlbum {
  albumId: number;
  nombreAlumno: string;
  codigo: string | null;
  urlCanje: string | null;
  qrPngBase64: string | null;
}

/** Tarjetas de un curso completo (una por alumno) con los nombres para el encabezado. */
export interface TarjetasCurso {
  cursoId: number;
  nombreCurso: string;
  nombreEvento: string;
  tarjetas: TarjetaAlbum[];
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
