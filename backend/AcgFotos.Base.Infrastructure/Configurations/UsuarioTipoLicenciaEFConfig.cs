using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AcgFotos.Base.Domain.Entities;

namespace AcgFotos.Base.Infrastructure.Configurations
{
    internal class UsuarioTipoLicenciaEFConfig : IEntityTypeConfiguration<UsuarioTipoLicencia>
    {
        public void Configure(EntityTypeBuilder<UsuarioTipoLicencia> builder)
        {
            builder.ToTable("gen_UsuarioTipoLicencia");

            builder.HasKey(c => c.Id);
            builder.Property(p => p.UsuarioId).IsRequired();
            builder.Property(p => p.TipoLicenciaId).IsRequired();

            builder.HasOne(r => r.TipoLicencia).WithMany(b => b.UsuarioTipoLicencia).HasForeignKey(b => b.TipoLicenciaId);
        }
    }
}
