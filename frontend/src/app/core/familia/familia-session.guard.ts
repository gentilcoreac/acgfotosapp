import { inject } from '@angular/core';
import { CanMatchFn, Router } from '@angular/router';
import { FamiliaSessionStore } from './familia-session.store';

/** Permite la ruta solo con sesión de familia activa (canje vigente); si no, manda a `/canje`. */
export const familiaSessionGuard: CanMatchFn = () => {
  const store = inject(FamiliaSessionStore);
  const router = inject(Router);
  return store.isActive() ? true : router.createUrlTree(['/canje']);
};
