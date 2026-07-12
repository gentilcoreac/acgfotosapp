using System;
using AcgFotos.Core.Domain;

namespace AcgFotos.Base.Domain.Entities
{
    public class TenantLicencia : EntityBase
    {
        public int Cantidad { get; set; }
        public DateTime ModifiedDatetime { get; set; }
        public DateTime StartDatetime { get; set; }
        public DateTime ExpireDatetime { get; set; }
        public long TipoLicenciaId { get; set; }
        public TipoLicencia TipoLicencia { get; set; }
        public Tenant Tenant { get; set; }
        public long TenantId { get; set; }
    }
}
