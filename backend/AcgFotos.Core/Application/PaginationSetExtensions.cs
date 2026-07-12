using System;
using System.Collections.Generic;
using System.Linq;

namespace AcgFotos.Core.Application
{
    public static class PaginationSetExtensions
    {
        /// <summary>
        /// Proyecta los Items de un PaginationSet aplicando un selector, preservando Page/TotalCount/TotalPages.
        /// Pensado para mapear PaginationSet&lt;Entity&gt; → PaginationSet&lt;Dto&gt; sin repetir el boilerplate.
        /// </summary>
        public static PaginationSet<TTarget> MapItems<TSource, TTarget>(
            this PaginationSet<TSource> source,
            Func<TSource, TTarget> selector)
        {
            return new PaginationSet<TTarget>
            {
                Page = source.Page,
                TotalCount = source.TotalCount,
                TotalPages = source.TotalPages,
                Items = source.Items.Select(selector).ToList()
            };
        }

        /// <summary>
        /// Devuelve un PaginationSet con los Items reemplazados, preservando Page/TotalCount/TotalPages.
        /// Útil cuando el caller necesita post-procesar los DTOs antes de retornarlos
        /// (ej. enriquecer con datos de queries batch).
        /// </summary>
        public static PaginationSet<TTarget> WithItems<TSource, TTarget>(
            this PaginationSet<TSource> source,
            IReadOnlyList<TTarget> items)
        {
            return new PaginationSet<TTarget>
            {
                Page = source.Page,
                TotalCount = source.TotalCount,
                TotalPages = source.TotalPages,
                Items = items
            };
        }
    }
}
