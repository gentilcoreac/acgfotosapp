using AcgFotos.Base.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AcgFotos.Base.Infrastructure.Configurations {
    public class RolEFConfig : IEntityTypeConfiguration<Rol> {
        public void Configure(EntityTypeBuilder<Rol> builder) {

            builder.HasKey(e => e.Id);

            builder.ToTable("gen_Roles");
            builder.Property(x => x.Descripcion).HasMaxLength(100).IsRequired();

            builder.HasMany(d => d.RolPermisos).WithOne(e => e.Rol).HasForeignKey(f => f.RolId).OnDelete(DeleteBehavior.Cascade);

        }
    }
}
