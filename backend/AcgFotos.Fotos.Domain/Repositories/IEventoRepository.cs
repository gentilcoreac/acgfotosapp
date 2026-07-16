using AcgFotos.Core.Application;
using AcgFotos.Core.Data;
using AcgFotos.Fotos.Domain.Entities;

namespace AcgFotos.Fotos.Domain.Repositories;

public interface IEventoRepository : IEntityBaseRepository<Evento>
{
    /// <summary>Listado del ABM: filtra por SearchText (nombre/lugarOrganizacion) y pagina.</summary>
    Task<PaginationSet<Evento>> PaginateHeadersAsync(ListaPaginadaCriteriaBase criteria);

    /// <summary>Detalle para edición (tracked): el evento con su catálogo de tamaños.</summary>
    Task<Evento?> GetByIdWithTamanosAsync(long id);

    /// <summary>Detalle read-only para GetById.</summary>
    Task<Evento?> GetByIdWithTamanosReadOnlyAsync(long id);
}
