using System;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace AcgFotos.Core.Security
{
    /// <summary>
    /// Generación y hashing de refresh tokens. El valor en claro se entrega una sola vez
    /// (cookie HttpOnly del cliente); en la base solo se persiste el hash SHA-256, de modo
    /// que un compromiso de la DB no permita reusar los tokens existentes.
    /// </summary>
    public static class RefreshTokenCrypto
    {
        /// <summary>Genera un refresh token aleatorio (512 bits) en base64url.</summary>
        public static string GenerateRawToken()
        {
            var bytes = RandomNumberGenerator.GetBytes(64);
            return Base64UrlEncoder.Encode(bytes);
        }

        /// <summary>SHA-256 del token en claro, en hex minúscula (para guardar/buscar en DB).</summary>
        public static string Hash(string rawToken)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }
    }
}
