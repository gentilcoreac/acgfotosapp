using AcgFotos.Core.Application;
using System;
using System.Collections.Generic;
using System.Text;

namespace AcgFotos.Base.Application.Dtos
{
    public class RolPermisoDto : DtoBase
    {
        public long RolId { get; set; }
        public RolDto Rol { get; set; }
        public long PermisoId { get; set; }
        public PermisoDto Permiso { get; set; }
    }
}
