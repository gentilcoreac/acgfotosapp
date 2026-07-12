using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AcgFotos.Base.Domain.Entities;

namespace AcgFotos.Base.Infrastructure.Configurations
{
    public class ParametroValorTenantEFConfig : IEntityTypeConfiguration<ParametroValorTenant>
    {
        public void Configure(EntityTypeBuilder<ParametroValorTenant> builder)
        {
            builder.ToTable("gen_ParametrosValoresTenants");

            builder.HasKey(e => e.Id);

            builder.HasIndex(e => new { e.TenantId, e.ParametroId })
                .IsUnique();

            builder.Property(x => x.Valor)
                .IsRequired(false);

            builder.Property(x => x.TenantId)
                .IsRequired();

            builder.Property(x => x.ParametroId)
                .IsRequired();

        }
    }
}
