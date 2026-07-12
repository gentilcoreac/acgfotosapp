using System.Collections.Generic;

namespace AcgFotos.Core.Application {
    public class PaginationSet<T> {
        public int Page { get; set; }

        public int Count => this.Items?.Count ?? 0;

        public int TotalPages { get; set; }
        public int TotalCount { get; set; }

        public IReadOnlyList<T> Items { get; set; }
    }
}
