import { MatPaginatorIntl } from '@angular/material/paginator';

/**
 * Textos del `mat-paginator` en español. Material los trae en inglés por default y no son claves
 * propias de la app, por eso no entran en el diferimiento de i18n (Fase 4): sin esto, las tablas
 * mezclan "Items per page" en una UI 100% en español.
 */
export function paginatorIntlEs(): MatPaginatorIntl {
  const intl = new MatPaginatorIntl();
  intl.itemsPerPageLabel = 'Filas por página';
  intl.nextPageLabel = 'Página siguiente';
  intl.previousPageLabel = 'Página anterior';
  intl.firstPageLabel = 'Primera página';
  intl.lastPageLabel = 'Última página';
  intl.getRangeLabel = (page, pageSize, length) => {
    if (length === 0 || pageSize === 0) {
      return `0 de ${length}`;
    }
    const start = page * pageSize;
    const end = Math.min(start + pageSize, length);
    return `${start + 1}–${end} de ${length}`;
  };
  return intl;
}
