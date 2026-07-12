using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AcgFotos.Core.Domain;

namespace AcgFotos.Base.Domain.Entities
{
    public class TipoLicenciaRoles : EntityBase
    {

        public long RolId { get; set; }

        public Rol Rol { get; set; }

        public long TipoLicenciaId { get; set; }

        public TipoLicencia TipoLicencia { get; set; }

    }
}
