using System;
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
    public class AuditoriaRepository : EntityBaseRepository<Auditoria>, IAuditoriaRepository
    {
        public AuditoriaRepository(IDbContext dbContext, IAppContext appContext)
            : base(dbContext, appContext)
        {
        }

        public Task<PaginationSet<AuditoriaHeaderProjection>> PaginateWithFiltersAsync(
            IListaPaginadaCriteriaBase criteria,
            string clientIP,
            string clientUserAgent,
            string metodo,
            string servicio,
            string resultStatusCode,
            long? usuarioId,
            DateTime? fechaDesde,
            DateTime? fechaHasta)
        {
            IQueryable<Auditoria> query = this.DbContext.Set<Auditoria>()
                .AsNoTracking()
                .Include(x => x.Usuario);

            // El audit log es herramienta de root (ADR-0005); por seed sus endpoints son solo-root.
            // Defensa en profundidad: si un no-root llegara a tener acceso (re-mapeo del permiso),
            // ve solo la actividad de SU tenant. gen_AuditLogs no tiene TenantId: se resuelve por
            // el usuario auditado. Las filas anonimas (UsuarioId NULL, ej. logins) quedan solo-root.
            if (!this.AppContext.IsRoot)
            {
                var tenantId = this.AppContext.TenantId;
                query = query.Where(x => x.Usuario.TenantId == tenantId);
            }

            if (!string.IsNullOrEmpty(clientIP))
            {
                query = query.Where(x => x.ClientIP.Contains(clientIP));
            }
            if (!string.IsNullOrEmpty(clientUserAgent))
            {
                var ua = clientUserAgent.ToLower();
                query = query.Where(x => x.ClientUserAgent.ToLower().Contains(ua));
            }
            if (!string.IsNullOrEmpty(metodo))
            {
                var m = metodo.ToLower();
                query = query.Where(x => x.Metodo.ToLower().Contains(m));
            }
            if (!string.IsNullOrEmpty(servicio))
            {
                var s = servicio.ToLower();
                query = query.Where(x => x.Servicio.ToLower().Contains(s));
            }
            if (!string.IsNullOrEmpty(resultStatusCode))
            {
                var sc = resultStatusCode.ToLower();
                query = query.Where(x => x.ResultStatusCode.ToLower().Contains(sc));
            }
            if (usuarioId.HasValue)
            {
                query = query.Where(x => x.UsuarioId == usuarioId.Value);
            }
            if (fechaDesde.HasValue)
            {
                query = query.Where(x => x.FechaHora >= fechaDesde.Value);
            }
            if (fechaHasta.HasValue)
            {
                query = query.Where(x => x.FechaHora <= fechaHasta.Value);
            }

            // Proyección liviana: el listado no trae Parametros/ResultContent (nvarchar(max)) ni
            // ClientIP/UserAgent. Esos campos pesados/de detalle se obtienen por id.
            var projection = query.Select(x => new AuditoriaHeaderProjection
            {
                Id = x.Id,
                FechaHora = x.FechaHora,
                Duracion = x.Duracion,
                Servicio = x.Servicio,
                Metodo = x.Metodo,
                UsuarioId = x.UsuarioId,
                ImpersonatedBy = x.ImpersonatedBy,
                HttpMethod = x.HttpMethod,
                RequestAbsolutePath = x.RequestAbsolutePath,
                ResultStatusCode = x.ResultStatusCode,
                UsuarioNombre = x.Usuario != null ? x.Usuario.UserName : null,
            });

            return this.BuildPaginationAsync(projection, criteria);
        }

        public Task<Auditoria> GetByIdWithUsuarioAsync(long id)
        {
            IQueryable<Auditoria> query = this.DbContext.Set<Auditoria>()
                .AsNoTracking()
                .Include(x => x.Usuario);

            // Misma defensa en profundidad que el listado: no-root solo ve filas de su tenant.
            if (!this.AppContext.IsRoot)
            {
                var tenantId = this.AppContext.TenantId;
                query = query.Where(x => x.Usuario.TenantId == tenantId);
            }

            return query.FirstOrDefaultAsync(x => x.Id == id);
        }
    }
}
