import { Injectable } from '@angular/core';
import { injectCrudClient } from '../../../../core/http';
import { Curso } from '../domain/curso.model';

/**
 * Servicio de datos de Cursos del vertical Fotos. El CRUD base (`api/fotos/cursos`) cubre todo el
 * ABM: los álbumes viajan como colección hija dentro del detalle/update, y el listado se filtra
 * por evento (`CursoCriteria.EventoId`; 0 = todos los del tenant). El lookup de eventos lo aporta
 * `EventosService` (feature hermana).
 */
@Injectable({ providedIn: 'root' })
export class CursosService {
  readonly crud = injectCrudClient<Curso>('cursos', 'fotos');
}
