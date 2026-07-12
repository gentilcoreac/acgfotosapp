using AcgFotos.Core.Application;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AcgFotos.Core.ExtensionMethods
{
    public static class ListExtentions
    {
        public static PaginationSet<T> ToPaginationList<T>(this IEnumerable<T> list, int currentPage, int totalItems, int currentPageSize)
        {
            return new PaginationSet<T>()
            {
                Page = currentPage,
                TotalCount = totalItems,
                TotalPages = (int)Math.Ceiling((decimal)totalItems / currentPageSize),
                Items = list as IReadOnlyList<T> ?? list.ToList()
            };
        }

        public static IEnumerable<T> Paginar<T>(this IEnumerable<T> query, int Page, int PageSize)  //TODO: Revisar estas referencias para que sean iqueryable y usar el metodo de abajo
        {
            if (PageSize > 0)
            {
                query = query
                    .Skip(Page * PageSize)
                    .Take(PageSize);
            }
            return query;
        }
        public static IEnumerable<T> Paginar<T>(this IQueryable<T> query, int page, int pageSize)
        {
            if (pageSize > 0)
            {
                query = (IQueryable<T>)query
                    .Skip(page * pageSize)
                    .Take(pageSize);
            }
            return query.ToList();
        }

        public static IEnumerable<IEnumerable<T>> SplitList<T>(this IEnumerable<T> list, int cantidadPorLista)
        {
            if (!list.Any()) return Enumerable.Empty<IEnumerable<T>>();

            var toreturn = new List<IEnumerable<T>>();

            var cantidadDeListas = Math.Ceiling(decimal.Divide(list.Count(), cantidadPorLista));

            /*itera por la cantidad de listas que tengo que generar*/
            for (var i = 0; i <= (cantidadDeListas - 1); i++)
            {
                if (i == (cantidadDeListas - 1))
                {
                    toreturn.Add(list.Skip(i * cantidadPorLista));
                }
                else
                {
                    toreturn.Add(list.Skip(i * cantidadPorLista).Take(cantidadPorLista));
                }
            }
            return toreturn;
        }      
    }
}
