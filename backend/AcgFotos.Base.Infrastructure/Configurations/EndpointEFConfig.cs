using AcgFotos.Base.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AcgFotos.Base.Infrastructure.Configurations {
    class EndpointEFConfig : IEntityTypeConfiguration<Endpoint>
    {
        public void Configure(EntityTypeBuilder<Endpoint> builder)
        {

            builder.HasKey(e => e.Id);

            builder.ToTable("gen_EndPoints");
            builder.Property(x => x.ModuleName).HasMaxLength(100).IsRequired();
            builder.Property(x => x.ControllerName).HasMaxLength(100).IsRequired();
            builder.Property(x => x.ActionName).HasMaxLength(100).IsRequired();
            builder.Property(x => x.HttpMethod).HasMaxLength(10);
            builder.Property(x => x.Route).HasMaxLength(500);

            builder.HasIndex(e => new { e.Route, e.HttpMethod })
                .IsUnique()
                .HasDatabaseName("uk_endpoint_route_method");
        }
    }
}
