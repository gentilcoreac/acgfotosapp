import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiClient } from '../http';
import { TamanoPrecio } from './models/tamano-precio.model';

/** Catálogo de tamaños/precios del evento de la sesión de familia (`fotos/familia/tamanos-precios`). */
@Injectable({ providedIn: 'root' })
export class FamiliaCatalogoService {
  private readonly api = inject(ApiClient);

  /** Solo tamaños activos, ya ordenados por el backend. */
  listarTamanosPrecios(): Observable<TamanoPrecio[]> {
    return this.api.get<TamanoPrecio[]>('fotos/familia/tamanos-precios');
  }
}
