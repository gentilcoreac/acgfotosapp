using AcgFotos.Core.Application;
using System;
using System.Collections.Generic;

namespace AcgFotos.Base.Application.Dtos
{

    public class UsuarioDto :  DtoBase {
        public string UserName { get; set; }
        public string Nombre { get; set; }
        public string Apellido { get; set; }
        public string Email { get; set; }
        public string SecurityStamp { get; set; }
        public long? TenantId { get; set; }
        public long? Telefono { get; set; }
        public byte[] ProfilePicture { get; set; }
        public bool Bloqueado { get; set; }
        public bool Administrador { get; set; }
        public DateTime? UltimoLogin { get; set; }

        public ICollection<UsuarioRolDto> Roles { get; set; }

        public string PhoneNumber { get; set; }

        /// <summary>
        /// Este campo no se puede modificar desde el frontend.
        /// </summary>
        public bool EmailConfirmed { get; set; }

        public ICollection<UsuarioAplicacionDto> UsuarioAplicaciones { get; set; }

        public UsuarioTipoLicenciaDto TipoLicenciaActiva { get; set; }

        public UsuarioDto() {
            this.Roles = new List<UsuarioRolDto>();
            this.UsuarioAplicaciones = new List<UsuarioAplicacionDto>();
        }
    }
}
