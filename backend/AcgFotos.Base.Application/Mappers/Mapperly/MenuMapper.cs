using System.Collections.Generic;
using Riok.Mapperly.Abstractions;
using AcgFotos.Base.Application.Dtos;
using AcgFotos.Base.Application.Outputs;
using AcgFotos.Base.Domain.Entities;

namespace AcgFotos.Base.Application.Mappers.Mapperly
{
    /// <summary>
    /// Mapeo de Menu, 100% Mapperly (sin AutoMapper). Cubre los tres shapes de salida del módulo:
    /// <see cref="ToDto"/> (ABM lista/detalle), <see cref="ToDashDto"/> (accesos del dashboard) y
    /// <see cref="ToAllowedRoute"/> (rutas permitidas en runtime). El write path va por
    /// <c>SetValues</c> (EF nativo), no por este mapper.
    ///
    /// <para><see cref="RequiredMappingStrategy.Target"/>: sólo se exige completar los miembros del
    /// DTO destino; los miembros de la entidad no usados por cada shape (p.ej. navegaciones) no
    /// generan diagnósticos. PermisoNombre/AplicacionDescripcion/MenuPadreNombre se aplanan desde
    /// las navegaciones y son null-safe (quedan null si la nav no vino incluida en la query).</para>
    /// </summary>
    [Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
    public partial class MenuMapper
    {
        [MapProperty(new[] { nameof(Menu.Permiso), nameof(Permiso.Nombre) }, new[] { nameof(MenuDto.PermisoNombre) })]
        [MapProperty(new[] { nameof(Menu.Aplicacion), nameof(Aplicacion.Nombre) }, new[] { nameof(MenuDto.AplicacionDescripcion) })]
        [MapperIgnoreTarget(nameof(MenuDto.MenuHijos))]
        public partial MenuDto ToDto(Menu entity);

        [MapProperty(new[] { nameof(Menu.MenuPadre), nameof(Menu.Nombre) }, new[] { nameof(MenuDashDto.MenuPadreNombre) })]
        public partial MenuDashDto ToDashDto(Menu entity);

        public partial List<MenuDashDto> ToDashDtos(List<Menu> entities);

        public partial AllowedRouteOutput ToAllowedRoute(Menu entity);

        public partial List<AllowedRouteOutput> ToAllowedRoutes(List<Menu> entities);
    }
}
