using AcgFotos.Core.Application;

namespace AcgFotos.Fotos.Application.Criterias;

/// <summary>Criteria del listado de cursos: se navega por evento (0 = todos los del tenant).</summary>
public class CursoCriteria : ListaPaginadaCriteriaBase
{
    public long EventoId { get; set; }
}
