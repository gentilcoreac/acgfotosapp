using AcgFotos.Core.Application;
using System;
using System.Collections.Generic;

namespace AcgFotos.Base.Application.Dtos
{

    public class UsuarioPerfilDto : DtoBase
    {
        public string UserName { get; set; }
        public string Nombre { get; set; }
        public string Apellido { get; set; }
        public string Email { get; set; }
        public byte[] ProfilePicture { get; set; }

        public long? Telefono { get; set; }
        public bool Administrador { get; set; }
        public bool EmailConfirmed { get; set; }
        public bool ClaveVigente { get; set; }
        //public bool UsaCuentas { get; set; }

        // ---
        //public List<UsuarioCuentasDto> UsuarioCuentas { get; set; }
        //public List<UsuarioAplicacionDto> UsuarioAplicaciones { get; set; }
    }
}
