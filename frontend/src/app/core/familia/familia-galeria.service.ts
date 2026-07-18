import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { ApiClient } from '../http';
import { FotoFamilia } from './models/foto-familia.model';

/** Derivados con watermark que sirve la galería de familia (`{id}/thumb` para la grilla, `{id}/preview` ampliado). */
export type VarianteDerivadoFamilia = 'thumb' | 'preview';

/**
 * Galería de la sesión de familia (`api/fotos/familia/fotos`, ADR-11): el alcance sale del JWT de
 * `FamiliaSessionStore` (el `authInterceptor` le agrega ese bearer, no el de plataforma) — acá no se
 * manda ningún filtro, el back decide qué fotos son visibles.
 */
@Injectable({ providedIn: 'root' })
export class FamiliaGaleriaService {
  private readonly api = inject(ApiClient);

  listar(): Observable<FotoFamilia[]> {
    return this.api.get<FotoFamilia[]>('fotos/familia/fotos');
  }

  /**
   * Bytes de un derivado con watermark. Igual que en la galería admin, el endpoint requiere bearer:
   * no sirve un `<img src>` directo, hay que bajar el blob (`tbi-foto-familia-img`).
   */
  derivado(fotoId: number, variante: VarianteDerivadoFamilia): Observable<Blob> {
    return this.api
      .getBlob(`fotos/familia/fotos/${fotoId}/${variante}`)
      .pipe(map((response) => response.body!));
  }
}
