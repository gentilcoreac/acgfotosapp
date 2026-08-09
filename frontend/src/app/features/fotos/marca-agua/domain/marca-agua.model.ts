import { EditableEntity } from '../../../../shared/forms/edit-component-base';

/** Espeja `ModoColocacionMarcaAgua` de la API. */
export const MODO_COLOCACION = {
  /** Repetida en mosaico cubriendo toda la foto (el modo más difícil de recortar). */
  Repetida: 0,
  /** Una sola vez, en `posicion`. */
  PosicionFija: 1,
} as const;
export type ModoColocacion = (typeof MODO_COLOCACION)[keyof typeof MODO_COLOCACION];

/** Espeja `PosicionMarcaAgua` de la API: grilla de 9 posiciones fijas. */
export const POSICION = {
  ArribaIzquierda: 0,
  ArribaCentro: 1,
  ArribaDerecha: 2,
  CentroIzquierda: 3,
  Centro: 4,
  CentroDerecha: 5,
  AbajoIzquierda: 6,
  AbajoCentro: 7,
  AbajoDerecha: 8,
} as const;
export type Posicion = (typeof POSICION)[keyof typeof POSICION];

/** Espeja `ModoFusionMarcaAgua` de la API — mapea 1 a 1 a `globalCompositeOperation` del canvas. */
export const MODO_FUSION = {
  Normal: 0,
  Superponer: 1,
  Diferencia: 2,
} as const;
export type ModoFusion = (typeof MODO_FUSION)[keyof typeof MODO_FUSION];

export const MODO_FUSION_COMPOSITE_OPERATION: Record<ModoFusion, GlobalCompositeOperation> = {
  [MODO_FUSION.Normal]: 'source-over',
  [MODO_FUSION.Superponer]: 'overlay',
  [MODO_FUSION.Diferencia]: 'difference',
};

/**
 * Capa de un perfil (`CapaMarcaAguaDto`). Una capa nueva SIEMPRE nace subiendo su imagen
 * (`MarcaAguaService.subirCapa`) — este tipo sólo se usa para leer/editar la colocación de capas
 * ya existentes (`id` real, nunca 0): el `PerfilMarcaAguaInputDto` del backend rechaza `id: 0`.
 */
export interface CapaMarcaAgua {
  id: number;
  orden: number;
  storageKey: string;
  anchoPx: number;
  altoPx: number;
  modoColocacion: ModoColocacion;
  /** Sólo aplica con `MODO_COLOCACION.PosicionFija`. Siempre presente (nunca `undefined`) para que Signal Forms pueda validarlo dentro de `applyEach`. */
  posicion: Posicion | null;
  escalaPorcentaje: number;
  margenPorcentaje: number;
  /** Cada cuánto se repite la marca, en % del ancho de la foto — independiente de `escalaPorcentaje`. Sólo aplica con `MODO_COLOCACION.Repetida`. */
  separacionPorcentaje: number;
  anguloGrados: number;
  opacidad: number;
  modoFusion: ModoFusion;
}

/**
 * Perfil de marca de agua (`PerfilMarcaAguaDto`/`PerfilMarcaAguaInputDto`, ADR-15). El detalle y el
 * listado traen las capas — spec: "listado con la marca real renderizada por fila".
 */
export interface PerfilMarcaAgua extends EditableEntity {
  id?: number;
  nombre: string;
  esDefault: boolean;
  marcarThumb: boolean;
  capas: CapaMarcaAgua[];
  /** Avisos del backend (D12), ej. "sin ninguna protección" — sólo lectura. */
  avisos?: string[];
}

/** Respuesta de subir el PNG de una capa (`CapaMarcaAguaSubidaDto`, design.md D14). */
export interface CapaMarcaAguaSubida {
  perfil: PerfilMarcaAgua;
  capa: CapaMarcaAgua;
  avisos: string[];
}
