import { Injectable, effect, inject } from '@angular/core';
import { Observable, catchError, map, shareReplay, tap, throwError } from 'rxjs';
import { ApiClient } from '../http';
import { FamiliaSessionStore } from './familia-session.store';
import { FotoFamilia } from './models/foto-familia.model';

/** Derivados con watermark que sirve la galería de familia (`{id}/thumb` para la grilla, `{id}/preview` ampliado). */
export type VarianteDerivadoFamilia = 'thumb' | 'preview';

/**
 * Techo de memoria del caché de `derivado()`, no de cantidad de fotos: así un álbum con muchas
 * miniaturas chicas cachea de sobra, y unas pocas fotos "preview" más pesadas no lo desbordan. Pensado
 * para no ahogar un celular gama baja (mobile-first, ver `frontend/CLAUDE.md`).
 */
const CACHE_BYTES_MAX = 60 * 1024 * 1024;

/**
 * Galería de la sesión de familia (`api/fotos/familia/fotos`, ADR-11): el alcance sale del JWT de
 * `FamiliaSessionStore` (el `authInterceptor` le agrega ese bearer, no el de plataforma) — acá no se
 * manda ningún filtro, el back decide qué fotos son visibles.
 */
@Injectable({ providedIn: 'root' })
export class FamiliaGaleriaService {
  private readonly api = inject(ApiClient);
  private readonly sessionStore = inject(FamiliaSessionStore);

  /** Observable ya resuelto por `fotoId:variante` (shareReplay), evita repedir la misma foto al
   * recorrer el carrusel. Map en orden de inserción = LRU (se reinserta la clave en cada hit). */
  private readonly derivadoCache = new Map<string, Observable<Blob>>();
  private readonly derivadoBytes = new Map<string, number>();
  private cacheBytesTotal = 0;
  private lastToken = this.sessionStore.token();

  constructor() {
    // Dispositivo compartido (ADR-11): al canjear un código nuevo o cerrar la sesión, no arrastrar
    // en memoria los blobs de la familia anterior.
    effect(() => {
      const token = this.sessionStore.token();
      if (token !== this.lastToken) {
        this.limpiarCache();
      }
      this.lastToken = token;
    });
  }

  listar(): Observable<FotoFamilia[]> {
    return this.api.get<FotoFamilia[]>('fotos/familia/fotos');
  }

  /**
   * Bytes de un derivado con watermark. Igual que en la galería admin, el endpoint requiere bearer:
   * no sirve un `<img src>` directo, hay que bajar el blob (`tbi-foto-familia-img`).
   */
  derivado(fotoId: number, variante: VarianteDerivadoFamilia): Observable<Blob> {
    const key = `${fotoId}:${variante}`;
    const cached = this.derivadoCache.get(key);
    if (cached) {
      this.derivadoCache.delete(key);
      this.derivadoCache.set(key, cached); // recency para el LRU
      return cached;
    }

    const obs$ = this.api.getBlob(`fotos/familia/fotos/${fotoId}/${variante}`).pipe(
      map((response) => response.body!),
      tap((blob) => this.registrarPeso(key, blob.size)),
      catchError((err: unknown) => {
        this.derivadoCache.delete(key); // no cachear un fallo: que la próxima vuelva a intentar
        return throwError(() => err);
      }),
      shareReplay(1),
    );
    this.derivadoCache.set(key, obs$);
    return obs$;
  }

  private registrarPeso(key: string, bytes: number): void {
    this.derivadoBytes.set(key, bytes);
    this.cacheBytesTotal += bytes;

    while (this.cacheBytesTotal > CACHE_BYTES_MAX && this.derivadoCache.size > 1) {
      const oldestKey: string | undefined = this.derivadoCache.keys().next().value;
      if (oldestKey === undefined || oldestKey === key) {
        break;
      }
      this.derivadoCache.delete(oldestKey);
      this.cacheBytesTotal -= this.derivadoBytes.get(oldestKey) ?? 0;
      this.derivadoBytes.delete(oldestKey);
    }
  }

  private limpiarCache(): void {
    this.derivadoCache.clear();
    this.derivadoBytes.clear();
    this.cacheBytesTotal = 0;
  }
}
