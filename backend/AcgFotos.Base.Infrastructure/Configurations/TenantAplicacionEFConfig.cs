using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AcgFotos.Base.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace AcgFotos.Base.Infrastructure.Configurations
{
    public class TenantAplicacionEFConfig : IEntityTypeConfiguration<TenantAplicacion>
    {
        public void Configure(EntityTypeBuilder<TenantAplicacion> builder)
        {
            builder.HasKey(e => e.Id);

            builder.ToTable("gen_TenantAplicaciones");

            builder.Property(x => x.AplicacionId).IsRequired();
            builder.HasIndex(x => x.AplicacionId).IsUnique(false);

            builder.Property(x => x.TenantId).IsRequired();
            builder.HasIndex(x => x.TenantId).IsUnique(false);

            builder.HasOne(p => p.Aplicacion)
                .WithMany(b => b.TenantAplicaciones)
                .HasForeignKey(b => b.AplicacionId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(p => p.Tenant)
                   .WithMany(b => b.TenantAplicaciones)
                   .HasForeignKey(b => b.TenantId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
