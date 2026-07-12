using System;
using System.Threading.Tasks;
using AcgFotos.Base.Domain.Entities;
using AcgFotos.Core.Data;

namespace AcgFotos.Base.Infrastructure.Repositories
{
    public interface IRefreshTokenRepository : IEntityBaseRepository<RefreshToken>
    {
        /// <summary>
        /// Refresh token por su hash, sin filtro de tenant (en /auth/refresh el contexto es
        /// anónimo y el hash es único global). Tracked. Null si no existe.
        /// </summary>
        Task<RefreshToken> GetByHashAsync(string tokenHash);

        /// <summary>
        /// Revoca todos los refresh activos del usuario (cambio de password, replay, logout-all).
        /// </summary>
        Task RevokeAllActiveForUserAsync(long userId, string reason);

        /// <summary>
        /// Borra refresh tokens expirados o revocados antes de <paramref name="threshold"/>
        /// (purga). Devuelve la cantidad de filas eliminadas.
        /// </summary>
        Task<int> DeleteOlderThanAsync(DateTime threshold);
    }
}
