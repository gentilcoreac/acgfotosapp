using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.Controllers;
using System.Linq;

namespace AcgFotos.Core.Security
{
    /// <summary>
    /// Único punto que traduce un ControllerActionDescriptor (la verdad de ruteo de ASP.NET) a los
    /// 6 campos que identifican un endpoint en gen_Endpoints, y que arma su firma estable.
    /// Lo usan TANTO el discovery (DiscoveryEndpoints → catálogo) como la autorización
    /// (EndpointAuthoritation.IsGranted → firma del request actual): al salir ambos del mismo
    /// descriptor y del mismo mapeo, el match es por construcción — no puede haber drift entre cómo
    /// se registra un endpoint y cómo se valida en runtime (la clase de bug que con el scan por
    /// reflexión dejaba 401 en rutas embebidas tipo [HttpGet("ruta")]). Ver ADR-0004.
    /// </summary>
    public static class EndpointDescriptorMapper
    {
        private const string ModuleNamespaceSuffix = ".Controllers.Api";

        // Separador improbable en rutas/nombres para armar la firma de un endpoint.
        private const char SignatureSeparator = '\u0001';

        public static EndpointDto ToDto(ControllerActionDescriptor descriptor)
        {
            var ns = descriptor.ControllerTypeInfo.Namespace ?? string.Empty;

            return new EndpointDto
            {
                ActionName = descriptor.MethodInfo.Name,
                ControllerName = descriptor.ControllerTypeInfo.Name,
                ModuleName = ns.Replace(ModuleNamespaceSuffix, string.Empty),
                Namespace = ns,
                Route = descriptor.AttributeRouteInfo?.Template,
                HttpMethod = GetHttpMethod(descriptor)
            };
        }

        /// <summary>
        /// Firma estable de un endpoint = los 6 campos que lo identifican unidos por un separador
        /// improbable. Permite chequear permiso con un HashSet (O(1)) en vez de comparar campo a campo.
        /// </summary>
        public static string Signature(EndpointDto endpoint) =>
            string.Join(SignatureSeparator,
                        endpoint.HttpMethod,
                        endpoint.Route,
                        endpoint.ActionName,
                        endpoint.ControllerName,
                        endpoint.ModuleName,
                        endpoint.Namespace);

        private static string GetHttpMethod(ControllerActionDescriptor descriptor)
        {
            var constraint = descriptor.ActionConstraints?
                .OfType<HttpMethodActionConstraint>()
                .FirstOrDefault();

            return constraint?.HttpMethods?.FirstOrDefault() ?? "GET";
        }
    }
}
