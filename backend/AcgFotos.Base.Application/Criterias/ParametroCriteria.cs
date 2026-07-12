using AcgFotos.Core.Application;

namespace AcgFotos.Base.Application.Criterias
{
    public class ParametroCriteria : ListaPaginadaCriteriaBase
    {
        public long? AplicacionId { get; set; }
        public bool? Estado { get; set; }
    }
}
