import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiClient } from '../../../core/http';
import { LicenciaResumen } from '../domain/licencia-resumen.model';

/**
 * Servicio de datos de licencias (a nivel tenant). Expone el resumen de licencias que la API ya
 * calculaba para validar el alta/edición de tenants: por cada tipo de licencia, el total
 * contratado, lo asignado/disponible y su estado de vencimiento.
 *
 * Es deliberadamente transversal a `usuarios`: lo consumen el listado de usuarios (columna +
 * banner + indicador) y, en una fase posterior, el homepage. Por eso vive en su propia feature
 * en vez de colgar de `usuarios` (el endpoint está bajo el controller de usuarios sólo por ruta).
 */
@Injectable({ providedIn: 'root' })
export class LicenciasService {
  private readonly api = inject(ApiClient);

  /**
   * Resumen de licencias del tenant actual. Una sola llamada alimenta la columna de licencia del
   * listado (id→descripción), el banner de "por vencer" y el indicador de disponibles.
   */
  getResumen(): Observable<LicenciaResumen[]> {
    return this.api.get<LicenciaResumen[]>('general/usuarios/get-cantidad-licencias-asignadas');
  }
}
