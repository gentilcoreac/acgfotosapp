using AcgFotos.Core.Domain;

namespace AcgFotos.Base.Domain.Entities {
    public class RolPermiso : EntityBase {
        public long RolId { get; set; }
        public Rol Rol { get; set; }

        public long PermisoId { get; set; }
        public Permiso Permiso { get; set; }
    }
}
