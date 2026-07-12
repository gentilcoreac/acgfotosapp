using AcgFotos.Core.Domain;

namespace AcgFotos.Base.Domain.Entities {
    public class UsuarioGrupo : MultiTenantEntityBase {

        public long UsuarioId { get; set; }
        public Usuario Usuario { get; set; }

        public long GrupoId { get; set; }
        public Grupo Grupo { get; set; }
    }
}
