using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.EntityFrameworkCore;

namespace AcgFotos.Core.ExtensionMethods {
    public static class EntityExtensionMethods {

        public static void CopyToOnlyPrimitiveValues(this object source, object target) {
            if (source != null && target != null) {
                foreach (PropertyInfo pi in source.GetType().GetProperties()) {
                    if (pi.PropertyType.IsPrimitive ||
                        pi.PropertyType == typeof(string) ||
                        pi.PropertyType == typeof(int) ||
                        pi.PropertyType == typeof(String) ||
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
                        pi.PropertyType == typeof(DateTime) ||
                        pi.PropertyType == typeof(DateTime?)) {
                        object valueToCopy = pi.GetValue(source, null);

                        MethodInfo setMethod = pi.GetSetMethod();

                        if (setMethod != null) {
                            pi.SetValue(target, valueToCopy, null);
                        }
                    }
                }
            }
        }

        public static List<string> GetEntityRelationshipsWithAnyItem<T>(this T entity)
        {
            var existingCollections = new List<string>();

            foreach (var collection in entity.GetCollections<T>())
            {
                if (collection.AsQueryable().Any())
                {
                    var collectionName = collection.AsQueryable().ElementType.Name;

                    existingCollections.Add(collectionName);
                }
            }

            return existingCollections;
        }

        public static IQueryable<T> IncludeAllCollections<T>(this IQueryable<T> query) where T : class
        {
            var entityType = typeof(T);
            var collectionProperties = entityType.GetProperties()
                .Where(p => p.PropertyType.IsGenericType &&
                            p.PropertyType.GetGenericTypeDefinition() == typeof(ICollection<>))
                .ToList();

            foreach (var property in collectionProperties)
            {
                query = query.Include(property.Name);
            }

            return query;
        }
    }
}
