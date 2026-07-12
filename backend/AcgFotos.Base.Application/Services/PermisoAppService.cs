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
using AcgFotos.Base.Infrastructure.Repositories.Projections;
using AcgFotos.Core.Application;
using AcgFotos.Core.Data;
using AcgFotos.Core.Exceptions;
using AcgFotos.Core.Localization.APIResources;
using AcgFotos.Core.Session;
using AcgFotos.Core.TreeView;

namespace AcgFotos.Base.Application.Services
{
    public class PermisoAppService : ExtendedEntityAppServiceBase<Permiso,
                                                                  PermisoInputDto,
                                                                  PermisoDto,
                                                                  PermisoHeaderDto,
                                                                  ListaPaginadaCriteriaBase>, IPermisoAppService
    {
        private readonly IPermisoRepository _permisoRepository;
        private readonly IConfiguration _configuration;
        private readonly PermisoMapper _permisoMapper;

        public PermisoAppService(
            IUnitOfWork unitOfWork,
            IEntityBaseRepository<Permiso> entityRepository,
            IPermisoRepository permisoRepository,
            IAppContext appContext,
            IConfiguration configuration,
            IMapper mapper,
            PermisoMapper permisoMapper) : base(unitOfWork, entityRepository, appContext, mapper)
        {
            _permisoRepository = permisoRepository;
            _configuration = configuration;
            _permisoMapper = permisoMapper;
        }

        // No-root users no pueden ver permisos restringidos.
        private bool ShouldExcludeRestringidos()
        {
            var rootTenantId = _configuration.GetValue<long>("RootTenantId");
            return !(this.AppContext.IsRoot && this.AppContext.TenantId == rootTenantId);
        }

        public override async Task<PaginationSet<PermisoHeaderDto>> SearchAsync(ListaPaginadaCriteriaBase criteria)
        {
            var page = await _permisoRepository.PaginateHeadersAsync(criteria, this.ShouldExcludeRestringidos());
            return page.MapItems(_permisoMapper.ToHeaderDto);
        }

        protected override Task<Permiso> GetEntityToUpdateAsync(long id) =>
            _permisoRepository.GetByIdWithEndpointsAsync(id);

        protected override PermisoDto ToOutput(Permiso entity) => _permisoMapper.ToDto(entity);

        public override async Task<PermisoDto> GetByIdAsync(long id)
        {
            var entity = await _permisoRepository.GetByIdWithDetailsAsync(id);
            return _permisoMapper.ToDto(entity);
        }

        public override async Task<IEnumerable<PermisoHeaderDto>> GetAllAsync()
        {
            var entities = await _permisoRepository.GetAllAsync(this.ShouldExcludeRestringidos());
            return entities.Select(_permisoMapper.ToHeaderDtoFromEntity);
        }

        /// <summary>
        /// Update público: recibe PermisoInputDto (shape defensivo), carga la entidad una sola vez
        /// (tracked, con Endpoints), preserva el campo sensible EsRestringido desde la entidad
        /// (admin-only, endpoint dedicado), y delega al pipeline interno. CodigoPermiso es editable.
        /// La entidad tracked se reutiliza en todo el flujo — sin doble fetch.
        /// </summary>
        public override async Task<PermisoDto> UpdateAsync(PermisoInputDto inputDto)
        {
            var isNew = (inputDto.Id == 0);
            if (isNew)
            {
                // En Create, SetValues copia CodigoPermiso (NOT NULL) desde el input; EsRestringido
                // queda en default (false) — se cambia luego por el endpoint dedicado (root).
                return await base.UpdateAsync(inputDto);
            }

            var currentEntity = await this.GetEntityToUpdateAsync(inputDto.Id)
                ?? throw new BusinessValidationException(MessagesAPI.ErrorGenericInternal);

            // Map InputDto → entidad existente (Mapperly). EsRestringido no se toca (ignore);
            // CodigoPermiso sí se actualiza (editable). Endpoints se reconcilia aparte (UpdateEndpoints).
            _permisoMapper.ApplyInput(inputDto, currentEntity);
            this.UpdateEndpoints(currentEntity, inputDto);

            await this.UnitOfWork.CommitAsync();
            return _permisoMapper.ToDto(currentEntity);
        }

        // Reemplaza la colección Endpoints del permiso por la del input.
        // ApplyInput copia primitives pero NO toca Endpoints (lo ignora); acá hacemos el
        // clear + add explícito para evitar comportamiento inesperado con tracking de EF Core
        // (las entidades viejas quedarían orphan).
        private void UpdateEndpoints(Permiso current, PermisoInputDto input)
        {
            current.Endpoints.Clear();
            if (input.Endpoints == null)
            {
                return;
            }
            foreach (var endpointDto in input.Endpoints)
            {
                current.Endpoints.Add(new PermisoEndpoint
                {
                    EndpointId = endpointDto.EndpointId,
                    PermisoId = current.Id
                });
            }
        }

        // Solo se usa en alta vía base.UpdateAsync. En edición Permiso tiene su propio override.
        protected override void SyncCollections(Permiso permiso, PermisoInputDto dto) =>
            ChildCollectionSync.SyncBy(
                permiso.Endpoints,
                dto.Endpoints?.Select(e => e.EndpointId) ?? Enumerable.Empty<long>(),
                pe => pe.EndpointId,
                endpointId => new PermisoEndpoint { EndpointId = endpointId });

        public async Task<List<HierarchicalItem<long>>> GetHierarchicalItemsAsync()
        {
            var hierarchicalManager = new HierarchicalManager();
            var permisos = await _permisoRepository.GetWithRolPermisosAsync(this.ShouldExcludeRestringidos());

            var permisosItems = permisos
                .Select(item => new ItemInfo
                {
                    Id = item.Id,
                    ParentId = item.PermisoPadreId,
                    Nombre = item.Nombre,
                    Description = item.Descripcion
                })
                .ToList();

            return hierarchicalManager.GetItems(permisosItems);
        }

        /// <summary>
        /// Endpoint dedicado para EsRestringido. Requiere root (validado en runtime + en Controller).
        /// </summary>
        public async Task SetEsRestringidoAsync(long id, bool esRestringido)
        {
            if (!this.AppContext.IsRoot)
            {
                throw new BusinessValidationException(MessagesAPI.ErrorUserNoPrivilegesChangeTenantData);
            }

            var permiso = await this.GetEntityToUpdateAsync(id)
                ?? throw new BusinessValidationException(MessagesAPI.ErrorGenericInternal);

            permiso.EsRestringido = esRestringido;
            await this.UnitOfWork.CommitAsync();
        }
    }
}
