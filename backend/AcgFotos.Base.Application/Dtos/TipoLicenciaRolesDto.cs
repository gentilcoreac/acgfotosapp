using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AcgFotos.Core.Application;

namespace AcgFotos.Base.Application.Dtos
{
    public class TipoLicenciaRolesDto : DtoBase
    {
        public long RolId { get; set; }

        public string RolDescripcion { get; set; }

    }
}
