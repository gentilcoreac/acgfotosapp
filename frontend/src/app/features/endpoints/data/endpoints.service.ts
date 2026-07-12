import { Injectable, inject } from '@angular/core';
import { ApiClient, injectCrudClient } from '../../../core/http';
import { Endpoint } from '../domain/endpoint.model';

@Injectable({ providedIn: 'root' })
export class EndpointsService {
  readonly crud = injectCrudClient<Endpoint>('endpoints');

  private readonly api = inject(ApiClient);

  /** Ejecuta el discovery de endpoints en la API (`GET api/general/discover`). Solo root. */
  descubrir() {
    return this.api.get<Endpoint[]>('general/discover');
  }
}
