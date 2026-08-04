import { Injectable } from '@angular/core';
import { injectCrudClient } from '../../../../core/http';
import { OpcionesPublicacion } from '../domain/opciones-publicacion.model';

/** CRUD de opciones de publicación (`api/fotos/marca-agua/opciones-publicacion`, ADR-15 §5). */
@Injectable({ providedIn: 'root' })
export class OpcionesPublicacionService {
  readonly crud = injectCrudClient<OpcionesPublicacion>('marca-agua/opciones-publicacion', 'fotos');
}
