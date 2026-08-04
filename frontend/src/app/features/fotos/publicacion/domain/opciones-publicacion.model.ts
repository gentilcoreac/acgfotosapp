import { EditableEntity } from '../../../../shared/forms/edit-component-base';

/**
 * Conjunto de resolución/calidad de publicación (`OpcionesPublicacionDto`, ADR-15 §5,
 * `api/fotos/marca-agua/opciones-publicacion`). Eje independiente del perfil de marca de agua: el
 * mismo circuito default-del-tenant → override por evento, pero una entidad propia.
 */
export interface OpcionesPublicacion extends EditableEntity {
  id?: number;
  nombre: string;
  esDefault: boolean;
  ladoMayorPreview: number;
  ladoMayorThumb: number;
  calidad: number;
}
