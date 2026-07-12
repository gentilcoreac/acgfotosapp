using System.Collections.Generic;
using AcgFotos.Core.Application;

namespace AcgFotos.Base.Application.Dtos
{
    /// <summary>
    /// Shape de input para Update/Create de Tenant. NO incluye campos que el cliente
    /// no debe poder setear directamente:
    ///   - HasError / ErrorDescription: los maneja el sistema durante la creación del tenant.
    /// Estos campos solo se actualizan por flujos internos del backend, no por el body de un PUT.
    /// </summary>
    public class TenantInputDto : DtoBase
    {
        public string Codigo { get; set; }
        public string Nombre { get; set; }
        public string TituloWeb { get; set; }
        public bool Activo { get; set; }
        public string HostName { get; set; }

        // Estilo
        public string ColorPrimarioDark { get; set; }
        public string ColorPrimarioLight { get; set; }
        public bool DarkModeByDefault { get; set; }
        public string LogoLoginLightUrl { get; set; }
        public string LogoLoginDarkUrl { get; set; }
        public string LogoHeaderLightUrl { get; set; }
        public string LogoHeaderDarkUrl { get; set; }
        public string FaviconUrl { get; set; }
        public string ImagenFondoLoginLightUrl { get; set; }
        public string ImagenFondoLoginDarkUrl { get; set; }
        public string StyleSheetCssUrl { get; set; }
        public int TipoLayoutLogin { get; set; }

        // Files
        public TenantResourceDto LogoLoginLightFile { get; set; }
        public TenantResourceDto LogoLoginDarkFile { get; set; }
        public TenantResourceDto LogoHeaderLightFile { get; set; }
        public TenantResourceDto LogoHeaderDarkFile { get; set; }
        public TenantResourceDto FaviconFile { get; set; }
        public TenantResourceDto ImagenFondoLoginLightFile { get; set; }
        public TenantResourceDto ImagenFondoLoginDarkFile { get; set; }
        public byte[] StyleSheetFile { get; set; }

        // Business
        public UsuarioDto Usuario { get; set; }
        public ICollection<TenantAplicacionDto> TenantAplicaciones { get; set; }
        public ICollection<TenantLicenciaDto> TenantLicenses { get; set; }

        public TenantInputDto()
        {
            this.TenantAplicaciones = new List<TenantAplicacionDto>();
            this.TenantLicenses = new List<TenantLicenciaDto>();
        }
    }
}
