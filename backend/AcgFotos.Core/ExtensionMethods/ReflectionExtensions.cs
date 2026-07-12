using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace AcgFotos.Core.ExtensionMethods {
    public static class ReflectionExtensions {
        public static bool EsNuleable(this PropertyInfo pi) {
            return (pi.PropertyType.IsGenericType && pi.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>));
        }
        public static bool IsPrimitiveEx(this PropertyInfo pi) {
                    if (pi.PropertyType.IsPrimitive ||
                        pi.PropertyType == typeof(string) ||
                        pi.PropertyType == typeof(String) ||
                        pi.PropertyType == typeof(short) ||
                        pi.PropertyType == typeof(short?) ||
                        pi.PropertyType == typeof(long) ||
                        pi.PropertyType == typeof(long?) ||
                        pi.PropertyType == typeof(int) ||
                        pi.PropertyType == typeof(int?) ||
                        pi.PropertyType == typeof(bool) ||
                        pi.PropertyType == typeof(bool?) ||
                        pi.PropertyType == typeof(Boolean) ||
                        pi.PropertyType == typeof(Boolean?) ||
                        pi.PropertyType == typeof(Int16) ||
                        pi.PropertyType == typeof(Int16?) ||
                        pi.PropertyType == typeof(Int32) ||
                        pi.PropertyType == typeof(Int32?) ||
                        pi.PropertyType == typeof(Int64) ||
                        pi.PropertyType == typeof(Int64?) ||
                        pi.PropertyType == typeof(Decimal) ||
                        pi.PropertyType == typeof(Decimal?) ||
                        pi.PropertyType == typeof(double) ||
                        pi.PropertyType == typeof(double?) ||
                        pi.PropertyType == typeof(decimal) ||
                        pi.PropertyType == typeof(decimal?) ||
                        pi.PropertyType == typeof(DateTime) ||
                        pi.PropertyType == typeof(DateTime?)) {
                return true;
            }
            return false;
        }

        public static IEnumerable<IEnumerable> GetCollections<T>(this object obj)
        {
            if (obj == null) throw new ArgumentNullException("obj");
            var type = obj.GetType();
            var res = new List<IEnumerable>();
            foreach (var prop in type.GetProperties())
            {

                if (prop.PropertyType.Name.Contains("ICollection"))
                {
                    var collection = (IEnumerable)prop.GetValue(obj, null);
                    if (collection!= null) 
                    {//Una colección puede estar vacía.
                     //Pero no debe ser null. Si es null es porque no se incluyó para actualizar
                        res.Add(collection); 
                    }
                }
            }
            return res;
        }
    }
}
