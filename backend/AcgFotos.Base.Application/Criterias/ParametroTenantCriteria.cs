using AcgFotos.Core.Application;

namespace AcgFotos.Base.Application.Criterias
{
    public class ParametrosTenantCriteria : ListaPaginadaCriteriaBase
    {
        public long AplicacionId { get; set; }
        public long TenantId { get; set; }
    }
}
