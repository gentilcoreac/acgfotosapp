import { Injectable, inject } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ErrorSnackComponent, ErrorSnackData } from './error-snack.component';

/**
 * Ventana de coalescencia de errores. El `errorInterceptor` notifica TODO error HTTP (fuente única);
 * los componentes además llaman `error()` con su mensaje contextual para la misma falla. Como el
 * interceptor corre primero (al desenrollar la respuesta) y el componente justo después, dentro de
 * esta ventana el segundo toast se descarta → un solo cartel, sin doble. También evita la avalancha
 * cuando fallan varios requests juntos (p. ej. los widgets del home con 429).
 */
const ERROR_COALESCE_MS = 3000;

/**
 * Notificaciones de éxito/error sobre `MatSnackBar`. Reemplaza al `NotificationService`
 * del original.
 *
 * - `error(message)` simple → snackbar de una línea (caso de la mayoría: una falla, un mensaje).
 * - `error(message, detail)` → abre el `ErrorSnackComponent` con "Ver detalle" (validaciones de
 *   negocio que traen varias reglas: no se aplastan en un `join(' · ')`).
 * - `error(message, [], traceId)` → snackbar con "ref" copiable para errores técnicos.
 */
@Injectable({ providedIn: 'root' })
export class NotificationService {
  private readonly snackBar = inject(MatSnackBar);
  private lastErrorAt = 0;

  success(message: string): void {
    this.snackBar.open(message, 'OK', {
      duration: 4000,
      panelClass: 'tbi-snack--success',
    });
  }

  /**
   * @param message Titular del error (una línea).
   * @param detail Detalle accionable (validaciones). Si hay, el toast trae "Ver detalle".
   * @param traceId Id de correlación (errores técnicos): se muestra como "ref" copiable.
   */
  error(message: string, detail: string[] = [], traceId?: string): void {
    // Coalesce: el primer error de la ráfaga gana; los inmediatamente siguientes se descartan
    // (ver ERROR_COALESCE_MS). Evita el doble toast interceptor+componente y la avalancha de 429.
    const now = Date.now();
    if (now - this.lastErrorAt < ERROR_COALESCE_MS) {
      return;
    }
    this.lastErrorAt = now;

    const hasDetail = detail.length > 0;
    // El "ref" solo tiene sentido en errores técnicos (sin detalle accionable que mostrar).
    const ref = hasDetail ? undefined : traceId;

    if (!hasDetail && !ref) {
      this.snackBar.open(message, 'Cerrar', {
        duration: 8000,
        panelClass: 'tbi-snack--error',
      });
      return;
    }

    this.snackBar.openFromComponent<ErrorSnackComponent, ErrorSnackData>(ErrorSnackComponent, {
      data: { message, detail, traceId: ref },
      panelClass: 'tbi-snack--error',
      // Con detalle queda fijo hasta que el usuario cierre (puede estar leyendo varias reglas); el
      // caso de solo-ref se autocierra como un error normal.
      duration: hasDetail ? undefined : 10000,
    });
  }
}
