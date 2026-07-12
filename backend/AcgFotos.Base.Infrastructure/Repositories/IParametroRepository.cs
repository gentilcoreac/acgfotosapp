using System.Collections.Generic;
using System.Threading.Tasks;
using AcgFotos.Base.Domain.Entities;
using AcgFotos.Core.Application;
using AcgFotos.Core.Data;

namespace AcgFotos.Base.Infrastructure.Repositories
{
    public interface IParametroRepository : IEntityBaseRepository<Parametro>
    {
        /// <summary>Paginado con Aplicacion incluido. Opcionalmente filtra por aplicacionId. Read-only.</summary>
        Task<PaginationSet<Parametro>> PaginateWithAplicacionAsync(IListaPaginadaCriteriaBase criteria, long? aplicacionId);

        /// <summary>Parametro por Id con Aplicacion. Read-only.</summary>
        Task<Parametro> GetByIdWithAplicacionAsync(long id);

        /// <summary>Todos los parametros con Aplicacion. Read-only.</summary>
        Task<IReadOnlyList<Parametro>> GetAllWithAplicacionAsync();

        /// <summary>Parametros de una aplicación con Aplicacion incluida. Read-only.</summary>
        Task<IReadOnlyList<Parametro>> GetByAplicacionIdAsync(long aplicacionId);

        /// <summary>Parametro por Nombre + AplicacionId (exact match). Read-only.</summary>
        Task<Parametro> GetByNombreAndAplicacionAsync(string nombre, long aplicacionId);

        /// <summary>Parametro por Nombre (primer match). Read-only.</summary>
        Task<Parametro> GetByNombreAsync(string nombre);

        /// <summary>ParametroValorTenant de un tenant con Parametro incluido. Read-only.</summary>
        Task<IReadOnlyList<ParametroValorTenant>> GetValoresByTenantIdAsync(long tenantId);

        /// <summary>
        /// Paginado de overrides (ParametroValorTenant). Si <paramref name="tenantId"/> tiene valor,
        /// acota a ese tenant (aislamiento de NO-root); si es null, no filtra (root ve todos). Read-only.
        /// </summary>
        Task<PaginationSet<ParametroValorTenant>> PaginateValoresAsync(IListaPaginadaCriteriaBase criteria, long? tenantId);
    }
}
