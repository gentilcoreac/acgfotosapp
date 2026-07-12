using AcgFotos.Core.Application;

namespace AcgFotos.Base.Application.Dtos
{
    /// <summary>
    /// DTO utilizado por el ABM del cliente donde el mismo gestiona menos propiedades a modificar.
    /// </summary>
    public class TenantEditByAdminClientInput: DtoBase
    {

        public string TituloWeb { get; set; }
        public bool Activo { get; set; }

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

        //Files
        public TenantResourceDto LogoLoginLightFile { get; set; }
        public TenantResourceDto LogoLoginDarkFile { get; set; }
        public TenantResourceDto LogoHeaderLightFile { get; set; }
        public TenantResourceDto LogoHeaderDarkFile { get; set; }
        public TenantResourceDto FaviconFile { get; set; }
        public TenantResourceDto ImagenFondoLoginLightFile { get; set; }
        public TenantResourceDto ImagenFondoLoginDarkFile { get; set; }
        public byte[] StyleSheetFile { get; set; }

        //Layout
        public int TipoLayoutLogin { get; set; }
    }
}
