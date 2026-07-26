using Microsoft.Extensions.DependencyInjection;
using AcgFotos.Fotos.Application.Imaging;
using AcgFotos.Api.IntegrationTests.Infrastructure;
using Xunit;

namespace AcgFotos.Api.IntegrationTests.Fotos
{
    /// <summary>
    /// Humo del cableado del módulo Fotos: si alguien lo saca de <c>AppModulesName</c> o del
    /// <c>ConfigureContainer</c> del Startup, esto falla antes que cualquier feature.
    /// </summary>
    public class FotosModuleSmokeTests : IntegrationTestBase
    {
        public FotosModuleSmokeTests(TestWebApplicationFactory factory)
            : base(factory)
        {
        }

        [Fact]
        public void El_contenedor_resuelve_el_pipeline_de_imagenes()
        {
            var processor = Factory.Services.GetService<IImageProcessor>();

            Assert.NotNull(processor);
        }

        [Theory] // El AcgFotosDbContext descubre las EF configs del módulo ⇒ la migración creó las tablas.
        [InlineData("fot_Eventos")]
        [InlineData("fot_Grupos")]
        [InlineData("fot_Participantes")]
        [InlineData("fot_Fotos")]
        [InlineData("fot_CodigosAcceso")]
        [InlineData("fot_TamanosPrecios")]
        [InlineData("fot_Pedidos")]
        [InlineData("fot_PedidoItems")]
        public async Task La_tabla_del_vertical_existe(string tabla)
        {
            // Postgres guarda los nombres de tabla en minúscula (UseLowerCaseNamingConvention).
            var count = await CountAsync(
                $"SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = LOWER('{tabla}')");

            Assert.Equal(1, count);
        }
    }
}
