import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiClient, injectCrudClient } from '../../../../core/http';
import { Curso, TarjetasCurso } from '../domain/curso.model';

/**
 * Servicio de datos de Cursos del vertical Fotos. El CRUD base (`api/fotos/cursos`) cubre todo el
 * ABM: los álbumes viajan como colección hija dentro del detalle/update, y el listado se filtra
 * por evento (`CursoCriteria.EventoId`; 0 = todos los del tenant). El lookup de eventos lo aporta
 * `EventosService` (feature hermana).
 */
@Injectable({ providedIn: 'root' })
export class CursosService {
  private readonly api = inject(ApiClient);

  readonly crud = injectCrudClient<Curso>('cursos', 'fotos');

  /** Tarjetas imprimibles del curso (una por alumno, con código y QR ya generado por la API). */
  getTarjetas(cursoId: number): Observable<TarjetasCurso> {
    return this.api.get<TarjetasCurso>(`fotos/cursos/${cursoId}/tarjetas`);
  }
}
