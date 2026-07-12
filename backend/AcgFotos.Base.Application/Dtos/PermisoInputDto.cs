using System.Collections.Generic;
using AcgFotos.Core.Application;

namespace AcgFotos.Base.Application.Dtos
{
    /// <summary>
    /// Shape de input para Update/Create de Permiso. `CodigoPermiso` es editable (alta y edición)
    /// como en la API original. NO incluye `EsRestringido` (admin-only): se cambia por
    /// POST /permisos/{id}/set-es-restringido. La defensa de mass-assignment es el shape.
    ///
    /// Nota: `CodigoPermiso` es la clave de negocio (la usan JWT claims, atributos [Permiso(...)],
    /// asignaciones RolPermiso). Renombrarlo en una edición puede desvincular esos usos — queda a
    /// criterio del administrador.
    /// </summary>
    public class PermisoInputDto : DtoBase
    {
        public string Nombre { get; set; }
        public string CodigoPermiso { get; set; }
        public string Descripcion { get; set; }
        public bool Activo { get; set; }
        public long? PermisoPadreId { get; set; }
        public long? AplicacionId { get; set; }

        public ICollection<PermisoEndpointDto> Endpoints { get; set; }

        public PermisoInputDto()
        {
            this.Endpoints = new List<PermisoEndpointDto>();
        }
    }
}
