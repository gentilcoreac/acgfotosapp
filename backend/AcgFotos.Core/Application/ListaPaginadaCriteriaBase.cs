namespace AcgFotos.Core.Application {

    public class ListaPaginadaCriteriaBase : IListaPaginadaCriteriaBase {

        public int Page { get; set; }
        public int PageSize { get; set; }
        public string SearchText { get; set; }
        public string OrderBy { get; set; }
        public bool DescendingOrder { get; set; }

        public ListaPaginadaCriteriaBase() {
            this.Page = 0;
            this.PageSize = 50;
        }
    }
}
