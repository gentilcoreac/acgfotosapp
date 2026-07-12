using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AcgFotos.Base.Domain.Entities;

namespace AcgFotos.Base.Infrastructure.Configurations
{
    /// <summary>
    /// Mapea <see cref="AuthzVersion"/> a la tabla de 1 fila <c>gen_AuthzVersion</c>. El Id es fijo
    /// (no autogenerado) y se siembra la única fila (Id=1, Version=0) con <c>HasData</c> para que exista
    /// desde la migración. El incremento lo hace <c>AcgFotosDbContext.SaveChanges</c> de forma transaccional
    /// (ver ADR-0003 §6.3); el read-side vive en <c>AcgFotos.Core</c> (IAuthzVersion, SQL crudo).
    /// </summary>
    public class AuthzVersionEFConfig : IEntityTypeConfiguration<AuthzVersion>
    {
        public void Configure(EntityTypeBuilder<AuthzVersion> builder)
        {
            builder.ToTable("gen_AuthzVersion");

            builder.HasKey(c => c.Id);
            builder.Property(c => c.Id).ValueGeneratedNever();
            builder.Property(c => c.Version).IsRequired();

            builder.HasData(new AuthzVersion { Id = 1, Version = 0 });
        }
    }
}
