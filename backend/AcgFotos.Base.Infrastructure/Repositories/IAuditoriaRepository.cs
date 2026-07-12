using System;
using System.Threading.Tasks;
using AcgFotos.Base.Domain.Entities;
using AcgFotos.Base.Infrastructure.Repositories.Projections;
using AcgFotos.Core.Application;
using AcgFotos.Core.Data;

namespace AcgFotos.Base.Infrastructure.Repositories
{
    public interface IAuditoriaRepository : IEntityBaseRepository<Auditoria>
    {
        /// <summary>
        /// Paginado liviano (proyección <see cref="AuditoriaHeaderProjection"/>, sin los campos
        /// pesados) con filtros opcionales. Null/empty deshabilita el filtro. Read-only.
        /// </summary>
        Task<PaginationSet<AuditoriaHeaderProjection>> PaginateWithFiltersAsync(
            IListaPaginadaCriteriaBase criteria,
            string clientIP,
            string clientUserAgent,
            string metodo,
            string servicio,
            string resultStatusCode,
            long? usuarioId,
            DateTime? fechaDesde,
            DateTime? fechaHasta);

        /// <summary>Auditoría por Id con Usuario. Read-only.</summary>
        Task<Auditoria> GetByIdWithUsuarioAsync(long id);
    }
}
