using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AcgFotos.Base.Domain.Entities;

namespace AcgFotos.Base.Infrastructure.Configurations
{
    public class TenantLicenciaEFConfig : IEntityTypeConfiguration<TenantLicencia>
    {
        public void Configure(EntityTypeBuilder<TenantLicencia> builder)
        {
            builder.HasKey(e => e.Id);

            builder.ToTable("gen_TenantLicencias");

            builder.Property(x => x.TenantId).IsRequired();
            builder.HasIndex(x => x.TenantId).IsUnique(false);
            builder.HasOne(d => d.Tenant).WithMany(e => e.TenantLicenses).HasForeignKey(f => f.TenantId);
        }
    }
}
