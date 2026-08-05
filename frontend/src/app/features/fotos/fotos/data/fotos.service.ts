import { HttpResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { ApiClient } from '../../../../core/http';
import { QueryParams } from '../../../../core/models/query-params.model';
import { Foto } from '../domain/foto.model';

/**
 * Derivados con watermark que sirve la API (`{id}/thumb` para la grilla, `{id}/preview` ampliado) +
 * `original` (solo admin, sin watermark — mismo endpoint que `descargarOriginal`, con la misma ruta
 * `{id}/{variante}`: se puede pedir como blob para RENDERIZAR inline, no solo para descargar).
 */
export type VarianteDerivado = 'thumb' | 'preview' | 'original';

/**
 * Fotos del vertical (`api/fotos/fotos`). No es un CRUD Extended: la foto se sube (multipart) y se
 * procesa en background (`Pendiente → Lista/Error`); el listado es por grupo (opcionalmente por
 * participante) y sin paginar.
 */
@Injectable({ providedIn: 'root' })
export class FotosService {
  private readonly api = inject(ApiClient);

  listar(grupoId: number, participanteId?: number | null): Observable<Foto[]> {
    const query: QueryParams = { grupoId };
    if (participanteId != null) {
      query['participanteId'] = participanteId;
    }
    return this.api.get<Foto[]>('fotos/fotos', query);
  }

  /**
   * Sube una tanda de archivos al grupo (grupales) o a un participante puntual. La respuesta trae las
   * fotos creadas en `Pendiente`; el estado final se consulta con `listar`. El tandeo (no mandar
   * cientos de archivos en un request) es responsabilidad del caller.
   */
  subir(grupoId: number, participanteId: number | null, archivos: File[]): Observable<Foto[]> {
    const formData = new FormData();
    formData.append('grupoId', String(grupoId));
    if (participanteId != null) {
      formData.append('participanteId', String(participanteId));
    }
    for (const archivo of archivos) {
      formData.append('archivos', archivo, archivo.name);
    }
    return this.api.postMultipart<Foto[]>('fotos/fotos/upload', formData);
  }

  /**
   * Bytes de un derivado con watermark. Los endpoints de imagen requieren el bearer, así que un
   * `<img src>` directo no sirve: se baja el blob y el caller arma un object URL (`tbi-foto-img`).
   * 404 mientras la foto no esté Lista.
   */
  derivado(fotoId: number, variante: VarianteDerivado): Observable<Blob> {
    return this.api
      .getBlob(`fotos/fotos/${fotoId}/${variante}`)
      .pipe(map((response) => response.body!));
  }

  /** El ORIGINAL limpio para imprimir (solo admin). El nombre viaja en `Content-Disposition`. */
  descargarOriginal(fotoId: number): Observable<HttpResponse<Blob>> {
    return this.api.getBlob(`fotos/fotos/${fotoId}/original`);
  }

  /** Borra la foto completa (fila + original + derivados en el storage). */
  borrar(fotoId: number): Observable<void> {
    return this.api.delete<void>(`fotos/fotos/${fotoId}`);
  }

  /** Cuántas fotos del evento se van a reprocesar (para el diálogo de confirmación, ANTES de encolar). */
  contarParaRegenerar(eventoId: number): Observable<number> {
    return this.api.get<number>('fotos/fotos/regenerar/conteo', { eventoId });
  }

  /** Marca `Pendiente` y encola esas fotos (D9: nunca se dispara solo — el fotógrafo lo pide con el conteo a la vista). */
  regenerar(eventoId: number): Observable<number> {
    return this.api.post<number>(`fotos/fotos/regenerar?eventoId=${eventoId}`);
  }
}
