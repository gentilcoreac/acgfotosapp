import { Injectable } from '@angular/core';
import { injectCrudClient } from '../../../../core/http';
import { Evento } from '../domain/evento.model';

/**
 * Servicio de datos de Eventos del vertical Fotos. El CRUD base (`api/fotos/eventos`) cubre todo
 * el ABM: el catálogo de tamaños viaja como colección hija dentro del detalle/update.
 */
@Injectable({ providedIn: 'root' })
export class EventosService {
  readonly crud = injectCrudClient<Evento>('eventos', 'fotos');
}