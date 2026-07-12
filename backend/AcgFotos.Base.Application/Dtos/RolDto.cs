using AcgFotos.Core.Application;
using System.Collections.Generic;

namespace AcgFotos.Base.Application.Dtos {
    public class RolDto : DtoBase {
        public string Descripcion { get; set; }

        public bool EsDefaultParaNuevoTenant { get; set; }

        public ICollection<RolPermisoDto> RolPermisos { get; set; }
    }
}
