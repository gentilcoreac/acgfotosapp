import { Injectable, inject } from '@angular/core';
import { Observable, forkJoin, from, map, of, switchMap } from 'rxjs';
import { ApiClient, injectCrudClient } from '../../../../core/http';
import { CapaComposicion } from '../domain/marca-agua-canvas.util';
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

  /** Baja y decodifica los assets de todas las capas del perfil, listas para `componerMarcaAgua`. */
  cargarComposicion(perfil: PerfilMarcaAgua): Observable<CapaComposicion[]> {
    if (perfil.id == null || perfil.capas.length === 0) {
      return of([]);
    }
    const perfilId = perfil.id;
    return forkJoin(
      perfil.capas.map((capa) =>
        this.asset(perfilId, capa.storageKey).pipe(
          switchMap((blob) => from(createImageBitmap(blob))),
          map((imagen): CapaComposicion => ({ capa, imagen })),
        ),
      ),
    );
  }
}
