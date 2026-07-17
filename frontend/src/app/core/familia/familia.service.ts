import { Injectable, inject } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { ApiClient } from '../http';
import { FamiliaSessionStore } from './familia-session.store';
import { CanjeResult } from './models/canje-result.model';

/** Canje de código de acceso (ADR-02): sin registro, endpoint público (`fotos/canje`). */
@Injectable({ providedIn: 'root' })
export class FamiliaService {
  private readonly api = inject(ApiClient);
  private readonly store = inject(FamiliaSessionStore);

  /** Canjea el código y, si es válido, deja la sesión de familia guardada. */
  canjear(codigo: string): Observable<CanjeResult> {
    return this.api
      .post<CanjeResult, { codigo: string }>('fotos/canje', { codigo })
      .pipe(tap((result) => this.store.setSession(result)));
  }
}
