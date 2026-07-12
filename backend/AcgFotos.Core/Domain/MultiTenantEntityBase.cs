namespace AcgFotos.Core.Domain
{
    public class MultiTenantEntityBase : EntityBase, IMultiTenantEntityBase
    {
        public long TenantId { get; set; }
    }
}
