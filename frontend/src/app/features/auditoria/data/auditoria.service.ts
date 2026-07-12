import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { injectCrudClient } from '../../../core/http';
import { Auditoria } from '../domain/auditoria.model';

/**
 * Servicio de datos de Auditoría (entidad `auditoria` del módulo `general`). Es **solo lectura**:
 * se expone el listado paginado/filtrado (`getAllByCriteria`, vía `tbi-table`) y el detalle por id
 * (trae `parametros`/`resultContent` completos, que el listado omite por peso).
 */
@Injectable({ providedIn: 'root' })
export class AuditoriaService {
  readonly crud = injectCrudClient<Auditoria>('auditoria');

  /** Detalle completo de un registro (incluye parámetros y contenido de respuesta). */
  getById(id: number): Observable<Auditoria> {
    return this.crud.getById(id);
  }
}
