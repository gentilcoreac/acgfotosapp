namespace AcgFotos.Core.Domain
{
    public interface IMultiTenantEntityBase : IEntityBase
    {
        public long TenantId { get; set; }
    }
}
