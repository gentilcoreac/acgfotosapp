using System.IdentityModel.Tokens.Jwt;

namespace AcgFotos.Core.Security
{
    public interface IJwtSecurityTokenFactory
    {
        /// <summary>
        /// Emite un JWT para el usuario. Si <paramref name="impersonatedBy"/> tiene valor, el token
        /// es de <b>impersonalización</b>: scopeado al usuario destino (los claims tenant/userId/isRoot
        /// son del destino) + claim firmado <c>impersonatedBy</c> con el userId real de root (auditoría).
        /// Ver Docs/adrs/0002-impersonalizacion-jwt-scopeado.md.
        /// </summary>
        TokenModel CreateInstance(long userId, string userName, string email, long tenantId, bool isAdmin, string securityStamp, long? impersonatedBy = null);
    }
}
