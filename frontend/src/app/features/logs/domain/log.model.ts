/**
 * Entrada del log de aplicación (`LogInfoAllOutput` del endpoint root `logInfo/AllTenants`). Serilog
 * persiste solo errores explícitos en `gen_LogInfos`. Es la **vista cross-tenant de root** → incluye
 * `tenantId`. `exception`/`properties` son los campos pesados que se muestran en el detalle.
 */
/** Tenant para el filtro/columna del log (id + nombre). */
export interface TenantLookup {
  id: number;
  nombre: string;
}

export interface LogInfo {
  id: number;
  message: string;
  messageTemplate?: string;
  level: string;
  timeStamp: string;
  exception?: string | null;
  properties?: string | null;
  tenantId: number;
}
