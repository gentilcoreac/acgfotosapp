using System.Collections.Generic;

namespace AcgFotos.Base.Infrastructure.Repositories.Projections
{
    /// <summary>
    /// Rol licenciado por el tenant (lo usa el ABM de grupos) junto con la(s) licencia(s) del tenant
    /// que lo incluyen, para mostrar chips de licencia por rol (§11.1). Un rol puede estar en varias
    /// licencias; sólo se listan las que el tenant tiene contratadas (las que lo hacen elegible).
    /// </summary>
    public class RolDelTenantProjection
    {
        public long Id { get; set; }
        public string Descripcion { get; set; }
        public List<TipoLicenciaTagProjection> Licencias { get; set; }
    }

    public class TipoLicenciaTagProjection
    {
        public long Id { get; set; }
        public string Descripcion { get; set; }
    }
}
