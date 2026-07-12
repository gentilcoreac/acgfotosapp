using System;
using AcgFotos.Core.Domain;

namespace AcgFotos.Base.Domain.Entities
{
    public class UsuarioTipoLicencia : MultiTenantEntityBase
    {
        public bool IsActive { get; set; }
        public long UsuarioId { get; set; }
        public Usuario Usuario { get; set; }

        public long TipoLicenciaId { get; set; }

        public TipoLicencia TipoLicencia { get; set; }
        public DateTime CreatedDatetime { get; set; }
    }
}
