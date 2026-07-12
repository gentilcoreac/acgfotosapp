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
    public class RolAppService : ExtendedEntityAppServiceBase<Rol,
                                                              RolInputDto,
                                                              RolDto,
                                                              RolHeaderDto,
                                                              ListaPaginadaCriteriaBase>, IRolAppService
    {
        private readonly IRolRepository _rolRepository;
        private readonly IUsuarioRepository _usuarioRepository;
        private readonly IUsuarioAppService _usuarioAppService;
        private readonly RolMapper _rolMapper;

        public RolAppService(
            IUnitOfWork unitOfWork,
            IEntityBaseRepository<Rol> entityRepository,
            IRolRepository rolRepository,
            IUsuarioRepository usuarioRepository,
            IUsuarioAppService usuarioAppService,
            IAppContext appContext,
            IMapper mapper,
            RolMapper rolMapper) : base(unitOfWork, entityRepository, appContext, mapper)
        {
            _rolRepository = rolRepository;
            _usuarioRepository = usuarioRepository;
            _usuarioAppService = usuarioAppService;
            _rolMapper = rolMapper;
        }

        public override async Task<PaginationSet<RolHeaderDto>> SearchAsync(ListaPaginadaCriteriaBase criteria)
        {
            var page = await _rolRepository.PaginateHeadersAsync(criteria);
            return page.MapItems(projection => _rolMapper.ToHeaderDto(projection));
        }

        protected override Task<Rol> GetEntityToUpdateAsync(long id) =>
            _rolRepository.GetByIdWithRolPermisosAsync(id);

        public override async Task<RolDto> GetByIdAsync(long id)
        {
            var entity = await _rolRepository.GetByIdWithRolPermisosReadOnlyAsync(id);
            return _rolMapper.ToDto(entity);
        }

        protected override RolDto ToOutput(Rol entity) => _rolMapper.ToDto(entity);

        // Match por PermisoId: el front manda checkboxes (no el Id surrogate del join).
        protected override void SyncCollections(Rol rol, RolInputDto dto) =>
            ChildCollectionSync.SyncBy(
                rol.RolPermisos,
                dto.PermisoIds,
                rp => rp.PermisoId,
                permisoId => new RolPermiso { PermisoId = permisoId });

        public async Task<PaginationSet<RolDto>> SearchByUserAplicacionesAsync(ListaPaginadaCriteriaBase criteria)
        {
            var usuario = await _usuarioRepository.GetByUserNameAsync(this.AppContext.UserName);
            var aplicationsByUser = usuario != null
                ? await _usuarioRepository.GetAplicacionesPermitidasAsync(usuario.Id)
                : new List<UsuarioAplicacion>();

            // SearchText filtrado en SQL. El filtro "todas sus aplicaciones están permitidas" sí
            // queda en memoria porque depende del set aplicationsByUser; el universo de roles es chico.
            var roles = await _rolRepository.GetWithRolPermisosAndPermisosAsync(criteria.SearchText);

            var rolesDisponibles = new List<RolDto>();
            foreach (var rol in roles)
            {
                var todasSusAplicacionesEstanPermitidas = rol.RolPermisos.All(
                    rp => aplicationsByUser.Any(a => a.AplicacionId == rp.Permiso.AplicacionId));

                if (todasSusAplicacionesEstanPermitidas)
                {
                    rolesDisponibles.Add(_rolMapper.ToDto(rol));
                }
            }

            return new PaginationSet<RolDto>
            {
                Items = rolesDisponibles,
                TotalCount = rolesDisponibles.Count,
                TotalPages = 1
            };
        }

        public async Task<List<Rol>> GetRolesByUsuarioIdAsync(long id)
        {
            var existingUser = await _usuarioAppService.GetByIdAsync(id);
            if (existingUser == null)
            {
                return new List<Rol>();
            }

            return existingUser.Roles
                .Select(userRole => new Rol
                {
                    Descripcion = userRole.RolDescripcion,
                    Id = userRole.RolId
                })
                .Distinct()
                .ToList();
        }

        public async Task<List<Rol>> GetRolesPermisosAsync()
        {
            var list = await _rolRepository.GetWithRolPermisosAndPermisosAsync();
            return list.ToList();
        }

        public async Task<List<Rol>> GetRolesByTipoLicenciaAsync(long tipoLicenciaId)
        {
            var list = await _rolRepository.GetByTipoLicenciaIdAsync(tipoLicenciaId);
            return list.ToList();
        }

        public async Task<List<RolDelTenantDto>> GetRolesDelTenantAsync()
        {
            var list = await _rolRepository.GetDelTenantConLicenciasAsync(this.AppContext.TenantId);
            return list.Select(r => new RolDelTenantDto
            {
                Id = r.Id,
                Descripcion = r.Descripcion,
                Licencias = r.Licencias
                    .Select(l => new TipoLicenciaTagDto { Id = l.Id, Descripcion = l.Descripcion })
                    .ToList()
            }).ToList();
        }

        /// <summary>
        /// Endpoint dedicado para EsDefaultParaNuevoTenant. Requiere root (validado en runtime + Controller).
        /// </summary>
        public async Task SetDefaultTenantAsync(long id, bool esDefaultParaNuevoTenant)
        {
            if (!this.AppContext.IsRoot)
            {
                throw new BusinessValidationException(MessagesAPI.ErrorUserNoPrivilegesChangeTenantData);
            }

            var rol = await this.GetEntityToUpdateAsync(id)
                ?? throw new BusinessValidationException(MessagesAPI.ErrorGenericInternal);

            rol.EsDefaultParaNuevoTenant = esDefaultParaNuevoTenant;
            await this.UnitOfWork.CommitAsync();
        }
    }
}
