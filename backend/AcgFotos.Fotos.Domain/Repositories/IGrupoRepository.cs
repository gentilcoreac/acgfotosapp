using AcgFotos.Core.Application;
using AcgFotos.Core.Data;
using AcgFotos.Fotos.Domain.Entities;

namespace AcgFotos.Fotos.Domain.Repositories;

public interface IGrupoRepository : IEntityBaseRepository<Grupo>
{
    /// <summary>Listado del ABM: filtra por evento (0 = todos) y SearchText (nombre), y pagina.</summary>
    Task<PaginationSet<Grupo>> PaginateHeadersAsync(ListaPaginadaCriteriaBase criteria, long eventoId);

    /// <summary>Detalle para edición (tracked): el grupo con sus participantes y códigos de acceso.</summary>
    Task<Grupo?> GetByIdWithParticipantesAsync(long id);

    /// <summary>Detalle read-only para GetById.</summary>
    Task<Grupo?> GetByIdWithParticipantesReadOnlyAsync(long id);

    /// <summary>GetAll con los participantes cargados (el header expone la cantidad).</summary>
    Task<List<Grupo>> GetAllWithParticipantesReadOnlyAsync();

    /// <summary>Para las tarjetas: grupo + evento + participantes con sus códigos (read-only).</summary>
    Task<Grupo?> GetByIdParaTarjetasAsync(long id);

    /// <summary>El evento existe para el tenant actual (guard: el EventoId viene del input).</summary>
    Task<bool> ExisteEventoAsync(long eventoId);

    /// <summary>Ids de participantes del grupo que tienen fotos (no pueden darse de baja sin borrarlas).</summary>
    Task<List<long>> GetParticipanteIdsConFotosAsync(long grupoId);

    /// <summary>El grupo tiene fotos (propias de participantes o grupales): bloquea el delete.</summary>
    Task<bool> TieneFotosAsync(long grupoId);
}
