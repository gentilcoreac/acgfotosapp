using AcgFotos.Core.Domain;

namespace AcgFotos.Base.Domain.Entities {
    public class UsuarioRol : MultiTenantEntityBase {

        public long UsuarioId { get; set; }
        public Usuario Usuario { get; set; }

        public long RolId { get; set; }
        public Rol Rol { get; set; }
    }
}
