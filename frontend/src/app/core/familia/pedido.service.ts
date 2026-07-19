import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiClient } from '../http';
import { PedidoConfirmado, PedidoConfirmarInput } from './models/pedido.model';

/** Confirmación de pedido de la sesión de familia (`POST fotos/familia/pedidos`, ADR-07). */
@Injectable({ providedIn: 'root' })
export class PedidoService {
  private readonly api = inject(ApiClient);

  confirmar(input: PedidoConfirmarInput): Observable<PedidoConfirmado> {
    return this.api.post<PedidoConfirmado, PedidoConfirmarInput>('fotos/familia/pedidos', input);
  }
}
