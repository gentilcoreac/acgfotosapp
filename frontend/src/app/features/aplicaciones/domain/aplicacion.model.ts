import { EditableEntity } from '../../../shared/forms/edit-component-base';

/**
 * Aplicación (ABM de `api/general/aplicaciones`, DTO `AplicacionDto`).
 *
 * Análisis de contrato (API actual ↔ ABM original `ClienteOriginal`):
 * - La API expone CRUD estándar (`EntityApiControllerBase`); el read path se migró a Mapperly
 *   (`AplicacionMapper`) en la rama `refactor/aplicaciones-mapperly`. No hay HeaderDto/InputDto:
 *   `Aplicacion` no califica (entidad chica, sin campos sensibles ni listado pesado).
 * - `codigo` es **editable** (a diferencia de `codigoPermiso`, no es clave usada en claims): el
 *   ABM original lo dejaba editar y mantenemos ese criterio.
 * - `icono` ⇆ `iconoUrl` son **mutuamente excluyentes**: el original usa un toggle; al guardar,
 *   el campo no elegido va en `null`. Uno de los dos es obligatorio.
 * - `permisos` es **read-only**: viene poblado sólo en el detalle (`getById` → `aplicaciones/{id}`),
 *   no se edita acá (la gestión de permisos vive en su propio ABM). El modelo lo omite porque el
 *   ABM no lo consume.
 */
export interface Aplicacion extends EditableEntity {
  id?: number;
  codigo: string;
  nombre: string;
  activo: boolean;
  /** Nombre del Material Icon. Excluyente con `iconoUrl`. */
  icono: string | null;
  /** URL del ícono. Excluyente con `icono`. */
  iconoUrl: string | null;
}
