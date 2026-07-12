using System.Text.RegularExpressions;
using AcgFotos.Core.Security;
using Xunit;

namespace AcgFotos.Api.IntegrationTests.Auth
{
    /// <summary>
    /// AUTH-40 / AUTH-41 — hashing y generacion de refresh tokens (logica pura, sin I/O).
    /// Protege que la DB nunca guarde el token reutilizable y que los tokens sean impredecibles.
    /// </summary>
    public class RefreshTokenCryptoTests
    {
        [Fact] // AUTH-40
        public void Hash_es_SHA256_determinista_en_hex_minuscula_de_64_chars()
        {
            const string raw = "un-refresh-token-cualquiera";

            var h1 = RefreshTokenCrypto.Hash(raw);
            var h2 = RefreshTokenCrypto.Hash(raw);

            Assert.Equal(h1, h2);                                   // determinista
            Assert.Equal(64, h1.Length);                            // SHA-256 = 32 bytes = 64 hex
            Assert.Matches("^[0-9a-f]{64}$", h1);                   // hex minuscula
        }

        [Fact] // AUTH-40
        public void Hash_de_entradas_distintas_difiere()
        {
            Assert.NotEqual(RefreshTokenCrypto.Hash("a"), RefreshTokenCrypto.Hash("b"));
        }

        [Fact] // AUTH-41
        public void GenerateRawToken_es_unico_de_alta_entropia_y_url_safe()
        {
            var tokens = new HashSet<string>();
            for (var i = 0; i < 1000; i++)
            {
                var token = RefreshTokenCrypto.GenerateRawToken();

                Assert.True(tokens.Add(token), "colision en GenerateRawToken (no deberia ocurrir)");
                // base64url: sin '+', '/' ni '=' de padding.
                Assert.Matches("^[A-Za-z0-9_-]+$", token);
                // 64 bytes -> 86 chars base64url sin padding.
                Assert.Equal(86, token.Length);
            }
        }
    }
}
