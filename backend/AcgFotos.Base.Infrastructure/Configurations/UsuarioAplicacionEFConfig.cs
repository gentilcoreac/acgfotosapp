using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AcgFotos.Base.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace AcgFotos.Base.Infrastructure.Configurations
{
    public class UsuarioAplicacionEFConfig : IEntityTypeConfiguration<UsuarioAplicacion>
    {
        public void Configure(EntityTypeBuilder<UsuarioAplicacion> builder)
        {
            builder.HasKey(e => e.Id);
            builder.ToTable("gen_UsuarioAplicaciones");

            builder.Property(x => x.AplicacionId).IsRequired();
            builder.HasIndex(x => x.AplicacionId).IsUnique(false);

            builder.Property(x => x.UsuarioId).IsRequired();
            builder.HasIndex(x => x.UsuarioId).IsUnique(false);

            builder.HasOne(p => p.Usuario)
                .WithMany(b => b.UsuarioAplicaciones)
                .HasForeignKey(b => b.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(p => p.Aplicacion)
                   .WithMany(b => b.UsuarioAplicaciones)
                   .HasForeignKey(b => b.AplicacionId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
