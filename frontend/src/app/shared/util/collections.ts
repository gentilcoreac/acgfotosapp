/** Devuelve un nuevo `Set` con `value` agregado o quitado según `present` (para signals inmutables). */
export function toggleInSet<T>(set: ReadonlySet<T>, value: T, present: boolean): ReadonlySet<T> {
  const next = new Set(set);
  if (present) {
    next.add(value);
  } else {
    next.delete(value);
  }
  return next;
}
