using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AcgFotos.Base.Domain.Entities;

namespace AcgFotos.Base.Infrastructure.Configurations
{
    internal class TipoLicenciaRolesEFConfig : IEntityTypeConfiguration<TipoLicenciaRoles>
    {
        public void Configure(EntityTypeBuilder<TipoLicenciaRoles> builder)
        {
            builder.ToTable("gen_TipoLicenciaRoles");

            builder.HasKey(c => c.Id);
            builder.Property(p => p.RolId).IsRequired();
            builder.Property(p => p.TipoLicenciaId).IsRequired();

            builder.HasOne(r => r.TipoLicencia).WithMany(b => b.TipoLicenciaRoles).HasForeignKey(b => b.TipoLicenciaId).OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(r => r.Rol).WithMany(b => b.TipoLicenciaRoles).HasForeignKey(b => b.RolId).OnDelete(DeleteBehavior.Cascade);
        }
    }
}
