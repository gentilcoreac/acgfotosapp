/**
 * Error normalizado de la API, producido por el `errorInterceptor`.
 *
 * La API devuelve los errores con la forma estándar `{ message, errors, traceId? }` (ver
 * `ApiErrorResponse` en el backend). Acá se conserva esa forma para que los consumidores
 * (toast, formularios) no parseen a mano:
 * - `message`: titular del error (una línea). Es la cabecera del toast.
 * - `errors`: detalle accionable (p. ej. varias reglas de validación). Vacío si no hay.
 * - `traceId`: id de correlación que devuelve la API solo en errores técnicos; sirve de "ref"
 *   para soporte (matchea el audit log). Ausente en validaciones de negocio.
 */
export interface ApiError {
  /** Código de estado HTTP (0 si no hubo respuesta del server). */
  status: number;
  /** Titular del error. Siempre presente. */
  message: string;
  /** Detalle accionable (validaciones de negocio). Vacío si no hay. */
  errors: string[];
  /** Id de correlación para soporte (errores técnicos). */
  traceId?: string;
}

/**
 * Lista plana de mensajes a mostrar (para los formularios que pintan los errores inline): el
 * titular seguido del detalle, sin repetidos. Si no hay nada, devuelve `fallback`.
 */
export function errorMessages(
  error: ApiError | null | undefined,
  fallback: string[] = [],
): string[] {
  if (!error) {
    return fallback;
  }
  const all = [error.message, ...(error.errors ?? [])].filter((m): m is string => !!m);
  const unique = all.filter((m, i) => all.indexOf(m) === i);
  return unique.length > 0 ? unique : fallback;
}
