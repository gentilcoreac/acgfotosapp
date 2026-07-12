using Microsoft.AspNetCore.Identity;
using AcgFotos.Core.Domain;
using System;
using System.Collections.Generic;

namespace AcgFotos.Base.Domain.Entities
{

    public class Usuario : IdentityUser<long>, IMultiTenantEntityBase
    {
        public long TenantId { get; set; }
        public string Nombre { get; set; }
        public string Apellido { get; set; }

        public long? Telefono { get; set; }
        public byte[] ProfilePicture { get; set; }
        public bool Administrador { get; set; }

        public string EmailConfirmationToken { get; set; }
        public DateTime TokenExpirationDate { get; set; }

        public DateTime DateCreated { get; set; }
        public DateTime FechaCambioClave { get; set; }
        public ICollection<UsuarioRol> Roles { get; set; }
        public ICollection<UsuarioAplicacion> UsuarioAplicaciones { get; set; }
        public ICollection<UsuarioGrupo> UsuarioGrupos { get; set; }

        //Se comenta porque sino, cada vez que queres actualizar la entidad Usuario, tendríamos que haber llenado esa collección con todos los registros del historial para que EF no los borre
        //public ICollection<UsuarioTipoLicencia> UsuarioTipoLicencias { get; set; }

        public Usuario()
        {
            this.Roles = new List<UsuarioRol>();
            this.UsuarioAplicaciones = new List<UsuarioAplicacion>();
            this.UsuarioGrupos = new List<UsuarioGrupo>();
        }
    }
}
