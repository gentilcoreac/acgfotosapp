using System.Collections.Generic;
using AcgFotos.Core.Application;

namespace AcgFotos.Base.Application.Dtos
{
    /// <summary>
    /// Los miembros viajan como ids (UsuarioIds); la reconciliación de la colección
    /// gen_UsuarioGrupos la hace GrupoAppService.SyncCollections (ChildCollectionSync.SyncBy).
    /// TenantId no se expone: lo asigna SetAppContextTenant en runtime.
    /// </summary>
    public class GrupoInputDto : DtoBase
    {
        public string Nombre { get; set; }

        public List<long> UsuarioIds { get; set; }

        /// <summary>Ids de los roles que otorga el grupo (sync explícito por GrupoAppService.SyncCollections).</summary>
        public List<long> RolIds { get; set; }

        public GrupoInputDto()
        {
            this.UsuarioIds = new List<long>();
            this.RolIds = new List<long>();
        }
    }
}
