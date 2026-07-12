using System.Collections.Generic;

namespace AcgFotos.Base.Application.Dtos
{
    /// <summary>
    /// Rol que el tenant puede otorgar por grupo (endpoint <c>roles/del-tenant</c>), con la(s)
    /// licencia(s) del tenant que lo incluyen para los chips del selector del ABM de grupos (§11.1).
    /// </summary>
    public class RolDelTenantDto
    {
        public long Id { get; set; }
        public string Descripcion { get; set; }
        public List<TipoLicenciaTagDto> Licencias { get; set; } = new();
    }

    /// <summary>Etiqueta mínima de una licencia (id + descripción) para los chips.</summary>
    public class TipoLicenciaTagDto
    {
        public long Id { get; set; }
        public string Descripcion { get; set; }
    }
}
