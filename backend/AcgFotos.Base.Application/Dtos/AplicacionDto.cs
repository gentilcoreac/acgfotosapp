using AcgFotos.Core.Application;
using System.Collections.Generic;

namespace AcgFotos.Base.Application.Dtos {
    public class AplicacionDto : DtoBase {
        public string Codigo { get; set; }
        public string Nombre { get; set; }
        public bool Activo { get; set; }
        public string Icono { get; set; }
        public string IconoUrl { get; set; }

        public ICollection<PermisoDto> Permisos { get; set; }

        public AplicacionDto()
        {
            this.Permisos = new List<PermisoDto>();
        }
    }
}
