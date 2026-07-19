import { Signal, effect, signal } from '@angular/core';

/**
 * Deriva una `object URL` (`URL.createObjectURL`) de un blob reactivo, con `revoke` automático al
 * cambiar de blob y al destruir el componente — la parte que se repetía igual entre `tbi-foto-img`
 * (admin) y `tbi-foto-familia-img` (familia): ambos bajan un blob autenticado (el endpoint de
 * imagen exige bearer, no sirve un `<img src>` directo) y lo muestran así. Lo que NO se comparte
 * (marca de agua, fricción anti-copia, distinto servicio/sesión por auth) se queda en cada
 * componente — solo este último tramo, el ciclo de vida de la URL, era código idéntico.
 *
 * Debe llamarse en contexto de inyección (constructor o inicializador de campo de un componente).
 */
export function blobObjectUrl(blob: () => Blob | null | undefined): Signal<string | null> {
  const url = signal<string | null>(null);

  effect((onCleanup) => {
    const b = blob();
    const objectUrl = b ? URL.createObjectURL(b) : null;
    url.set(objectUrl);
    onCleanup(() => {
      if (objectUrl) {
        URL.revokeObjectURL(objectUrl);
      }
    });
  });

  return url.asReadonly();
}
