using AcgFotos.Core.Storage;
using AcgFotos.Core.Theme;
using Xunit;

namespace AcgFotos.Api.IntegrationTests.Tenants
{
    /// <summary>
    /// Tenants (230) — unit puro (sin DB/host) de los helpers que usa la regeneración de themes:
    /// ColorConverter (HEX→HSL, con punto decimal locale-safe) y Base64Helper (limpia el prefijo data-uri).
    /// </summary>
    public class TenantThemeUnitTests
    {
        [Fact] // TEN-67 — ConvertHexToHSL: HEX válido → string HSL con punto decimal (no coma del locale)
        public void ConvertHexToHSL_usa_punto_decimal()
        {
            var model = ColorConverter.ConvertHexToHSL("#009688");

            Assert.StartsWith("hsl(", model.Hsl);
            // El token del hue (antes de "deg") usa punto decimal, no coma → CSS válido en cualquier locale.
            var hueToken = model.Hsl.Substring(4, model.Hsl.IndexOf("deg") - 4);
            Assert.DoesNotContain(",", hueToken);
            Assert.True(model.HslHue > 0);
        }

        [Fact] // TEN-68 — ConvertHexToHSL con un HEX inválido lanza (Unicolour no parsea "no-es-hex"); el flujo de
               // save lo traduce a error vía el middleware (no hay validación previa del formato del color).
        public void ConvertHexToHSL_hex_invalido_lanza()
        {
            Assert.ThrowsAny<System.Exception>(() => ColorConverter.ConvertHexToHSL("no-es-hex"));
        }

        [Theory] // TEN-70 — Base64Helper.Clean descarta el prefijo data-uri; sin prefijo devuelve el string tal cual
        [InlineData("data:image/png;base64,AAAABBBB", "AAAABBBB")]
        [InlineData("AAAABBBB", "AAAABBBB")]
        public void Base64_Clean_descarta_prefijo_data_uri(string input, string expected)
        {
            Assert.Equal(expected, Base64Helper.Clean(input));
        }
    }
}
