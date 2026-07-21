using AcgFotos.Core.Application;
using AcgFotos.Fotos.Domain.Entities;

namespace AcgFotos.Fotos.Application.Criterias;

public class PedidoCriteria : ListaPaginadaCriteriaBase
{
    /// <summary>0 = todos los eventos (mismo criterio que <see cref="GrupoCriteria.EventoId"/>).</summary>
    public long EventoId { get; set; }

    /// <summary>Null = todos los estados.</summary>
    public EstadoPedido? Estado { get; set; }
}
