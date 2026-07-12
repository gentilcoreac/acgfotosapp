using System.Text;
using System.Text.Json;
using AcgFotos.Core.Security;
using Xunit;

namespace AcgFotos.Api.IntegrationTests.Auth
{
    /// <summary>
    /// AUTH-84..88 — cookie-overlay de impersonalizacion (ADR-0002). Firma HMAC-SHA256 sobre
    /// base64url(json)."."base64url(hmac). Logica pura, sin I/O. Protege contra que el front
    /// manipule tenant/usuario o que un overlay sea eterno.
    /// </summary>
    public class ImpersonationCookieTests
    {
        private const string Key = "clave-de-firma-de-pruebas-1234567890";

        private static ImpersonationTicket NewTicket(long tenant = 2, long user = 10, long by = 1, int minutesAhead = 30)
            => new(tenant, user, by, DateTimeOffset.UtcNow.AddMinutes(minutesAhead));

        [Fact] // AUTH-84
        public void Protect_Unprotect_round_trip_preserva_el_ticket()
        {
            var ticket = NewTicket();

            var cookie = ImpersonationCookie.Protect(ticket, Key);
            var back = ImpersonationCookie.Unprotect(cookie, Key);

            Assert.NotNull(back);
            Assert.Equal(ticket.TenantId, back!.TenantId);
            Assert.Equal(ticket.UserId, back.UserId);
            Assert.Equal(ticket.By, back.By);
            // exp se serializa a segundos unix -> comparar truncado a segundos.
            Assert.Equal(ticket.ExpiresAt.ToUnixTimeSeconds(), back.ExpiresAt.ToUnixTimeSeconds());
        }

        [Fact] // AUTH-85
        public void Unprotect_rechaza_payload_manipulado_conservando_la_firma()
        {
            var cookie = ImpersonationCookie.Protect(NewTicket(user: 10), Key);
            var parts = cookie.Split('.');

            // Reescribe el payload (cambia el usuario a 999) pero conserva la firma original -> mismatch.
            var tamperedPayload = JsonSerializer.SerializeToUtf8Bytes(new { t = 2L, u = 999L, by = 1L, exp = DateTimeOffset.UtcNow.AddMinutes(30).ToUnixTimeSeconds() });
            var tampered = ToBase64Url(tamperedPayload) + "." + parts[1];

            Assert.Null(ImpersonationCookie.Unprotect(tampered, Key));
        }

        [Fact] // AUTH-86
        public void Unprotect_rechaza_ticket_expirado()
        {
            var cookie = ImpersonationCookie.Protect(NewTicket(minutesAhead: -1), Key);

            Assert.Null(ImpersonationCookie.Unprotect(cookie, Key));
        }

        [Theory] // AUTH-87
        [InlineData(null)]
        [InlineData("")]
        [InlineData("sin-punto")]
        [InlineData("@@@.@@@")]            // base64 invalido
        [InlineData("Zm9v.Zm9v")]         // firma no valida + json corrupto ("foo")
        public void Unprotect_rechaza_formatos_invalidos_sin_excepcion(string? value)
        {
            Assert.Null(ImpersonationCookie.Unprotect(value!, Key));
        }

        [Fact] // AUTH-88
        public void Unprotect_con_otra_clave_es_rechazado()
        {
            var cookie = ImpersonationCookie.Protect(NewTicket(), Key);

            Assert.Null(ImpersonationCookie.Unprotect(cookie, "OTRA-CLAVE-distinta-9876543210"));
        }

        private static string ToBase64Url(byte[] bytes)
            => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}
