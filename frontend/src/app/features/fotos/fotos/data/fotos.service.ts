import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiClient } from '../../../../core/http';
import { QueryParams } from '../../../../core/models/query-params.model';
import { Foto } from '../domain/foto.model';

/**
 * Fotos del vertical (`api/fotos/fotos`). No es un CRUD Extended: la foto se sube (multipart) y se
 * procesa en background (`Pendiente → Lista/Error`); el listado es por curso (opcionalmente por
 * álbum) y sin paginar.
 */
@Injectable({ providedIn: 'root' })
export class FotosService {
  private readonly api = inject(ApiClient);

  listar(cursoId: number, albumId?: number | null): Observable<Foto[]> {
    const query: QueryParams = { cursoId };
    if (albumId != null) {
      query['albumId'] = albumId;
    }
    return this.api.get<Foto[]>('fotos/fotos', query);
  }

  /**
   * Sube una tanda de archivos al curso (grupales) o a un álbum puntual. La respuesta trae las
   * fotos creadas en `Pendiente`; el estado final se consulta con `listar`. El tandeo (no mandar
   * cientos de archivos en un request) es responsabilidad del caller.
   */
  subir(cursoId: number, albumId: number | null, archivos: File[]): Observable<Foto[]> {
    const formData = new FormData();
    formData.append('cursoId', String(cursoId));
    if (albumId != null) {
      formData.append('albumId', String(albumId));
    }
    for (const archivo of archivos) {
      formData.append('archivos', archivo, archivo.name);
    }
    return this.api.postMultipart<Foto[]>('fotos/fotos/upload', formData);
  }
}
