using System.Collections.Generic;
using AcgFotos.Core.Application;

namespace AcgFotos.Base.Application.Dtos
{
    /// <summary>
    /// EsDefaultParaNuevoTenant no se incluye a propósito (admin-only, endpoint dedicado
    /// POST /roles/{id}/set-default-tenant). Los permisos viajan como ids; la reconciliación
    /// la hace RolAppService.SyncCollections.
    /// </summary>
    public class RolInputDto : DtoBase
    {
        public string Descripcion { get; set; }

        public List<long> PermisoIds { get; set; }

        public RolInputDto()
        {
            this.PermisoIds = new List<long>();
        }
    }
}
