using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Configuration;
using AcgFotos.Core.Security;
using System.Collections.Generic;
using System.Linq;

namespace AcgFotos.Core.Controllers
{
    /// <summary>
    /// Arma el catálogo de endpoints (gen_Endpoints) desde IActionDescriptorCollectionProvider: los
    /// descriptores REALES con los que ASP.NET rutea, con el template ya compuesto (incluye rutas
    /// embebidas en [HttpVerb("...")], rutas absolutas, tokens y las acciones heredadas de los
    /// controllers base genéricos). Reemplaza al scan por reflexión, que re-armaba las rutas a mano
    /// y podía divergir del template real → endpoints registrados que nunca matcheaban contra
    /// EndpointAuthoritation (401 silencioso). Ver ADR-0004.
    /// Se limita a los módulos de AppModulesName (namespace *.Controllers.Api), igual que el scan viejo.
    /// </summary>
    public class DiscoveryEndpoints
    {
        public static List<EndpointDto> Discover(
            IConfiguration configuration,
            IActionDescriptorCollectionProvider actionDescriptorProvider)
        {
            var moduleNamespaces = configuration.GetSection("AppModulesName").Value
                .Split(";")
                .Select(module => module + ".Controllers.Api")
                .ToList();

            var endpoints = new List<EndpointDto>();
            var seenSignatures = new HashSet<string>();

            var descriptors = actionDescriptorProvider.ActionDescriptors.Items
                .OfType<ControllerActionDescriptor>();

            foreach (var descriptor in descriptors)
            {
                var controllerNamespace = descriptor.ControllerTypeInfo.Namespace;
                if (controllerNamespace == null ||
                    !moduleNamespaces.Any(ns => controllerNamespace.Contains(ns)))
                {
                    continue;
                }

                var endpoint = EndpointDescriptorMapper.ToDto(descriptor);

                // Los módulos rutean sólo por atributos; un descriptor sin template (ruteo
                // convencional) no es alcanzable por la autorización y no se cataloga.
                if (string.IsNullOrEmpty(endpoint.Route))
                {
                    continue;
                }

                if (seenSignatures.Add(EndpointDescriptorMapper.Signature(endpoint)))
                {
                    endpoints.Add(endpoint);
                }
            }

            return endpoints;
        }
    }
}
