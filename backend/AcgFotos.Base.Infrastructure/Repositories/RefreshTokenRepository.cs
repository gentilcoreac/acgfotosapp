using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AcgFotos.Base.Domain.Entities;
using AcgFotos.Core.Data;
using AcgFotos.Core.Session;

namespace AcgFotos.Base.Infrastructure.Repositories
{
    public class RefreshTokenRepository : EntityBaseRepository<RefreshToken>, IRefreshTokenRepository
    {
        public RefreshTokenRepository(IDbContext dbContext, IAppContext appContext)
            : base(dbContext, appContext)
        {
        }

        public Task<RefreshToken> GetByHashAsync(string tokenHash) =>
            this.DbContext.Set<RefreshToken>()
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.TokenHash == tokenHash);

        public Task RevokeAllActiveForUserAsync(long userId, string reason) =>
            this.DbContext.Set<RefreshToken>()
                .IgnoreQueryFilters()
                .Where(x => x.UserId == userId && x.RevokedAt == null)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.RevokedAt, DateTime.UtcNow)
                    .SetProperty(x => x.RevokeReason, reason));

        public Task<int> DeleteOlderThanAsync(DateTime threshold) =>
            this.DbContext.Set<RefreshToken>()
                .IgnoreQueryFilters()
                .Where(x => x.ExpiresAt < threshold || (x.RevokedAt != null && x.RevokedAt < threshold))
                .ExecuteDeleteAsync();
    }
}
