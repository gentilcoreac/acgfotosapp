using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace AcgFotos.Core.ObjectComparer
{
    public static class ObjectComparer
    {
        public static bool ObjectsAreEqual<T>(T obj1, T obj2)
        {
            if (obj1 == null && obj2 == null)
                return true;

            if (obj1 == null || obj2 == null)
                return false;

            Type objectType = typeof(T);
            PropertyInfo[] properties = objectType.GetProperties();

            foreach (var property in properties)
            {
                // Valores de las propiedades
                var value1 = property.GetValue(obj1);
                var value2 = property.GetValue(obj2);

                // Si la propiedad es una colección, realiza una comparación especial.
                if (IsCollection(property.PropertyType) && property.PropertyType != typeof(string))
                {
                    // Hace un cast de los valores a IEnumerable<object> para poder compararlos.
                    var enumerableVal1 = value1 as IEnumerable<object>;
                    var enumerableVal2 = value2 as IEnumerable<object>;

                    if(enumerableVal1.Count() > 0 && enumerableVal2.Count() > 0)
                    {
                        if (!CollectionsEquals(enumerableVal1, enumerableVal2))
                            return false;
                    }
                }
                else
                {
                    if (!Equals(value1, value2))
                    {
                        // Se encontró una diferencia en al menos una propiedad.
                        return false;
                    }
                }
            }

            // No se encontraron diferencias en ninguna propiedad.
            return true;
        }

        private static bool IsCollection(Type type)
        {
            return type != null && typeof(System.Collections.IEnumerable).IsAssignableFrom(type);
        }

        private static bool CollectionsEquals<T>(this IEnumerable<T> obj, IEnumerable<T> another)
        {
            if (ReferenceEquals(obj, another)) return true;
            if ((obj == null) || (another == null)) return false;
            bool result = true;

            using (IEnumerator<T> enumerator1 = obj.GetEnumerator())
            using (IEnumerator<T> enumerator2 = another.GetEnumerator())
            {
                while (true)
                {
                    bool hasNext1 = enumerator1.MoveNext();
                    bool hasNext2 = enumerator2.MoveNext();

                    if (hasNext1 != hasNext2 || !ObjectsAreEqual(enumerator1.Current, enumerator2.Current) && hasNext1 != false && hasNext2 != false)
                    {
                        result = false;
                        break;
                    }

                    if (!hasNext1) break;
                }
            }

            return result;
        }
    }
}
