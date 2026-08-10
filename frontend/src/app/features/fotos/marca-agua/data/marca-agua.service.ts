import { Injectable, inject } from '@angular/core';
import { Observable, forkJoin, from, map, of, shareReplay, switchMap } from 'rxjs';
import { ApiClient, injectCrudClient } from '../../../../core/http';
import { CapaMarcaAguaSubida, PerfilMarcaAgua } from '../domain/marca-agua.model';

/**
 * Perfiles de marca de agua (`api/fotos/marca-agua/perfiles`, ADR-15). El CRUD base cubre
 * metadata/colocación de capas ya existentes; el alta/reemplazo de contenido de una capa va por
 * `subirCapa` (multipart, design.md D14: sin `perfilId` crea el perfil en el mismo paso).
 */
@Injectable({ providedIn: 'root' })
export class MarcaAguaService {
  private readonly api = inject(ApiClient);
  readonly crud = injectCrudClient<PerfilMarcaAgua>('marca-agua/perfiles', 'fotos');

  subirCapa(
    perfilId: number | null,
    nombrePerfilSiNuevo: string | null,
    archivo: File,
  ): Observable<CapaMarcaAguaSubida> {
    const formData = new FormData();
    if (perfilId != null) {
      formData.append('perfilMarcaAguaId', String(perfilId));
    }
    if (nombrePerfilSiNuevo) {
      formData.append('nombrePerfilSiNuevo', nombrePerfilSiNuevo);
    }
    formData.append('archivo', archivo, archivo.name);
    return this.api.postMultipart<CapaMarcaAguaSubida>('fotos/marca-agua/perfiles/capas/upload', formData);
  }

  /** Bytes del PNG de una capa ya subida (lectura autenticada, para renderizar en canvas). */
  asset(perfilId: number, storageKey: string): Observable<Blob> {
    return this.api
      .getBlob(`fotos/marca-agua/perfiles/${perfilId}/capas/${storageKey}`)
      .pipe(map((response) => response.body!));
  }

  // El PNG de una capa es inmutable (cambiar el contenido = otra capa con otro storageKey), así que
  // una vez decodificado se reusa. Sin esto, las cinco vistas previas del editor bajan el mismo
  // archivo cinco veces y el tope global de pedidos salta a los pocos segundos de uso.
  private readonly assetsDecodificados = new Map<string, Observable<ImageBitmap>>();

  private assetDecodificado(perfilId: number, storageKey: string): Observable<ImageBitmap> {
    const clave = `${perfilId}:${storageKey}`;
    let cacheado = this.assetsDecodificados.get(clave);
    if (!cacheado) {
      cacheado = this.asset(perfilId, storageKey).pipe(
        switchMap((blob) => from(createImageBitmap(blob))),
        shareReplay({ bufferSize: 1, refCount: false }),
      );
      this.assetsDecodificados.set(clave, cacheado);
    }
    return cacheado;
  }

  /**
   * Imágenes de las capas del perfil, indexadas por `storageKey`. Devuelve sólo las imágenes y no
   * `CapaComposicion` armada porque la colocación cambia con cada ajuste del editor y las imágenes
   * no: quien renderiza combina estas imágenes con la colocación del momento.
   */
  cargarAssets(perfil: PerfilMarcaAgua): Observable<Map<string, ImageBitmap>> {
    if (perfil.id == null || perfil.capas.length === 0) {
      return of(new Map());
    }
    const perfilId = perfil.id;
    return forkJoin(
      perfil.capas.map((capa) =>
        this.assetDecodificado(perfilId, capa.storageKey).pipe(
          map((imagen) => [capa.storageKey, imagen] as const),
        ),
      ),
    ).pipe(map((pares) => new Map(pares)));
  }
}
