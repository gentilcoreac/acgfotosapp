using System.Collections.Generic;

namespace AcgFotos.Base.Application.Dtos
{
    public class TenantDto : TenantHeaderStyleDto
    {
        public bool? HasError { get; set; }
        public string ErrorDescription { get; set; }
        /// STYLEs del ABM
        public TenantResourceDto LogoLoginLightFile { get; set; }
        public TenantResourceDto LogoLoginDarkFile { get; set; }
        public TenantResourceDto LogoHeaderLightFile { get; set; }
        public TenantResourceDto LogoHeaderDarkFile { get; set; }
        public TenantResourceDto FaviconFile { get; set; }
        public TenantResourceDto ImagenFondoLoginLightFile { get; set; }
        public TenantResourceDto ImagenFondoLoginDarkFile { get; set; }
        public byte[] StyleSheetFile { get; set; }

        /// BUSINESS
        public UsuarioDto Usuario { get; set; }
        public ICollection<TenantAplicacionDto> TenantAplicaciones { get; set; }
        public ICollection<TenantLicenciaDto> TenantLicenses { get; set; }

        public TenantDto()
        {
            this.TenantAplicaciones = new List<TenantAplicacionDto>();
            this.TenantLicenses = new List<TenantLicenciaDto>();
        }
    }
}
