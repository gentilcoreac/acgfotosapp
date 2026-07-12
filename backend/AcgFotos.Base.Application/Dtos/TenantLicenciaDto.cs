using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AcgFotos.Core.Application;

namespace AcgFotos.Base.Application.Dtos
{
    public class TenantLicenciaDto : DtoBase
    {

        public long TenantId { get; set; }

        public long TipoLicenciaId { get; set; }

        public long Cantidad { get; set; }

        public DateTime ModifiedDateTime { get; set; }

        public DateTime StartDateTime { get; set; }

        public DateTime ExpireDateTime { get; set; }



    }
}
