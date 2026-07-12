using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace AcgFotos.Core.Security
{
    /// <summary>
    /// Datos de una sesión de impersonalización (ADR-0002): tenant + usuario destino, el userId real de
    /// root que la inició (<see cref="By"/>) y su expiración.
    /// </summary>
    public sealed record ImpersonationTicket(long TenantId, long UserId, long By, DateTimeOffset ExpiresAt);

    /// <summary>
    /// Cookie-overlay de impersonalización (ADR-0002). Sostiene la impersonalización a través de los
    /// refresh: <c>impersonate</c> la setea, <c>refresh</c> la lee para re-emitir el token scopeado y
    /// <c>stop-impersonation</c> la borra. Se firma con HMAC-SHA256 (clave = la del JWT) para que el
    /// front no pueda manipular tenant/usuario. Formato: <c>base64url(json) + "." + base64url(hmac)</c>.
    /// </summary>
    public static class ImpersonationCookie
    {
        /// <summary>Serializa y firma el ticket. El resultado va en una cookie HttpOnly.</summary>
        public static string Protect(ImpersonationTicket ticket, string key)
        {
            var payload = JsonSerializer.SerializeToUtf8Bytes(new TicketDto
            {
                t = ticket.TenantId,
                u = ticket.UserId,
                by = ticket.By,
                exp = ticket.ExpiresAt.ToUnixTimeSeconds(),
            });
            var signature = Sign(payload, key);
            return ToBase64Url(payload) + "." + ToBase64Url(signature);
        }

        /// <summary>
        /// Verifica la firma y la expiración. Devuelve el ticket o <c>null</c> si la cookie es inválida,
        /// fue manipulada o ya expiró.
        /// </summary>
        public static ImpersonationTicket Unprotect(string value, string key)
        {
            if (string.IsNullOrEmpty(value))
            {
                return null;
            }

            var parts = value.Split('.');
            if (parts.Length != 2)
            {
                return null;
            }

            byte[] payload;
            byte[] signature;
            try
            {
                payload = FromBase64Url(parts[0]);
                signature = FromBase64Url(parts[1]);
            }
            catch (FormatException)
            {
                return null;
            }

            // Comparación en tiempo constante para no filtrar info por timing.
            if (!CryptographicOperations.FixedTimeEquals(signature, Sign(payload, key)))
            {
                return null;
            }

            TicketDto dto;
            try
            {
                dto = JsonSerializer.Deserialize<TicketDto>(payload);
            }
            catch (JsonException)
            {
                return null;
            }

            if (dto == null)
            {
                return null;
            }

            var expiresAt = DateTimeOffset.FromUnixTimeSeconds(dto.exp);
            if (DateTimeOffset.UtcNow >= expiresAt)
            {
                return null;
            }

            return new ImpersonationTicket(dto.t, dto.u, dto.by, expiresAt);
        }

        private static byte[] Sign(byte[] payload, string key)
            => HMACSHA256.HashData(Encoding.UTF8.GetBytes(key), payload);

        private static string ToBase64Url(byte[] bytes)
            => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

        private static byte[] FromBase64Url(string value)
        {
            var base64 = value.Replace('-', '+').Replace('_', '/');
            switch (base64.Length % 4)
            {
                case 2: base64 += "=="; break;
                case 3: base64 += "="; break;
            }
            return Convert.FromBase64String(base64);
        }

        // Nombres cortos a propósito (la cookie viaja en cada request al /api/auth).
        private sealed class TicketDto
        {
            public long t { get; set; }
            public long u { get; set; }
            public long by { get; set; }
            public long exp { get; set; }
        }
    }
}
