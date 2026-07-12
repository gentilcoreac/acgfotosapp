using AcgFotos.Core.Domain;
using System.Collections.Generic;

namespace AcgFotos.Base.Domain.Entities {
    public class Grupo : MultiTenantEntityBase {
        public string Nombre { get; set; }

        public ICollection<UsuarioGrupo> UsuarioGrupos { get; set; }
        public ICollection<GrupoRol> GrupoRoles { get; set; }

        public Grupo() {
            this.UsuarioGrupos = new List<UsuarioGrupo>();
            this.GrupoRoles = new List<GrupoRol>();
        }
    }
}
