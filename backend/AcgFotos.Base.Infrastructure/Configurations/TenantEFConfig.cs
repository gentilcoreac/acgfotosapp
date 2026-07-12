using AcgFotos.Base.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AcgFotos.Base.Infrastructure.Configurations
{
    public class TenantEFConfig : IEntityTypeConfiguration<Tenant>
    {
        public void Configure(EntityTypeBuilder<Tenant> builder)
        {

            builder.HasKey(e => e.Id);

            builder.ToTable("gen_Tenants");
            //builder.Property(x => x.Descripcion).HasMaxLength(100).IsRequired();

            builder.Property(x => x.Codigo).HasMaxLength(100).IsRequired();
            builder.HasIndex(x => x.Codigo).IsUnique();
            builder.Property(x => x.Nombre).HasMaxLength(100).IsRequired();

            builder.Property(x => x.DarkModeByDefault).IsRequired();
            builder.Property(x => x.TipoLayoutLogin).IsRequired();

            //builder.Property(x => x.EmailTo).HasMaxLength(100).IsRequired();
            //builder.Property(x => x.ColorPrimario).HasMaxLength(100).IsRequired();

            //builder.HasMany(d => d.TenantAplicaciones).WithOne(e => e.Tenant).HasForeignKey(f => f.TenantId).OnDelete(DeleteBehavior.Cascade);

            //builder.HasOne(x => x.LogoHeaderDark).WithOne().HasForeignKey<Tenant>(p => p.LogoHeaderDarkId).OnDelete(DeleteBehavior.Restrict);
            //builder.HasOne(x => x.LogoHeaderLight).WithOne().HasForeignKey<Tenant>(p => p.LogoHeaderLightId).OnDelete(DeleteBehavior.Restrict);
            //builder.HasOne(x => x.LogoLoginDark).WithOne().HasForeignKey<Tenant>(p => p.LogoLoginDarkId).OnDelete(DeleteBehavior.Restrict);
            //builder.HasOne(x => x.LogoLoginLight).WithOne().HasForeignKey<Tenant>(p => p.LogoLoginLightId).OnDelete(DeleteBehavior.Restrict);
            //builder.HasOne(x => x.Favicon).WithOne().HasForeignKey<Tenant>(p => p.FaviconId).OnDelete(DeleteBehavior.Restrict);

        }
    }
}
