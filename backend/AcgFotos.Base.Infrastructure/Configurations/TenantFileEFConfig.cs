using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AcgFotos.Base.Domain.Entities;

namespace AcgFotos.Base.Infrastructure.Configurations
{
    public class TenantFileEFConfig : IEntityTypeConfiguration<TenantFile>
    {
        public void Configure(EntityTypeBuilder<TenantFile> builder)
        {
            builder.HasKey(e => e.Id);

            builder.ToTable("gen_TenantFiles");

            builder.Property(x => x.TenantId).IsRequired();
            builder.HasIndex(x => x.TenantId).IsUnique(false);

            builder.Property(x => x.FileName).IsRequired();
            builder.Property(x => x.StorageKey).IsRequired();
            builder.Property(x => x.Visibility).HasConversion<int>();

            // FK a Tenant por TenantId, sin navigations en las entidades (el filtro global
            // por TenantId ya aísla; los archivos se consultan por su propio repo).
            builder.HasOne<Tenant>().WithMany().HasForeignKey(f => f.TenantId);
        }
    }
}
