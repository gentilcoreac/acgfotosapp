using AcgFotos.Core.Application;
using System.Collections.Generic;

namespace AcgFotos.Base.Application.Dtos
{
    public class PermisoDto : DtoBase
    {
        public string Nombre { get; set; }
        public string CodigoPermiso { get; set; }
        public string Descripcion { get; set; }
        public bool Activo { get; set; }

        public ICollection<PermisoEndpointDto> Endpoints { get; set; }
        public long? PermisoPadreId { get; set; }
        public string PermisoPadreDescripcion { get; set; }

        public PermisoDto PermisoPadre { get; set; }
        public long? AplicacionId { get; set; }
        public string AplicacionDescripcion { get; set; }

        // Esta propiedad no debería estar
        //public AplicacionDto Aplicacion { get; set; }

        public ICollection<RolPermisoDto> RolPermisos { get; set; }

        public bool EsRestringido { get; set; }

        public PermisoDto()
        {
            this.Endpoints = new List<PermisoEndpointDto>();
            this.RolPermisos = new List<RolPermisoDto>();
        }

    }
}
