using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Configuration;
using AcgFotos.Core.Controllers;
using AcgFotos.Core.ExtensionMethods;
using AcgFotos.Core.Localization;
using AcgFotos.Core.Security;
using AcgFotos.Core.Session;
using Newtonsoft.Json;

namespace AcgFotos.Api.Controllers
{
    [ApiController]
    [Route("api/general")]

    public class HomeController : ApiControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ISecurityRepository _securityRepository;
        private readonly ILocalizationResourcesManager _localizationResourcesManager;
        private readonly IAppContext _appContext;
        private readonly IActionDescriptorCollectionProvider _actionDescriptorProvider;

        public HomeController(
            IConfiguration configuration,
            ISecurityRepository securityRepository,
            IAppContext appContext,
            ILocalizationResourcesManager localizationResourcesManager,
            IActionDescriptorCollectionProvider actionDescriptorProvider)
        {
            _configuration = configuration;
            _securityRepository = securityRepository;
            _localizationResourcesManager = localizationResourcesManager;
            _appContext = appContext;
            _actionDescriptorProvider = actionDescriptorProvider;
        }

        [HttpGet]
        [AllowAnonymous]
        [Route("version")]
        public IActionResult Get()
        {
            var apiVersionConfig = _configuration.GetSection("ApiVersion").Get<ApiVersionConfig>();

            return this.Ok(new
            {
                ApiVersion = apiVersionConfig.GetApiVersion(),
                apiVersionConfig.Major,
                apiVersionConfig.Minor,
                apiVersionConfig.Patch,
                apiVersionConfig.IsTestEnviroment
            });
        }

        [HttpGet]
        [AllowAnonymous]
        [Route("GetLocalizationByCultureInfoName")]
        public ActionResult<LocalizationResourcesDto> GetLocalizationByCultureInfoName(string cultureInfo)
        {
            if (string.IsNullOrEmpty(cultureInfo))
                cultureInfo = _configuration.GetValue<string>("CultureInfoNameDefault");

            var includePrivateResources = _appContext.UserId != 0;

            bool isRoot = _appContext.IsRoot;

            long appId = _appContext.AplicacionId != null ? (long)_appContext.AplicacionId : 0;

            var localizationResources = _localizationResourcesManager.GetAllResourcesByCultureInfo(cultureInfo, appId, isRoot, includePrivateResources);

            var localResourcesDto = new LocalizationResourcesDto(localizationResources);

            return this.Ok(localResourcesDto);
        }

        [Route("discover")]
        [HttpGet]
        public IActionResult Discover()
        {
            // Repuebla el catálogo completo de gen_Endpoints: sólo root cuando la auth está activa.
            // (Sin auth — dev local sin tokens — se deja abierto para poder poblar el catálogo.)
            var authEnabled = _configuration.GetValue<bool>("AuthenticationEnabled");
            if (authEnabled && !_appContext.IsRoot)
            {
                return this.Unauthorized();
            }

            var endpoints = DiscoveryEndpoints.Discover(_configuration, _actionDescriptorProvider);
            _securityRepository.RegisterEndpoints(endpoints);

            return this.Ok(endpoints);
        }
    }
}
