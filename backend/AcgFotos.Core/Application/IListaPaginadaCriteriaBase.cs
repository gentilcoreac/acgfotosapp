namespace AcgFotos.Core.Application {

    public interface IListaPaginadaCriteriaBase {

        int Page { get; set; }

        int PageSize { get; set; }

        string SearchText { get; set; }

        string OrderBy { get; set; }


        bool DescendingOrder { get; set; }
    }
}
