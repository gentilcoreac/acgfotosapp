using AcgFotos.Base.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AcgFotos.Base.Infrastructure.Configurations {
    class PermisoEndpointEFConfig : IEntityTypeConfiguration<PermisoEndpoint>
    {
        public void Configure(EntityTypeBuilder<PermisoEndpoint> builder)
        {

            builder.ToTable("gen_PermisoEndpoints");

            builder.HasKey(c => c.Id);
            builder.Property(p => p.PermisoId).IsRequired();
            builder.Property(p => p.EndpointId).IsRequired();

            builder.HasIndex(x => new { x.PermisoId, x.EndpointId })
                .IsUnique().HasName("uk_permisos_endpoints");
        }
    }
}
