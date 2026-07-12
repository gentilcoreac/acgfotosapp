using System.Collections.Generic;
using AcgFotos.Core.Application;

namespace AcgFotos.Base.Application.Dtos
{
    /// <summary>
    /// EsDefaultParaNuevoTenant no se incluye a propósito (admin-only, endpoint dedicado
    /// POST /tipos-licencia/{id}/set-default-tenant). Los roles viajan como ids; la
    /// reconciliación la hace TipoLicenciaAppService.SyncCollections.
    /// </summary>
    public class TipoLicenciaInputDto : DtoBase
    {
        public string CodigoTipoLicencia { get; set; }

        public string Descripcion { get; set; }

        public List<long> RolIds { get; set; }

        public TipoLicenciaInputDto()
        {
            this.RolIds = new List<long>();
        }
    }
}
