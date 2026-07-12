using System.Threading.Tasks;
using AutoMapper;
using AcgFotos.Base.Application.Dtos;
using AcgFotos.Base.Application.IServices;
using AcgFotos.Base.Domain.Entities;
using AcgFotos.Base.Infrastructure.Repositories;
using AcgFotos.Core.Application;
using AcgFotos.Core.Data;
using AcgFotos.Core.Session;

namespace AcgFotos.Base.Application.Services
{
    public class AuditoriaAppService : ExtendedEntityAppServiceBase<Auditoria,
                                                                    AuditoriaDto,
                                                                    AuditoriaDto,
                                                                    AuditoriaHeaderDto,
                                                                    AuditoriaCriteria>, IAuditoriaAppService
    {
        private readonly IAuditoriaRepository _auditoriaRepository;

        public AuditoriaAppService(
            IUnitOfWork unitOfWork,
            IEntityBaseRepository<Auditoria> entityRepository,
            IAuditoriaRepository auditoriaRepository,
            IAppContext appContext,
            IMapper mapper) : base(unitOfWork, entityRepository, appContext, mapper)
        {
            _auditoriaRepository = auditoriaRepository;
        }

        /// <summary>Listado liviano (proyección sin los campos pesados). Aplica los filtros del criteria.</summary>
        public override async Task<PaginationSet<AuditoriaHeaderDto>> SearchAsync(AuditoriaCriteria criteria)
        {
            var page = await _auditoriaRepository.PaginateWithFiltersAsync(
                criteria,
                criteria.ClientIP,
                criteria.ClientUserAgent,
                criteria.Metodo,
                criteria.Servicio,
                criteria.ResultStatusCode,
                criteria.UsuarioId,
                criteria.FechaDesde,
                criteria.FechaHasta);

            return page.MapItems(p => new AuditoriaHeaderDto
            {
                Id = p.Id,
                FechaHora = p.FechaHora,
                Duracion = p.Duracion,
                Servicio = p.Servicio,
                Metodo = p.Metodo,
                UsuarioId = p.UsuarioId,
                ImpersonatedBy = p.ImpersonatedBy,
                HttpMethod = p.HttpMethod,
                RequestAbsolutePath = p.RequestAbsolutePath,
                ResultStatusCode = p.ResultStatusCode,
                UsuarioNombre = p.UsuarioNombre,
            });
        }

        /// <summary>Detalle completo por id (incluye Parametros/ResultContent y datos del usuario).</summary>
        public override async Task<AuditoriaDto> GetByIdAsync(long id)
        {
            var entity = await _auditoriaRepository.GetByIdWithUsuarioAsync(id);
            return this.Mapper.Map<AuditoriaDto>(entity);
        }
    }
}
