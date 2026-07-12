using AcgFotos.Core.Application;
using AcgFotos.Core.Data;
using AcgFotos.Fotos.Domain.Entities;

namespace AcgFotos.Fotos.Domain.Repositories;

public interface IFotoRepository : IEntityBaseRepository<Foto>
{
    /// <summary>El curso del upload, dentro del tenant (define el EventoId de las fotos).</summary>
    Task<Curso?> GetCursoAsync(long cursoId);

    /// <summary>El álbum destino existe y pertenece al curso (guard del upload).</summary>
    Task<bool> AlbumPerteneceAlCursoAsync(long albumId, long cursoId);

    /// <summary>Foto tracked para que el worker actualice estado/dimensiones.</summary>
    Task<Foto?> GetByIdTrackedAsync(long id);

    /// <summary>Listado admin: fotos del curso, opcionalmente de un álbum puntual.</summary>
    Task<List<Foto>> ListarAsync(long cursoId, long? albumId);

    /// <summary>
    /// Fotos en Pendiente de TODOS los tenants (IgnoreQueryFilters): SOLO para el barrido de
    /// arranque del worker, que re-encola lo que quedó a medias si el proceso se reinició.
    /// </summary>
    Task<List<Foto>> GetPendientesTodosLosTenantsAsync();
}
