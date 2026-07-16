import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiClient, injectCrudClient } from '../../../../core/http';
import { Grupo, TarjetasGrupo } from '../domain/grupo.model';

/**
 * Servicio de datos de Grupos del vertical Fotos. El CRUD base (`api/fotos/grupos`) cubre todo el
 * ABM: los participantes viajan como colección hija dentro del detalle/update, y el listado se filtra
 * por evento (`GrupoCriteria.EventoId`; 0 = todos los del tenant). El lookup de eventos lo aporta
 * `EventosService` (feature hermana).
 */
@Injectable({ providedIn: 'root' })
export class GruposService {
  private readonly api = inject(ApiClient);

  readonly crud = injectCrudClient<Grupo>('grupos', 'fotos');

  /** Tarjetas imprimibles del grupo (una por participante, con código y QR ya generado por la API). */
  getTarjetas(grupoId: number): Observable<TarjetasGrupo> {
    return this.api.get<TarjetasGrupo>(`fotos/grupos/${grupoId}/tarjetas`);
  }
}
