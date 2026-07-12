using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.Extensions.Configuration;
using AcgFotos.Base.Application.Dtos;
using AcgFotos.Base.Application.IServices;
using AcgFotos.Base.Application.Mappers.Mapperly;
using AcgFotos.Base.Domain.Entities;
using AcgFotos.Base.Infrastructure.Repositories;
using AcgFotos.Core.Application;
using AcgFotos.Core.Data;
using AcgFotos.Core.Exceptions;
using AcgFotos.Core.Localization.APIResources;
using AcgFotos.Core.Session;

namespace AcgFotos.Base.Application.Services
{
    public class AplicacionAppService : EntityAppServiceBase<Aplicacion,
                                                             AplicacionDto,
                                                             ListaPaginadaCriteriaBase>, IAplicacionAppService
    {
        private readonly IConfiguration _configuration;
        private readonly IAplicacionRepository _aplicacionRepository;
        private readonly IUsuarioRepository _usuarioRepository;
        private readonly ITenantRepository _tenantRepository;
        private readonly AplicacionMapper _aplicacionMapper;

        public AplicacionAppService(
            IUnitOfWork unitOfWork,
            IEntityBaseRepository<Aplicacion> entityRepository,
            IAplicacionRepository aplicacionRepository,
            IUsuarioRepository usuarioRepository,
            ITenantRepository tenantRepository,
            IConfiguration configuration,
            IAppContext appContext,
            IMapper mapper,
            AplicacionMapper aplicacionMapper) : base(unitOfWork, entityRepository, appContext, mapper)
        {
            _aplicacionRepository = aplicacionRepository;
            _usuarioRepository = usuarioRepository;
            _tenantRepository = tenantRepository;
            _configuration = configuration;
            _aplicacionMapper = aplicacionMapper;
        }

        /// <summary>Read path por Mapperly. Lo usa el listado del ABM (Search heredado) y GetAll.</summary>
        protected override AplicacionDto ToOutput(Aplicacion entity) => _aplicacionMapper.ToDto(entity);

        public override async Task<AplicacionDto> GetByIdAsync(long id)
        {
            var entity = await this.EntityRepository.GetByIdAsync(id);
            if (entity == null)
            {
                return null;
            }

            var dto = _aplicacionMapper.ToDto(entity);

            var permisos = await _aplicacionRepository.GetPermisosByIdAsync(id);
            dto.Permisos = _aplicacionMapper.ToPermisoDtos(permisos);

            return dto;
        }

        public override async Task<PaginationSet<AplicacionDto>> SearchAsync(ListaPaginadaCriteriaBase criteria)
        {
            var page = await _aplicacionRepository.PaginateHeadersAsync(criteria);
            return page.MapItems(_aplicacionMapper.ToDto);
        }

        public override async Task<IEnumerable<AplicacionDto>> GetAllAsync()
        {
            var entities = await this.EntityRepository.GetAllAsync();
            return entities.Select(_aplicacionMapper.ToDto);
        }

        public async Task<List<UsuarioAplicacionDto>> GetAplicacionesPermitidasAsync()
        {
            var aplicaciones = await _usuarioRepository.GetAplicacionesPermitidasAsync(this.AppContext.UserId);
            return _aplicacionMapper.ToUsuarioAplicacionDtos(aplicaciones);
        }

        public async Task<List<AplicacionDto>> GetAplicacionesPorTenantAsync()
        {
            var aplicaciones = await _tenantRepository.GetAplicacionesByTenantIdAsync(this.AppContext.TenantId);
            return aplicaciones.Select(_aplicacionMapper.ToDto).ToList();
        }

        public async Task<List<AplicacionDto>> GetAplicacionesPorTenantIdAsync(long id)
        {
            var rootTenantId = _configuration.GetValue<long>("RootTenantId");
            if (!(this.AppContext.IsRoot && this.AppContext.TenantId == rootTenantId))
            {
                throw new BusinessValidationException(MessagesAPI.ErrorUserNotPrivilegesAccess);
            }

            var aplicaciones = await _tenantRepository.GetAplicacionesByTenantIdAsync(id);
            return aplicaciones.Select(_aplicacionMapper.ToDto).ToList();
        }
    }
}
