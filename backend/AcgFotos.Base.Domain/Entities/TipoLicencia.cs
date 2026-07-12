using System.Collections.Generic;
using AcgFotos.Core.Domain;

namespace AcgFotos.Base.Domain.Entities
{
    public class TipoLicencia : EntityBase
    {
        public string Descripcion { get; set; }

        public string CodigoTipoLicencia { get; set; }

        public bool EsDefaultParaNuevoTenant { get; set; }

        public ICollection<TipoLicenciaRoles> TipoLicenciaRoles { get; set; }

        public ICollection<UsuarioTipoLicencia> UsuarioTipoLicencia { get; set; }

        public ICollection<TenantLicencia> TenantLicenses { get; set; }

        public TipoLicencia()
        {
            this.TipoLicenciaRoles = new List<TipoLicenciaRoles>();
            this.UsuarioTipoLicencia = new List<UsuarioTipoLicencia>();
            this.TenantLicenses = new List<TenantLicencia>();
        }


    }
}
