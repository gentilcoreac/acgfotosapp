using AcgFotos.Core.Application;
using AcgFotos.Core.Data;
using AcgFotos.Fotos.Domain.Entities;

namespace AcgFotos.Fotos.Domain.Repositories;

public interface ICursoRepository : IEntityBaseRepository<Curso>
{
    /// <summary>Listado del ABM: filtra por evento (0 = todos) y SearchText (nombre), y pagina.</summary>
    Task<PaginationSet<Curso>> PaginateHeadersAsync(ListaPaginadaCriteriaBase criteria, long eventoId);

    /// <summary>Detalle para edición (tracked): el curso con sus álbumes y códigos de acceso.</summary>
    Task<Curso?> GetByIdWithAlbumesAsync(long id);

    /// <summary>Detalle read-only para GetById.</summary>
    Task<Curso?> GetByIdWithAlbumesReadOnlyAsync(long id);

    /// <summary>GetAll con los álbumes cargados (el header expone la cantidad).</summary>
    Task<List<Curso>> GetAllWithAlbumesReadOnlyAsync();

    /// <summary>El evento existe para el tenant actual (guard: el EventoId viene del input).</summary>
    Task<bool> ExisteEventoAsync(long eventoId);

    /// <summary>Ids de álbumes del curso que tienen fotos (no pueden darse de baja sin borrarlas).</summary>
    Task<List<long>> GetAlbumIdsConFotosAsync(long cursoId);

    /// <summary>El curso tiene fotos (propias de álbumes o grupales): bloquea el delete.</summary>
    Task<bool> TieneFotosAsync(long cursoId);
}
