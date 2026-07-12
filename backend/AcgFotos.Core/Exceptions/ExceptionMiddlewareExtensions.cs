using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using System.Net;
using System.Text.Json;
using AcgFotos.Core.Controllers;
using AcgFotos.Core.Localization.APIResources;

namespace AcgFotos.Core.ExtensionMethods
{
    public static class ExceptionMiddlewareExtensions
    {
        public static void ConfigureExceptionHandler(this IApplicationBuilder app)
        {
            app.UseExceptionHandler(appError =>
            {
                appError.Run(async context =>
                {
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    context.Response.ContentType = "application/json";

                    var contextFeature = context.Features.Get<IExceptionHandlerFeature>();

                    if (contextFeature != null)
                    {
                        // Misma forma estándar que el ExceptionHandlingMiddleware: { message, errors, traceId }.
                        // Este handler es el fallback del pipeline temprano (errores antes de llegar al
                        // middleware principal); mantenerlo consistente evita que el front reciba otra shape.
                        await context.Response.WriteAsync(JsonSerializer.Serialize(
                            ApiErrorResponse.Technical(MessagesAPI.ErrorGenericInternal, context.TraceIdentifier)));
                    }
                });
            });
        }
    }
}
