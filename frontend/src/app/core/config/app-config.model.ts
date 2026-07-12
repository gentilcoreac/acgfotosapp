/**
 * Configuración de la aplicación cargada en runtime desde
 * `configurations/app.config.json` (un archivo por ambiente).
 *
 * Reemplaza el patrón del proyecto original donde estos valores se inyectaban
 * mutando el objeto global `environment`. Acá es un contrato inmutable y tipado.
 *
 * NOTA: el estado mutable que el original guardaba en `environment` (tenant,
 * tenantId) NO va acá — pertenece a los stores de sesión/tenant.
 */
export interface AppConfig {
  /** Título base de la app (se sobreescribe por tenant en runtime). */
  readonly appTitle: string;
  /** Año mostrado en el footer/copyright. */
  readonly year: string;
  /** Versión del cliente. */
  readonly version: string;
  /** Flag de ambiente productivo (informativo; la optimización la maneja el build). */
  readonly production: boolean;

  /** URL base de la API (sin path; ej. `https://api.midominio.com`). */
  readonly apiUrl: string;

  /** Parámetros de detección de inactividad de sesión. */
  readonly inactivity: InactivityConfig;

  /** Tamaño de página por defecto en listados. */
  readonly pageSize: number;
  /** Opciones de tamaño de página ofrecidas en los paginadores. */
  readonly pageSizeOptions: readonly number[];

  /**
   * Tenant por defecto para cargar tema/logo/favicon antes del login.
   * Reservado para Fase 2 (consumo de `tenants/public-style`); todavía no se consume.
   */
  readonly defaultTenant: string;
  /**
   * Tema inicial de la app cuando no hay preferencia guardada ni default de tenant.
   * Valores válidos: `light` | `dark` (lo consume `ThemeStore.initialize()`; otros valores
   * caen a la preferencia del SO).
   */
  readonly defaultTheme: string;

  /** Longitud mínima aceptada para teléfonos. */
  readonly phoneMinLength: number;
  /** Longitud máxima aceptada para teléfonos. */
  readonly phoneMaxLength: number;

  /** URL del sistema anterior (link de migración/legacy). */
  readonly oldSystemUrl: string;
}

export interface InactivityConfig {
  /** Tiempo máximo de inactividad antes de avisar, en ms. */
  readonly maxTimeMs: number;
  /** Tiempo para responder al aviso de inactividad antes de cerrar sesión, en ms. */
  readonly maxResponseTimeMs: number;
}
