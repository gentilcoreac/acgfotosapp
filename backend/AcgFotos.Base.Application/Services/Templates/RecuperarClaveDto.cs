using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using AcgFotos.Core.Email;

namespace AcgFotos.Base.Application.Services.Templates
{
    public class RecuperarClaveDto(IConfiguration configuration, IHttpContextAccessor httpContextAccessor, string subject, string tenantName, string tenantColor, string tenantLogoUrl)
        : GeneralEmailModel(configuration, httpContextAccessor, subject, tenantName, tenantColor, tenantLogoUrl)
    {
        public string Url { get; set; }
        public string BodyMsg { get; set; }
        public string ButtonAction { get; set; }
    }
}
