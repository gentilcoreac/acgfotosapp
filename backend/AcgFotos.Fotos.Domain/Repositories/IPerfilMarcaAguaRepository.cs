using AcgFotos.Core.Data;
using AcgFotos.Fotos.Domain.Entities;

namespace AcgFotos.Fotos.Domain.Repositories;

public interface IPerfilMarcaAguaRepository : IEntityBaseRepository<PerfilMarcaAgua>
{
    /// <summary>Perfil por Id con sus capas, read-only (resolución de cascada y composición).</summary>
    Task<PerfilMarcaAgua?> GetByIdConCapasReadOnlyAsync(long id);

    /// <summary>El perfil default del tenant actual con sus capas, si hay alguno. Read-only.</summary>
    Task<PerfilMarcaAgua?> GetDefaultConCapasReadOnlyAsync();
}
