using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AcgFotos.Base.Domain.Entities;

namespace AcgFotos.Base.Infrastructure.Configurations
{
    public class TipoLicenciaEFConfig : IEntityTypeConfiguration<TipoLicencia>
    {
        public void Configure(EntityTypeBuilder<TipoLicencia> builder)
        {
            builder.HasKey(e => e.Id);

            builder.ToTable("gen_TipoLicencia");
            builder.Property(x => x.Descripcion).HasMaxLength(100).IsRequired();

            builder.HasMany(d => d.TipoLicenciaRoles).WithOne(e => e.TipoLicencia).HasForeignKey(f => f.TipoLicenciaId).OnDelete(DeleteBehavior.Cascade);
            builder.HasMany(d => d.UsuarioTipoLicencia).WithOne(e => e.TipoLicencia).HasForeignKey(f => f.TipoLicenciaId).OnDelete(DeleteBehavior.Cascade);
        }
    }
}
