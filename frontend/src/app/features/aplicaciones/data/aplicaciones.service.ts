import { Injectable } from '@angular/core';
import { injectCrudClient } from '../../../core/http';
import { Aplicacion } from '../domain/aplicacion.model';

/**
 * Servicio de datos de Aplicaciones. Expone el `CrudClient` (entidad `aplicaciones` del módulo
 * `general`) que consumen la lista y el edit. CRUD estándar; los endpoints extra de la API
 * (`aplicaciones-tenant`, `aplicaciones-permitidas`, etc.) no los usa este ABM.
 */
@Injectable({ providedIn: 'root' })
export class AplicacionesService {
  readonly crud = injectCrudClient<Aplicacion>('aplicaciones');
}
