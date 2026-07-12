using System.Net;
using AcgFotos.Core.Controllers;
using Xunit;

namespace AcgFotos.Api.IntegrationTests.Infrastructure
{
    /// <summary>
    /// Aserciones HTTP reutilizables (extension methods sobre <see cref="HttpResponseMessage"/>). Colapsan
    /// el trío repetido status + lectura del <see cref="ApiErrorResponse"/> + mensaje en una sola línea, y
    /// estandarizan el mensaje de fallo (incluye el cuerpo crudo cuando el status no es el esperado). Es
    /// <c>Assert</c> puro (sin FluentAssertions, por la convención de la suite). Ver
    /// <c>Docs/casos-de-prueba/testing-mejoras-escalabilidad.md</c> (palanca #1).
    /// </summary>
    public static class HttpAssertions
    {
        /// <summary>Afirma 2xx; si no, falla mostrando el status y el cuerpo crudo (para diagnosticar).</summary>
        public static async Task ShouldBeOk(this HttpResponseMessage resp)
        {
            Assert.True(resp.IsSuccessStatusCode,
                $"esperaba 2xx pero fue {(int)resp.StatusCode}: {await resp.Content.ReadAsStringAsync()}");
        }

        /// <summary>
        /// Afirma 400 y, opcionalmente, que el <see cref="ApiErrorResponse.Message"/> sea el esperado.
        /// Devuelve el error parseado para asertar el detalle (<c>Errors</c>) si hace falta.
        /// SUPONE que el cuerpo es un <see cref="ApiErrorResponse"/> (lo parsea SIEMPRE, con o sin mensaje):
        /// para los pocos endpoints que devuelven un body NO estándar (p. ej. <c>string[]</c> crudo) usar
        /// <see cref="ShouldBeStatus"/> en su lugar.
        /// </summary>
        public static async Task<ApiErrorResponse> ShouldBeBadRequest(this HttpResponseMessage resp, string? message = null)
            => await resp.ShouldBeError(HttpStatusCode.BadRequest, message);

        /// <summary>Afirma un status de error concreto y, opcionalmente, el mensaje del <see cref="ApiErrorResponse"/>.</summary>
        public static async Task<ApiErrorResponse> ShouldBeError(
            this HttpResponseMessage resp, HttpStatusCode expected, string? message = null)
        {
            // Leer el cuerpo ANTES del Assert: el string interpolado en el mensaje de fallo evaluaría
            // ReadAsStringAsync() incluso cuando la aserción pasa, consumiendo el stream y rompiendo
            // la lectura posterior de ReadErrorAsync.
            if (resp.StatusCode != expected)
            {
                var raw = await resp.Content.ReadAsStringAsync();
                Assert.True(false, $"esperaba {(int)expected} pero fue {(int)resp.StatusCode}: {raw}");
            }
            var error = await ApiClient.ReadErrorAsync(resp);
            if (message is not null)
            {
                Assert.Equal(message, error.Message);
            }
            return error;
        }

        /// <summary>Afirma un status exacto (sin parsear cuerpo), mostrando el cuerpo crudo si falla.</summary>
        public static async Task ShouldBeStatus(this HttpResponseMessage resp, HttpStatusCode expected)
        {
            Assert.True(resp.StatusCode == expected,
                $"esperaba {(int)expected} pero fue {(int)resp.StatusCode}: {await resp.Content.ReadAsStringAsync()}");
        }
    }
}
