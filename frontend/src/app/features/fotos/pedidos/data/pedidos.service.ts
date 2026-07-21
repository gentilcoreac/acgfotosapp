import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiClient, injectCrudClient } from '../../../../core/http';
import { EstadoPedido, Pedido } from '../domain/pedido.model';

/**
 * Servicio de datos de Pedidos (admin, Fase 2). El CRUD base (`api/fotos/pedidos`) cubre
 * listado (`getAllByCriteria`, filtrado por `eventoId`/`estado`) y detalle (`getById`); no hay
 * `save`/`delete` porque el pedido lo confirma la familia (ADR-07). El cambio de estado es un
 * endpoint propio fuera del CRUD.
 */
@Injectable({ providedIn: 'root' })
export class PedidosService {
  private readonly api = inject(ApiClient);

  readonly crud = injectCrudClient<Pedido>('pedidos', 'fotos');

  /** Transición de estado (Pendiente/Pagado → Impreso → Entregado); la API valida el salto. */
  cambiarEstado(id: number, estado: EstadoPedido): Observable<Pedido> {
    return this.api.post<Pedido, { estado: EstadoPedido }>(`fotos/pedidos/${id}/estado`, { estado });
  }
}
