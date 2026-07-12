using System;
using AcgFotos.Core.Application;

namespace AcgFotos.Base.Application.Dtos
{
    public class UsuarioHeaderDto : HeaderDtoBase
    {
        public string UserName { get; set; }
        public string Nombre { get; set; }
        public string Apellido { get; set; }
        public string Email { get; set; }
        public bool Bloqueado { get; set; }
        public DateTime? UltimoLogin { get; set; }

        public UsuarioTipoLicenciaDto TipoLicenciaActiva { get; set; }
    }
}
