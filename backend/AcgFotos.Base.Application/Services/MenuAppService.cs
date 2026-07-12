using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.Extensions.Configuration;
using AcgFotos.Base.Application.Constantes;
using AcgFotos.Base.Application.Criterias;
using AcgFotos.Base.Application.Dtos;
using AcgFotos.Base.Application.IServices;
using AcgFotos.Base.Application.Mappers.Mapperly;
using AcgFotos.Base.Application.Outputs;
using AcgFotos.Base.Domain.Entities;
using AcgFotos.Base.Infrastructure.Repositories;
using AcgFotos.Core.Application;
using AcgFotos.Core.Data;
using AcgFotos.Core.Localization.APIResources;
using AcgFotos.Core.Session;

namespace AcgFotos.Base.Application.Services
{
    public class MenuAppService : EntityAppServiceBase<Menu,
                                                       MenuDto,
                                                       MenuCriteria>, IMenuAppService
    {
        private readonly IMenuRepository _menuRepository;
        private readonly IPermisoRepository _permisoRepository;
        private readonly IUsuarioRepository _usuarioRepository;
        private readonly IConfiguration _configuration;
        private readonly MenuMapper _menuMapper;

        public MenuAppService(
            IUnitOfWork unitOfWork,
            IEntityBaseRepository<Menu> entityRepository,
            IMenuRepository menuRepository,
            IPermisoRepository permisoRepository,
            IUsuarioRepository usuarioRepository,
            IConfiguration configuration,
            IAppContext appContext,
            IMapper mapper,
            MenuMapper menuMapper) : base(unitOfWork, entityRepository, appContext, mapper)
        {
            _menuRepository = menuRepository;
            _permisoRepository = permisoRepository;
            _usuarioRepository = usuarioRepository;
            _configuration = configuration;
            _menuMapper = menuMapper;
        }

        /// <summary>Read path por Mapperly (sin AutoMapper). Lo usa el detalle (GetById) y el update.</summary>
        protected override MenuDto ToOutput(Menu entity) => _menuMapper.ToDto(entity);

        public override async Task<PaginationSet<MenuDto>> SearchAsync(MenuCriteria criteria)
        {
            var page = await _menuRepository.PaginateWithPermisoAsync(criteria, criteria.AplicacionId);
            return page.MapItems(_menuMapper.ToDto);
        }

        public override async Task<IEnumerable<MenuDto>> GetAllAsync()
        {
            var entities = await this.EntityRepository.GetAllAsync();
            return entities.Select(_menuMapper.ToDto);
        }

        public override async Task<IEnumerable<MenuDto>> ExportAsync(MenuCriteria criteria)
        {
            var entities = await this.EntityRepository.GetAllAsync();
            return entities.Select(_menuMapper.ToDto);
        }

        public async Task<List<MenuDto>> ObtenerMenuAsideAsync()
        {
            var menues = await this.ArmarMenuUsuarioSegunPermisosAsync();
            var aside = menues.Where(x => x.VisibleSideMenu).ToList();
            var arbol = this.ArmarArbolJerarquico(aside);
            // Se devuelve el árbol como MenuDto (MenuHijos por id, sin back-reference al padre): la
            // entidad Menu tiene MenuPadre↔MenuHijos (ciclo) y System.Text.Json no lo serializa.
            return this.MapTree(arbol);
        }

        private List<MenuDto> MapTree(IEnumerable<Menu> menues) =>
            menues.Select(m =>
            {
                var dto = _menuMapper.ToDto(m);
                dto.MenuHijos = this.MapTree(m.MenuHijos ?? new List<Menu>());
                return dto;
            }).ToList();

        public async Task<List<MenuDashDto>> ObtenerMenusDashAsync()
        {
            var menues = await this.ArmarMenuUsuarioSegunPermisosAsync();
            var dash = menues.Where(x => x.VisibleDash).ToList();
            return _menuMapper.ToDashDtos(dash);
        }

        public async Task<List<AllowedRouteOutput>> ObtenerAllowedRoutesAsync()
        {
            var menues = await this.ArmarMenuUsuarioSegunPermisosAsync();
            return _menuMapper.ToAllowedRoutes(menues);
        }

        private async Task<List<Menu>> ArmarMenuUsuarioSegunPermisosAsync()
        {
            var rootTenantId = _configuration.GetValue<long>("RootTenantId");
            if (this.AppContext.IsRoot && this.AppContext.TenantId == rootTenantId)
            {
                var permisoRoot = await _permisoRepository.GetByCodigoPermisoAsync(Permisos.General.PermisoRoot)
                    ?? throw new System.Exception(MessagesAPI.ErrorOptionsValidNotFound);

                var menues = await _menuRepository.GetActivosByPermisoIdAsync(
                    permisoRoot.Id,
                    this.AppContext.AplicacionId);
                return menues.ToList();
            }

            if (!this.AppContext.AplicacionId.HasValue)
            {
                // Usuario sin aplicación seleccionada.
                return new List<Menu>();
            }

            var rolesId = await _usuarioRepository.GetEffectiveRolIdsByUserIdAsync(this.AppContext.UserId);
            var permisosId = await _permisoRepository.GetPermisoIdsByRolIdsAsync(rolesId);

            var menuesUsuario = await _menuRepository.GetActivosByPermisosAsync(
                permisosId,
                this.AppContext.AplicacionId.Value);
            return menuesUsuario.ToList();
        }

        private List<Menu> ArmarArbolJerarquico(List<Menu> menues)
        {
            //1. Armar el arbol de menù Completo
            var menuRoot = new Menu
            {
                Codigo = ".root",
                Nombre = "Root",
                MenuHijos = new List<Menu>(),
                MenuPadre = null
            };

            var menuArmado = (List<Menu>)menuRoot.MenuHijos;

            var menuPlanoWork = new List<Menu>(); //buscar referencia a items anidados

            if (menues == null)
            {
                return menuArmado;
            }

            foreach (var menuItem in menues)
            {
                if (menuItem.MenuPadreId == null)
                {
                    var itemRoot = new Menu()
                    {
                        Id = menuItem.Id,
                        Codigo = menuItem.Codigo,
                        Nombre = menuItem.Nombre,
                        ImagenWeb = menuItem.ImagenWeb,
                        MenuPadre = menuRoot,
                        PermisoId = menuItem.PermisoId,
                        Orden = menuItem.Orden,
                        RoutePath = menuItem.RoutePath,
                        VisibleDash = menuItem.VisibleDash
                    };

                    menuArmado.Add(itemRoot);
                    menuPlanoWork.Add(itemRoot);
                }
            }

            foreach (var menuItem in menues)
            {
                if (menuItem.MenuPadreId == null)
                {
                    continue;
                }

                var padre = menuPlanoWork.FirstOrDefault(x => x.Id == menuItem.MenuPadreId);

                var menuChild = new Menu()
                {
                    Id = menuItem.Id,
                    Codigo = menuItem.Codigo,
                    Nombre = menuItem.Nombre,
                    ImagenWeb = menuItem.ImagenWeb,
                    PermisoId = menuItem.PermisoId,
                    Orden = menuItem.Orden,
                    VisibleDash = menuItem.VisibleDash,
                    RoutePath = menuItem.RoutePath
                };

                if (padre == null)
                {
                    // El padre no está entre los menús del usuario (p.ej. un contenedor de otro
                    // permiso al que no tiene acceso): promovemos el hijo a nivel raíz para no perder
                    // un menú al que SÍ tiene permiso (antes el hijo quedaba huérfano y desaparecía).
                    menuChild.MenuPadre = menuRoot;
                    menuArmado.Add(menuChild);
                    menuPlanoWork.Add(menuChild);
                    continue;
                }

                menuChild.MenuPadre = padre;
                if (padre.MenuHijos == null) padre.MenuHijos = new List<Menu>();
                padre.MenuHijos.Add(menuChild);
                menuPlanoWork.Add(menuChild);
            }

            // Limpiar contenedores que queden sin hijos.
            foreach (var item in menuPlanoWork.Where(w => w.MenuPadre != null && w.MenuHijos == null))
            {
                this.PurgarParent(item);
            }

            menuArmado = menuArmado.OrderBy(x => x.Orden).ToList();

            foreach (var menuPadre in menuArmado)
            {
                if (menuPadre.MenuHijos.Count != 0)
                {
                    menuPadre.MenuHijos = menuPadre.MenuHijos.OrderBy(x => x.Orden).ToList();
                }
            }
            return menuArmado;
        }

        private void PurgarParent(Menu item)
        {
            if (item.MenuPadre == null) return;
            if (item.MenuHijos != null && item.MenuHijos.Count > 0) return;

            item.MenuPadre.MenuHijos.Remove(item);
            if (item.MenuPadre.MenuHijos.Count == 0) item.MenuPadre.MenuHijos = null;
            this.PurgarParent(item.MenuPadre);
        }
    }
}
