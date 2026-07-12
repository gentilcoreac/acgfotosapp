using AcgFotos.Core.Domain;

namespace AcgFotos.Base.Domain.Entities {
    public class GrupoRol : MultiTenantEntityBase {

        public long GrupoId { get; set; }
        public Grupo Grupo { get; set; }

        public long RolId { get; set; }
        public Rol Rol { get; set; }
    }
}
