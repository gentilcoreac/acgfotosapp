/** Estado del pipeline de derivados (`EstadoProcesamientoFoto` de la API). */
export enum EstadoProcesamientoFoto {
  /** Original guardado; el worker todavía no generó thumb/preview. */
  Pendiente = 0,
  /** Derivados generados: ya puede mostrarse. */
  Lista = 1,
  /** Falló el procesamiento (detalle en `errorProcesamiento`). */
  Error = 2,
}

/**
 * Foto del admin (`FotoDto`): respuesta del upload y del listado por curso. Los bytes NUNCA viajan
 * en el DTO — thumb/preview/original se piden por endpoints propios (galería, próxima pantalla).
 */
export interface Foto {
  id: number;
  eventoId: number;
  cursoId: number;
  /** Null ⇒ foto grupal del curso (visible para todos los álbumes). */
  albumId: number | null;
  nombreArchivoOriginal: string;
  ancho: number;
  alto: number;
  tamanoBytes: number;
  estadoProcesamiento: EstadoProcesamientoFoto;
  errorProcesamiento: string | null;
  creadoEn: string;
}
