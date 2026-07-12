using System.Collections.Generic;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using AcgFotos.Core.Controllers;

namespace AcgFotos.Api.Helpers
{
    /// <summary>
    /// Documenta en CADA operación las respuestas de error estándar (400 y 500 con
    /// <see cref="ApiErrorResponse"/>) que produce el <c>ExceptionHandlingMiddleware</c>, sin pisar la
    /// respuesta de éxito.
    /// <para>
    /// Se hace por OperationFilter (no con <c>[ProducesResponseType]</c> en la base): los controllers no
    /// tienen <c>[ApiController]</c> y Swagger infiere el 200 del tipo de retorno; declarar respuestas
    /// explícitas en la base suprimía ese 200 inferido. El filter corre después de armada la operación,
    /// así que solo agrega 400/500 si no estaban y conserva el 200 y demás respuestas propias.
    /// </para>
    /// </summary>
    public class ApiErrorResponsesOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var schema = context.SchemaGenerator.GenerateSchema(typeof(ApiErrorResponse), context.SchemaRepository);
            AddIfMissing(operation, "400", "Bad Request", schema);
            AddIfMissing(operation, "500", "Internal Server Error", schema);
        }

        private static void AddIfMissing(OpenApiOperation operation, string code, string description, IOpenApiSchema schema)
        {
            if (operation.Responses.ContainsKey(code))
            {
                return;
            }

            operation.Responses[code] = new OpenApiResponse
            {
                Description = description,
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    ["application/json"] = new OpenApiMediaType { Schema = schema },
                },
            };
        }
    }
}
