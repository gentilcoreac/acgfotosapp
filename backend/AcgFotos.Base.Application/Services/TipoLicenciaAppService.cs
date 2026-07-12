using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
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
    public class TipoLicenciaAppService : ExtendedEntityAppServiceBase<TipoLicencia,
                                                                       TipoLicenciaInputDto,
                                                                       TipoLicenciaDto,
                                                                       TipoLicenciaHeaderDto,
                                                                       ListaPaginadaCriteriaBase>, ITipoLicenciaAppService
    {
        private readonly ITipoLicenciaRepository _tipoLicenciaRepository;
        private readonly TipoLicenciaMapper _tipoLicenciaMapper;

        public TipoLicenciaAppService(
            IUnitOfWork unitOfWork,
            IEntityBaseRepository<TipoLicencia> entityRepository,
            ITipoLicenciaRepository tipoLicenciaRepository,
            IAppContext appContext,
            IMapper mapper,
            TipoLicenciaMapper tipoLicenciaMapper) : base(unitOfWork, entityRepository, appContext, mapper)
        {
            _tipoLicenciaRepository = tipoLicenciaRepository;
            _tipoLicenciaMapper = tipoLicenciaMapper;
        }

        public override async Task<PaginationSet<TipoLicenciaHeaderDto>> SearchAsync(ListaPaginadaCriteriaBase criteria)
        {
            var page = await _tipoLicenciaRepository.PaginateHeadersAsync(criteria);
            return new PaginationSet<TipoLicenciaHeaderDto>
            {
                Page = page.Page,
                TotalCount = page.TotalCount,
                TotalPages = page.TotalPages,
                Items = page.Items.Select(e => _tipoLicenciaMapper.ToHeaderDto(e)).ToList()
            };
        }

        protected override Task<TipoLicencia> GetEntityToUpdateAsync(long id) =>
            _tipoLicenciaRepository.GetByIdWithRolesAsync(id);

        public override async Task<TipoLicenciaDto> GetByIdAsync(long id)
        {
            var entity = await _tipoLicenciaRepository.GetByIdWithRolesReadOnlyAsync(id);
            return _tipoLicenciaMapper.ToDto(entity);
        }

        protected override TipoLicenciaDto ToOutput(TipoLicencia entity) => _tipoLicenciaMapper.ToDto(entity);

        // Match por RolId: el front manda checkboxes (no el Id surrogate del join).
        protected override void SyncCollections(TipoLicencia tipoLicencia, TipoLicenciaInputDto dto) =>
            ChildCollectionSync.SyncBy(
                tipoLicencia.TipoLicenciaRoles,
                dto.RolIds,
                tlr => tlr.RolId,
                rolId => new TipoLicenciaRoles { RolId = rolId });

        /// <summary>
        /// Endpoint dedicado para EsDefaultParaNuevoTenant. Requiere root (validado en runtime + Controller).
        /// </summary>
        public async Task SetDefaultTenantAsync(long id, bool esDefaultParaNuevoTenant)
        {
            if (!this.AppContext.IsRoot)
            {
                throw new BusinessValidationException(MessagesAPI.ErrorUserNoPrivilegesChangeTenantData);
            }

            var tipoLicencia = await this.GetEntityToUpdateAsync(id)
                ?? throw new BusinessValidationException(MessagesAPI.ErrorGenericInternal);

            tipoLicencia.EsDefaultParaNuevoTenant = esDefaultParaNuevoTenant;
            await this.UnitOfWork.CommitAsync();
        }
    }
}
