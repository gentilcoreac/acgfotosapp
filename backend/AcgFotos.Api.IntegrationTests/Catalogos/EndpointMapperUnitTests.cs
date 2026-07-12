using System.Collections.Generic;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Routing;
using AcgFotos.Core.Security;
using Xunit;

// Controller de juguete en un namespace que termina en .Controllers.Api para ejercitar la derivación
// de ModuleName (ns sin el sufijo). No se registra ni se rutea: solo provee Type/MethodInfo al descriptor.
namespace AcgFotos.Api.IntegrationTests.Fake.Controllers.Api
{
    public class FooController
    {
        public void Bar() { }
    }
}

namespace AcgFotos.Api.IntegrationTests.Catalogos
{
    /// <summary>
    /// Endpoints (250) — unit puro de <see cref="EndpointDescriptorMapper"/> (ADR-0004): el ÚNICO punto que
    /// traduce un ControllerActionDescriptor a los 6 campos del endpoint y arma su firma. Discovery (catálogo)
    /// y autorización (firma del request) salen de acá → el match es por construcción, sin drift. END-46/47/48.
    /// </summary>
    public class EndpointMapperUnitTests
    {
        private static ControllerActionDescriptor Descriptor(string template, string? httpMethod)
        {
            var constraints = httpMethod == null
                ? new List<IActionConstraintMetadata>()
                : new List<IActionConstraintMetadata> { new HttpMethodActionConstraint(new[] { httpMethod }) };

            return new ControllerActionDescriptor
            {
                ControllerTypeInfo = typeof(Fake.Controllers.Api.FooController).GetTypeInfo(),
                MethodInfo = typeof(Fake.Controllers.Api.FooController).GetMethod(nameof(Fake.Controllers.Api.FooController.Bar))!,
                AttributeRouteInfo = new AttributeRouteInfo { Template = template },
                ActionConstraints = constraints,
            };
        }

        [Fact] // END-46 — ToDto extrae los 6 campos del descriptor; ModuleName = namespace SIN ".Controllers.Api"
        public void ToDto_extrae_los_seis_campos()
        {
            var dto = EndpointDescriptorMapper.ToDto(Descriptor("api/general/foo", "POST"));

            Assert.Equal("Bar", dto.ActionName);
            Assert.Equal("FooController", dto.ControllerName);
            Assert.Equal("api/general/foo", dto.Route);
            Assert.Equal("POST", dto.HttpMethod);
            Assert.EndsWith(".Fake.Controllers.Api", dto.Namespace);
            Assert.DoesNotContain(".Controllers.Api", dto.ModuleName); // se le quitó el sufijo
            Assert.EndsWith(".Fake", dto.ModuleName);
        }

        [Fact] // END-47 — sin HttpMethodActionConstraint, el verbo cae a "GET" (fallback)
        public void ToDto_sin_constraint_default_get()
        {
            var dto = EndpointDescriptorMapper.ToDto(Descriptor("api/general/foo", httpMethod: null));

            Assert.Equal("GET", dto.HttpMethod);
        }

        [Fact] // END-48 — Signature es estable y determinística: el mismo dto da la misma firma; un cambio la altera
        public void Signature_estable_y_discriminante()
        {
            var dto1 = EndpointDescriptorMapper.ToDto(Descriptor("api/general/foo", "POST"));
            var dto2 = EndpointDescriptorMapper.ToDto(Descriptor("api/general/foo", "POST"));
            var distinto = EndpointDescriptorMapper.ToDto(Descriptor("api/general/foo", "GET")); // solo cambia el verbo

            Assert.Equal(EndpointDescriptorMapper.Signature(dto1), EndpointDescriptorMapper.Signature(dto2)); // igual → misma firma
            Assert.NotEqual(EndpointDescriptorMapper.Signature(dto1), EndpointDescriptorMapper.Signature(distinto)); // distinto verbo → distinta firma
        }
    }
}
