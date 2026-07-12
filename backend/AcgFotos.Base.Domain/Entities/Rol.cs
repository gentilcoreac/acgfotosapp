using AcgFotos.Core.Domain;
using System.Collections.Generic;

namespace AcgFotos.Base.Domain.Entities {
    public class Rol : EntityBase {
        public string Descripcion { get; set; }

        public bool EsDefaultParaNuevoTenant { get; set; }

        public ICollection<RolPermiso> RolPermisos { get; set; }

        public ICollection<TipoLicenciaRoles> TipoLicenciaRoles { get; set; }

        public Rol() {
            this.RolPermisos = new List<RolPermiso>();
            this.TipoLicenciaRoles = new List<TipoLicenciaRoles>();
        }
    }
}
