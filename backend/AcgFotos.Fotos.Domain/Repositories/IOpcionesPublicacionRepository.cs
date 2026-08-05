using AcgFotos.Core.Data;
using AcgFotos.Fotos.Domain.Entities;

namespace AcgFotos.Fotos.Domain.Repositories;

public interface IOpcionesPublicacionRepository : IEntityBaseRepository<OpcionesPublicacion>
{
    /// <summary>Las opciones default del tenant actual, si hay alguna. Read-only.</summary>
    Task<OpcionesPublicacion?> GetDefaultReadOnlyAsync();
}
