using AcgFotos.Core.Domain;

namespace AcgFotos.Base.Domain.Entities {
    public class TenantAplicacion : EntityBase
    {
        public long AplicacionId { get; set; }
        public Aplicacion Aplicacion { get; set; }
        public long TenantId { get; set; }
        public Tenant Tenant { get; set; }
    }
}
