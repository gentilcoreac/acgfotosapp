using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AcgFotos.Core.Application;
using AcgFotos.Core.Domain;

namespace AcgFotos.Base.Application.Dtos
{
    public class UsuarioTipoLicenciaDto : DtoBase
    {
        public long UsuarioId { get; set; }

        public long TipoLicenciaId { get; set; }

        public bool IsActive { get; set; }
        
    }
}
