using AcgFotos.Base.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AcgFotos.Base.Infrastructure.Configurations {
    public class ParametroEFConfig : IEntityTypeConfiguration<Parametro> {
        public void Configure(EntityTypeBuilder<Parametro> builder) {

            builder.HasKey(e => e.Id);

            builder.ToTable("gen_Parametros");

            builder.Property(x => x.Nombre)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(x => x.Valor)
                .IsRequired(false);

            builder.Property(x => x.Descripcion)
                .HasMaxLength(500)
                .IsRequired();

            builder.Property(x => x.AplicacionId)
                .IsRequired();

            builder.Property(x => x.TipoDato)
                .IsRequired();

            builder.Property(x => x.PermisoId)
                .IsRequired(false);

            builder.HasOne(x => x.Aplicacion)
                .WithMany()
                .HasForeignKey(x => x.AplicacionId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Permiso)
                .WithMany()
                .HasForeignKey(x => x.PermisoId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
