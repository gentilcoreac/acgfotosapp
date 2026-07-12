using AcgFotos.Core.Application;
using System.Collections.Generic;

namespace AcgFotos.Base.Application.Dtos {
    public class GrupoDto : DtoBase {
        public string Nombre { get; set; }

        public ICollection<UsuarioGrupoDto> UsuarioGrupos { get; set; }

        public ICollection<GrupoRolDto> GrupoRoles { get; set; }
    }
}
