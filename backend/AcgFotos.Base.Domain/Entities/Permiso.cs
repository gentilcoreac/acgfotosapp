using AcgFotos.Core.Domain;
using System.Collections.Generic;

namespace AcgFotos.Base.Domain.Entities
{

    public class Permiso : EntityBase
    {
        public string Nombre { get; set; }
        public string CodigoPermiso { get; set; }
        public string Descripcion { get; set; }

        public bool Activo { get; set; }
        public ICollection<PermisoEndpoint> Endpoints { get; set; }

        public long? PermisoPadreId { get; set; }
        public Permiso PermisoPadre { get; set; }
        public long? AplicacionId { get; set; }
        public Aplicacion Aplicacion { get; set; }
        public ICollection<RolPermiso> RolPermisos { get; set; }
        public ICollection<Permiso> PermisosHijos { get; set; }
        public bool EsRestringido { get; set; }

        public Permiso()
        {
            this.Endpoints = new List<PermisoEndpoint>();
            this.RolPermisos = new List<RolPermiso>();
            this.PermisosHijos = new List<Permiso>();
        }
    }
}
