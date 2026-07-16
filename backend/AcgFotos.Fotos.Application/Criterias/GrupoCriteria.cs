using AcgFotos.Core.Application;

namespace AcgFotos.Fotos.Application.Criterias;

/// <summary>Criteria del listado de grupos: se navega por evento (0 = todos los del tenant).</summary>
public class GrupoCriteria : ListaPaginadaCriteriaBase
{
    public long EventoId { get; set; }
}
