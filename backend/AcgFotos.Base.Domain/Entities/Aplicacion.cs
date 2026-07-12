using AcgFotos.Core.Domain;
using System.Collections.Generic;

namespace AcgFotos.Base.Domain.Entities {
    public class Aplicacion : EntityBase {
        public string Codigo { get; set; }
        public string Nombre { get; set; }
        public bool Activo { get; set; }
        public string Icono { get; set; }
        public string IconoUrl { get; set; }

        public ICollection<TenantAplicacion> TenantAplicaciones { get; set; }
        public ICollection<UsuarioAplicacion> UsuarioAplicaciones { get; set; }

        public Aplicacion() {
            this.TenantAplicaciones = new List<TenantAplicacion>();
            this.UsuarioAplicaciones = new List<UsuarioAplicacion>();
        }
    }
}
