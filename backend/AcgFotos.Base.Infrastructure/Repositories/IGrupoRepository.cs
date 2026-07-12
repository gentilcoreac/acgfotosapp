using System.Collections.Generic;
using System.Threading.Tasks;
using AcgFotos.Base.Domain.Entities;
using AcgFotos.Base.Infrastructure.Repositories.Projections;
using AcgFotos.Core.Application;
using AcgFotos.Core.Data;

namespace AcgFotos.Base.Infrastructure.Repositories
{
    public interface IGrupoRepository : IEntityBaseRepository<Grupo>
    {
        /// <summary>Listado paginado proyectado a GrupoHeaderProjection (id, nombre, cantidad de miembros). Read-only, en SQL.</summary>
        Task<PaginationSet<GrupoHeaderProjection>> PaginateHeadersAsync(IListaPaginadaCriteriaBase criteria);

        /// <summary>Grupo con sus UsuarioGrupos. Tracked (para edición).</summary>
        Task<Grupo> GetByIdWithUsuariosAsync(long id);

        /// <summary>Grupo con sus UsuarioGrupos. Read-only.</summary>
        Task<Grupo> GetByIdWithUsuariosReadOnlyAsync(long id);

        /// <summary>
        /// Id de la licencia ACTIVA de cada usuario dado (gen_UsuarioTipoLicencia con IsActive). Mapa
        /// usuarioId → tipoLicenciaId; los usuarios sin licencia activa no aparecen en el mapa. Lo usa el
        /// detalle del grupo para avisar mezcla de licencias (§11.2). Read-only, en SQL.
        /// </summary>
        Task<Dictionary<long, long>> GetActiveTipoLicenciaIdByUsuarioIdsAsync(IReadOnlyCollection<long> usuarioIds);
    }
}
