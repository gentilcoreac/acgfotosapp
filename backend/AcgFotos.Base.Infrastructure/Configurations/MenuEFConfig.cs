using AcgFotos.Base.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AcgFotos.Base.Infrastructure.Configurations {
    public class MenuEFConfig : IEntityTypeConfiguration<Menu> {
        public void Configure(EntityTypeBuilder<Menu> builder) {

            builder.HasKey(e => e.Id);
            
            builder.ToTable("gen_Menus");

            builder.Property(x => x.Nombre).HasMaxLength(50).IsRequired();
            builder.Property(x => x.Codigo).HasMaxLength(50).IsRequired();
            builder.Property(x => x.Estado).IsRequired();
            builder.Property(x => x.ImagenWeb).HasMaxLength(100);

            builder.Property(x => x.VisibleSideMenu)
                .IsRequired();

            builder.Property(x => x.VisibleDash)
                .IsRequired();

            builder.Property(x => x.Orden).IsRequired();

            builder.HasOne(x => x.MenuPadre)
                .WithMany(d => d.MenuHijos).HasForeignKey(f => f.MenuPadreId).OnDelete(DeleteBehavior.ClientNoAction);

            builder.Property(x => x.PermisoId);

            builder.HasIndex(x => x.Codigo).IsUnique().HasName("uk_menu_codigo");

            builder.Property(x => x.AplicacionId);
            builder.HasIndex(u => u.AplicacionId).IsUnique(false);

            builder.Property(x => x.RoutePath).IsRequired(false);
        }
    }
}
