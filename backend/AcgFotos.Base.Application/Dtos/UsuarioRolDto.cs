using System;
using System.Collections.Generic;
using AcgFotos.Core.Application;

namespace AcgFotos.Base.Application.Dtos {
    public class UsuarioRolDto : DtoBase {

        public long UsuarioId { get; set; }

        public long RolId { get; set; }
        public string RolDescripcion { get; set; }
    }
}