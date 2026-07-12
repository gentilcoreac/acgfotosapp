
namespace AcgFotos.Base.Application.Dtos
{
    public class TenantHeaderStyleDto : TenantHeaderDto
    {
        //Colores
        public string ColorPrimarioDark { get; set; }
        public string ColorPrimarioLight { get; set; }
        public bool DarkModeByDefault { get; set; }

        // Estilo
        public string LogoLoginLightUrl { get; set; }
        public string LogoLoginDarkUrl { get; set; }
        public string LogoHeaderLightUrl { get; set; }
        public string LogoHeaderDarkUrl { get; set; }
        public string FaviconUrl { get; set; }

        //Backgrounds
        public string ImagenFondoLoginLightUrl { get; set; }
        public string ImagenFondoLoginDarkUrl { get; set; }

        //CSS
        public string StyleSheetCssUrl { get; set; }

        //Layout
        public int TipoLayoutLogin { get; set; }
    }
}
