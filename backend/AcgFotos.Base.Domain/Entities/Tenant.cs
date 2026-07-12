using AcgFotos.Core.Domain;
using System.Collections.Generic;

namespace AcgFotos.Base.Domain.Entities
{
    public class Tenant : EntityBase
    {
        public string Codigo { get; set; }
        public string Nombre { get; set; }
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

        //Layout
        public int TipoLayoutLogin { get; set; }

        //HostName
        public string HostName { get; set; }

        //Manejo de estado en su creación - Errores
        public bool? HasError { get; set; }
        public string ErrorDescription { get; set; }

        //Aplicaciones del Tenant
        public ICollection<TenantAplicacion> TenantAplicaciones { get; set; }

        //Licencias del tenant
        public ICollection<TenantLicencia> TenantLicenses { get; set; }

        public Tenant()
        {
            this.TenantAplicaciones = new List<TenantAplicacion>();
            this.TenantLicenses = new List<TenantLicencia>();
        }
    }
}
