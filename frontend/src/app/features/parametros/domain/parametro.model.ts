import { EditableEntity } from '../../../shared/forms/edit-component-base';
import { TbiSelectOption } from '../../../shared/ui/tbi-select/tbi-select.component';

/**
 * Parámetro de sistema/aplicación (ABM de `api/general/parametros`, DTO `ParametroDto`).
 *
 * El modelo del front original (`_core/models/general/parametro.model.ts`) mezclaba estos
 * campos con los del override por tenant (`parametroValorTenant`, `parametroValorDefaultValue`,
 * `parametroValorId`); esos pertenecen a `ParametroValorOutput` y al feature **separado**
 * `parametros-valor-tenant`, no a este ABM. Acá el modelo refleja sólo el `ParametroDto`.
 */
export interface Parametro extends EditableEntity {
  id?: number;
  nombre: string;
  valor: string;
  descripcion: string;
  aplicacionId: number;
  /** Nombre de la aplicación, sólo lectura (lo deriva la API para el listado). */
  aplicacionNombre?: string;
  /** 1=Integer · 2=Boolean · 3=Text (sin enum en la API; ver `TIPO_DATO_OPTIONS`). */
  tipoDato: number;
  /** FK nullable en la API; el ABM original lo exige y mantenemos ese criterio. */
  permisoId: number | null;
}

/**
 * Aplicación (`AplicacionDto`). En el listado (`getAll`) viene sin `permisos`; el detalle
 * (`getById` → `aplicaciones/{id}`) **sí** los trae poblados. Lo usamos para cargar los
 * permisos de la aplicación elegida en el edit (el listado de `permisos` ya no expone
 * `aplicacionId`, así que filtrar contra él no es posible — ver `data/parametros.service.ts`).
 */
export interface AplicacionOption {
  id: number;
  nombre: string;
  permisos?: PermisoOption[];
}

/** Opción de permiso (subconjunto de `PermisoDto`) para el select del edit. */
export interface PermisoOption {
  id: number;
  nombre: string;
}

/**
 * Tipos de dato de un parámetro. Valores 1/2/3 heredados del front original y validados
 * contra `seed.sql` (la API guarda el `int` tal cual, sin enum).
 */
export const TIPO_DATO_OPTIONS: TbiSelectOption<number>[] = [
  { value: 1, label: 'Integer' },
  { value: 2, label: 'Boolean' },
  { value: 3, label: 'Text' },
];
