using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AcgFotos.Base.Domain.Entities;
using AcgFotos.Base.Infrastructure.Repositories.Projections;
using AcgFotos.Core.Application;
using AcgFotos.Core.Data;
using AcgFotos.Core.Session;

namespace AcgFotos.Base.Infrastructure.Repositories
{
    public class AplicacionRepository : EntityBaseRepository<Aplicacion>, IAplicacionRepository
    {
        public AplicacionRepository(IDbContext dbContext, IAppContext appContext)
            : base(dbContext, appContext)
        {
        }

        // Listado del ABM proyectado a AplicacionHeaderProjection (id/código/nombre/activo; la
        // proyección en SQL evita traer Icono/IconoUrl) + búsqueda libre server-side (código/nombre).
        public Task<PaginationSet<AplicacionHeaderProjection>> PaginateHeadersAsync(IListaPaginadaCriteriaBase criteria)
        {
            var query = this.DbContext.Set<Aplicacion>().AsNoTracking();

            if (!string.IsNullOrWhiteSpace(criteria.SearchText))
            {
                var s = criteria.SearchText.Trim();
                query = query.Where(a =>
                    a.Codigo.Contains(s) ||
                    a.Nombre.Contains(s));
            }

            var projection = query
                .Select(a => new AplicacionHeaderProjection
                {
                    Id = a.Id,
                    Codigo = a.Codigo,
                    Nombre = a.Nombre,
                    Activo = a.Activo
                });
            return this.BuildPaginationAsync(projection, criteria);
        }

        public async Task<IReadOnlyList<Permiso>> GetPermisosByIdAsync(long aplicacionId) =>
            await this.DbContext.Set<Permiso>()
                .AsNoTracking()
                .Where(p => p.AplicacionId == aplicacionId)
                .ToListAsync();
    }
}
