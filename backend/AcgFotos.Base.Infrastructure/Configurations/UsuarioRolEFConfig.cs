using AcgFotos.Base.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AcgFotos.Base.Infrastructure.Configurations {
    public class UsuarioRolEFConfig : IEntityTypeConfiguration<UsuarioRol> {
        public void Configure(EntityTypeBuilder<UsuarioRol> builder) {

            builder.ToTable("gen_UsuarioRoles");

            builder.HasKey(c => c.Id);
            builder.Property(p => p.RolId)
                .IsRequired();

            builder.HasOne(p => p.Usuario)
                   .WithMany(b => b.Roles)
                   .HasForeignKey(b => b.UsuarioId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => new { x.UsuarioId, x.RolId })
                .IsUnique()
                .HasName("uk_usuario_rol");
        }
    }
}
