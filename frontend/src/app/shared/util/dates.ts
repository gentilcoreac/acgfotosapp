/**
 * Helpers de fecha para `<input type="date">` (formato `yyyy-MM-dd`), siempre en hora LOCAL.
 * Ojo: `new Date().toISOString()` es UTC — en AR (UTC-3) desde las 21:00 devuelve "mañana",
 * por eso acá se formatea con los getters locales.
 */

const pad = (n: number): string => String(n).padStart(2, '0');

const toDateOnly = (d: Date): string =>
  `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`;

/** Hoy en fecha local, `yyyy-MM-dd`. */
export function todayLocalIso(): string {
  return toDateOnly(new Date());
}

/** Recorta un ISO datetime de la API a `yyyy-MM-dd` para un input date ('' si viene vacío). */
export function toDateInput(iso?: string): string {
  return iso ? iso.slice(0, 10) : '';
}

/** Suma un año a una fecha `yyyy-MM-dd` operando en local (29/02 → 01/03 del año siguiente). */
export function plusOneYear(iso: string): string {
  if (!iso) {
    return toDateOnly(
      new Date(new Date().getFullYear() + 1, new Date().getMonth(), new Date().getDate()),
    );
  }
  const [year, month, day] = iso.slice(0, 10).split('-').map(Number);
  return toDateOnly(new Date(year + 1, month - 1, day));
}
