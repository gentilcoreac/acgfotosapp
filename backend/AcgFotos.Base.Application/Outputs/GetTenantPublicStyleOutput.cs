namespace AcgFotos.Base.Application.Outputs
{
    public class GetTenantPublicStyleOutput
    {
        public string Codigo { get; set; }
        public string TituloWeb { get; set; }
        public string HostName { get; set; }

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

        /*Prop. para Office*/
        public string ColorHeaderSheet { get; set; }
        public string ColorFontHeaderSheet { get; set; }
        public string LogoHeaderSheetUrl { get; set; }
        public string ColorHeaderTable { get; set; }
        public string ColorHeaderFontTable { get; set; }
    }
}
