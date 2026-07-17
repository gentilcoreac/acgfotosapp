using AcgFotos.Core.Data;
using AcgFotos.Fotos.Domain.Entities;

namespace AcgFotos.Fotos.Domain.Repositories;

public interface ICodigoAccesoRepository : IEntityBaseRepository<CodigoAcceso>
{
    /// <summary>
    /// Busca el código (ya normalizado por el caller) SIN filtro de tenant: en el canje todavía no
    /// se sabe a qué tenant pertenece. Trae Participante → Grupo → Evento para validar vigencia.
    /// </summary>
    Task<CodigoAcceso?> GetVigenteConEventoAsync(string codigo);
}
