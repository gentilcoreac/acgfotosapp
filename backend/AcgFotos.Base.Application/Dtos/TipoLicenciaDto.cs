using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AcgFotos.Core.Application;
using AcgFotos.Base.Domain.Entities;

namespace AcgFotos.Base.Application.Dtos
{
    public class TipoLicenciaDto : DtoBase
    {
        public string Descripcion { get; set; }

        public string CodigoTipoLicencia { get; set; }

        public bool EsDefaultParaNuevoTenant { get; set; }

        public ICollection<TipoLicenciaRolesDto> TipoLicenciaRoles { get; set; }

    }
}
