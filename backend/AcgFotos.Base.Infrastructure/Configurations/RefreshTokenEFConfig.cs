using AcgFotos.Base.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AcgFotos.Base.Infrastructure.Configurations
{
    public class RefreshTokenEFConfig : IEntityTypeConfiguration<RefreshToken>
    {
        public void Configure(EntityTypeBuilder<RefreshToken> builder)
        {
            builder.HasKey(e => e.Id);

            builder.ToTable("gen_RefreshTokens");

            builder.Property(x => x.TokenHash).HasMaxLength(128).IsRequired();
            builder.HasIndex(x => x.TokenHash).IsUnique();

            builder.Property(x => x.UserId).IsRequired();
            // Para "tokens activos del usuario" (revoke-all, replay).
            builder.HasIndex(x => new { x.UserId, x.RevokedAt });

            builder.Property(x => x.CreatedAt).IsRequired();
            builder.Property(x => x.ExpiresAt).IsRequired();

            builder.Property(x => x.RevokeReason).HasMaxLength(40);
            builder.Property(x => x.ReplacedByTokenHash).HasMaxLength(128);
            builder.Property(x => x.CreatedByIp).HasMaxLength(64);
            builder.Property(x => x.CreatedByUserAgent).HasMaxLength(512);

            // Propiedad calculada: no se persiste.
            builder.Ignore(x => x.IsActive);
        }
    }
}
