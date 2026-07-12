using AcgFotos.Base.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AcgFotos.Base.Infrastructure.Configurations {
    public class AplicacionEFConfig : IEntityTypeConfiguration<Aplicacion> {
        public void Configure(EntityTypeBuilder<Aplicacion> builder) {

            builder.HasKey(e => e.Id);

            builder.ToTable("gen_Aplicaciones");
            builder.Property(x => x.Nombre).HasMaxLength(100).IsRequired();
            builder.Property(x => x.Codigo).IsRequired();
            builder.Property(x => x.Activo).IsRequired();
            builder.Property(x => x.Icono);
            builder.Property(x => x.IconoUrl);

            builder.HasMany(d => d.TenantAplicaciones).WithOne(e => e.Aplicacion).HasForeignKey(f => f.AplicacionId).OnDelete(DeleteBehavior.Cascade);
        }
    }
}
