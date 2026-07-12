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
using AcgFotos.Core.Session;

namespace AcgFotos.Base.Application.Services
{
    public class GrupoAppService : ExtendedEntityAppServiceBase<Grupo,
                                                                GrupoInputDto,
                                                                GrupoDto,
                                                                GrupoHeaderDto,
                                                                ListaPaginadaCriteriaBase>, IGrupoAppService
    {
        private readonly IGrupoRepository _grupoRepository;
        private readonly GrupoMapper _grupoMapper;

        public GrupoAppService(
            IUnitOfWork unitOfWork,
            IEntityBaseRepository<Grupo> entityRepository,
            IGrupoRepository grupoRepository,
            IAppContext appContext,
            IMapper mapper,
            GrupoMapper grupoMapper) : base(unitOfWork, entityRepository, appContext, mapper)
        {
            _grupoRepository = grupoRepository;
            _grupoMapper = grupoMapper;
        }

        // Listado paginado y proyectado en SQL (id, nombre, cantidadMiembros) — sin cargar usuarios.
        public override async Task<PaginationSet<GrupoHeaderDto>> SearchAsync(ListaPaginadaCriteriaBase criteria)
        {
            var page = await _grupoRepository.PaginateHeadersAsync(criteria);
            return page.MapItems(projection => _grupoMapper.ToHeaderDto(projection));
        }

        protected override Task<Grupo> GetEntityToUpdateAsync(long id) =>
            _grupoRepository.GetByIdWithUsuariosAsync(id);

        public override async Task<GrupoDto> GetByIdAsync(long id)
        {
            var entity = await _grupoRepository.GetByIdWithUsuariosReadOnlyAsync(id);
            if (entity == null) return null;
            var dto = _grupoMapper.ToDto(entity);

            // Licencia activa de cada miembro (una query; el Usuario incluido no trae sus licencias),
            // para que el ABM avise si el grupo mezcla licencias (§11.2).
            var licenciaPorUsuario = await _grupoRepository.GetActiveTipoLicenciaIdByUsuarioIdsAsync(
                dto.UsuarioGrupos.Select(m => m.UsuarioId).Distinct().ToList());

            // Enriquecer cada miembro con su nombre para los chips del selector en edición.
            // (El mapper deja estos campos en null; acá el Usuario está cargado -ThenInclude- y es read-only.)
            var miembrosPorId = entity.UsuarioGrupos.ToDictionary(ug => ug.Id);
            foreach (var miembro in dto.UsuarioGrupos)
            {
                if (miembrosPorId.TryGetValue(miembro.Id, out var ug) && ug.Usuario != null)
                {
                    miembro.UsuarioUserName = ug.Usuario.UserName;
                    miembro.UsuarioNombre = ug.Usuario.Nombre;
                    miembro.UsuarioApellido = ug.Usuario.Apellido;
                }

                miembro.UsuarioTipoLicenciaActivaId =
                    licenciaPorUsuario.TryGetValue(miembro.UsuarioId, out var tipoLicenciaId)
                        ? tipoLicenciaId
                        : (long?)null;
            }

            return dto;
        }

        protected override GrupoDto ToOutput(Grupo entity) => _grupoMapper.ToDto(entity);

        // Match por UsuarioId: el front manda los miembros como ids (no el Id surrogate del join).
        // El TenantId de las filas nuevas lo asigna SetAppContextTenant (recursivo) tras este hook.
        protected override void SyncCollections(Grupo grupo, GrupoInputDto dto)
        {
            // Match por clave de negocio (UsuarioId / RolId), no por el Id surrogate del join.
            // El TenantId de las filas nuevas lo asigna SetAppContextTenant (recursivo) tras este hook.
            ChildCollectionSync.SyncBy(
                grupo.UsuarioGrupos,
                dto.UsuarioIds,
                ug => ug.UsuarioId,
                usuarioId => new UsuarioGrupo { UsuarioId = usuarioId });

            ChildCollectionSync.SyncBy(
                grupo.GrupoRoles,
                dto.RolIds,
                gr => gr.RolId,
                rolId => new GrupoRol { RolId = rolId });
        }
    }
}
