import { TbiSelectOption } from '../../../shared/ui/tbi-select/tbi-select.component';

/**
 * Override de un parámetro **para un tenant puntual** (ABM de `api/general/parametros-valores`).
 *
 * Contexto: cada `Parametro` tiene un valor por defecto global (ABM `parametros`). Acá, root puede
 * customizar ese valor para un tenant específico; el backend ya resuelve el override en runtime
 * (`ParametroAppService.ValorParametroPorNombreAsync` aplica el valor del tenant si existe, y cae
 * al default si no). Esta feature es la cara de administración de ese mecanismo.
 *
 * Pantalla **root-only**: el endpoint `aplicaciones-tenant-id/{tenantId}` (y por ende
 * `parametros-por-tenant-aplicacion`) exige `IsRoot && TenantId == rootTenantId` en la API. El
 * acceso lo gobierna el permiso de menú `ParametroValorTenant` (root lo recibe siempre).
 */

/** Tipo de dato del parámetro (mismos valores que la feature `parametros`, validados vs `seed.sql`). */
export const TIPO_DATO = {
  Entero: 1,
  Booleano: 2,
  Texto: 3,
} as const;

/**
 * Fila de la grilla: un parámetro de la aplicación con su valor **efectivo** para el tenant
 * elegido. Mirror del `ParametroValorOutput` de la API (sólo los campos que consume el front;
 * el resto —`esPrivado`, overrides por usuario/rol, etc.— se ignoran por tipado estructural).
 */
export interface ParametroValorRow {
  /** Id del `Parametro` (no del override). */
  id: number;
  nombre: string;
  descripcion: string;
  /** Valor efectivo: el override del tenant si existe; si no, el default del parámetro. */
  valor: string;
  /** 1=Entero · 2=Booleano · 3=Texto (ver `TIPO_DATO`). */
  tipoDato: number;
  aplicacionId: number;
  /** Id del override (`gen_ParametrosValoresTenants`); `null`/ausente si el tenant usa el default. */
  parametroValorId: number | null;
  /** Valor por defecto del parámetro (para restaurar al borrar el override). */
  parametroValorDefaultValue: string;
}

/**
 * Body del upsert del override (`POST general/parametros-valores/update`). La API resuelve por el
 * id del body (`UpdateAsync`): **ausente/0 → crea**, con `id` → actualiza. El índice único
 * `(tenantId, parametroId)` evita duplicados.
 */
export interface ParametroValorTenantInput {
  /** Id del override existente; omitir (o 0) para crear uno nuevo. */
  id?: number;
  tenantId: number;
  parametroId: number;
  valor: string;
}

/** Item mínimo del listado de tenants (`TenantHeaderDto`) que consume el selector. */
export interface TenantListItem {
  id?: number;
  codigo: string;
  nombre: string;
}

/** Aplicación habilitada del tenant (`AplicacionDto` de `aplicaciones-tenant-id/{tenantId}`). */
export interface AplicacionTenant {
  id: number;
  nombre: string;
}

/** Texto para mostrar el valor efectivo de un booleano en modo lectura. */
export function displayValor(row: ParametroValorRow): string {
  if (row.tipoDato === TIPO_DATO.Booleano) {
    return esBooleanoTrue(row.valor) ? 'Sí' : 'No';
  }
  return row.valor ?? '';
}

/** Normaliza el valor string de un parámetro booleano a `boolean`. */
export function esBooleanoTrue(valor: string | null | undefined): boolean {
  return String(valor ?? '').toLowerCase() === 'true';
}

/** Opción para el `tbi-select` de tenants (etiqueta `codigo — nombre`, como el original). */
export function toTenantOption(t: TenantListItem): TbiSelectOption<number> {
  return { value: t.id as number, label: t.codigo ? `${t.codigo} — ${t.nombre}` : t.nombre };
}
