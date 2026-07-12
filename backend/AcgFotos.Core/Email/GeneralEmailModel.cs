using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using AcgFotos.Core.Localization.APIResources;

namespace AcgFotos.Core.Email
{
    public class GeneralEmailModel
    {
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public string CompanyName { get; set; }
        public string CompanyWebSite { get; set; }
        public string CompanyLogoUrl { get; set; }
        public string Year { get; set; }
        public string InfoSupport { get; set; }
        public string Subject { get; set; }
        public string TenantName { get; set; }
        public string TenantColor { get; set; }
        public string TenantLogoUrl { get; set; }

        public GeneralEmailModel(
            IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor, 
            string subject, 
            string tenantName, 
            string tenantColor, 
            string tenantLogoUrl)
        {
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
            this.InitConfig(subject, tenantName, tenantColor, tenantLogoUrl);
        }

        public void InitConfig(string subject, string tenantName, string tenantColor, string tenantLogoUrl)
        {
            this.CompanyName = _configuration.GetValue<string>("CompanyName");
            this.CompanyWebSite = _configuration.GetValue<string>("CompanyWebSite");

            this.CompanyLogoUrl = $"{this.GetHttpScheme()}://{this.GetHttpHost()}/{_configuration.GetValue<string>("CompanyLogo")}";
            var tenantDefaultLogo = $"{this.GetHttpScheme()}://{this.GetHttpHost()}/{_configuration.GetValue<string>("tenatDefaultLogo")}";

            this.Year = _configuration.GetValue<string>("Year");
            this.InfoSupport = MessagesAPI.EmailInfoSupport;
            this.TenantName = tenantName;
            this.TenantColor = tenantColor != null ? tenantColor : _configuration.GetValue<string>("tenatDefaultColor");
            this.TenantLogoUrl = tenantLogoUrl != null ? tenantLogoUrl : tenantDefaultLogo;
            this.Subject = subject;
        }

        private string GetHttpScheme()
        {
            return _httpContextAccessor.HttpContext.Request.Scheme;
        }

        private string GetHttpHost()
        {
            return _httpContextAccessor.HttpContext.Request.Host.Value;
        }
    }
}
