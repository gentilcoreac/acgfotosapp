using System;
using System.Collections.Generic;
using System.Linq;

namespace AcgFotos.Core.Application
{
    /// <summary>
    /// Sincronización tipada de colecciones hijas (joins N:N o detalle) contra el conjunto de
    /// claves deseado: quita las que sobran, agrega las que faltan, deja intactas las que ya
    /// están. EF resuelve INSERT/DELETE solo de las filas afectadas — sin churn ni orphans.
    /// Sin reflexión, sin AutoMapper. Pensado para usar dentro de los overrides de
    /// <c>SyncCollections</c> en los AppServices.
    /// </summary>
    public static class ChildCollectionSync
    {
        /// <summary>
        /// Reconcilia <paramref name="current"/> para que contenga exactamente los elementos cuyas
        /// claves están en <paramref name="desiredKeys"/>.
        /// </summary>
        /// <typeparam name="TChild">Tipo de la entidad hija (la fila join, ej. RolPermiso).</typeparam>
        /// <typeparam name="TKey">Tipo de la clave de negocio del hijo (ej. PermisoId).</typeparam>
        /// <param name="current">Colección hija de la entidad tracked.</param>
        /// <param name="desiredKeys">Claves que se deben dejar (las del DTO).</param>
        /// <param name="keyOf">Cómo obtener la clave de una fila existente.</param>
        /// <param name="create">Cómo construir una fila nueva a partir de una clave.</param>
        public static void SyncBy<TChild, TKey>(
            ICollection<TChild> current,
            IEnumerable<TKey> desiredKeys,
            Func<TChild, TKey> keyOf,
            Func<TKey, TChild> create)
        {
            var desired = desiredKeys != null
                ? new HashSet<TKey>(desiredKeys)
                : new HashSet<TKey>();

            foreach (var item in current.Where(c => !desired.Contains(keyOf(c))).ToList())
            {
                current.Remove(item);
            }

            var existing = new HashSet<TKey>(current.Select(keyOf));
            foreach (var key in desired.Where(k => !existing.Contains(k)))
            {
                current.Add(create(key));
            }
        }
    }
}
