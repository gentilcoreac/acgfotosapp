using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;
using System;
using System.Collections.Generic;

namespace AcgFotos.Api.Helpers
{
    public class SwaggerHelper
    {
        public static void ConfigureService(IServiceCollection service,
                                            IConfiguration configuration)
        {

            var companyName = configuration.GetValue<string>("CompanyName");
            var companyWebSite = configuration.GetValue<string>("CompanyWebSite");

            // https://docs.microsoft.com/en-us/aspnet/core/tutorials/getting-started-with-swashbuckle?view=aspnetcore-2.2&tabs=visual-studio
            // Register the Swagger generator, defining 1 or more Swagger documents

            service.AddSwaggerGen(c => {
                // Documenta 400/500 (ApiErrorResponse) en todos los endpoints sin pisar el 200 inferido.
                c.OperationFilter<ApiErrorResponsesOperationFilter>();

                // Microsoft.OpenApi 2.x (Swashbuckle 9+): security definition tipo Http+bearer.
                // Antes era ApiKey; con Http+bearer, SwaggerUI agrega el prefijo "Bearer " al header automáticamente
                // (el usuario sólo pega el token al hacer Authorize).
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme()
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    Description = "JWT Authorization. Pegá sólo el token (sin el prefijo 'Bearer').",
                });

                // El lambda recibe el OpenApiDocument host: pasárselo a OpenApiSecuritySchemeReference
                // hace que la referencia resuelva correctamente al security scheme registrado arriba.
                // Sin el doc, el $ref del swagger.json queda roto y SwaggerUI no inyecta el header → 401.
                c.AddSecurityRequirement(doc => new OpenApiSecurityRequirement
                {
                    {
                          new OpenApiSecuritySchemeReference("Bearer", doc),
                          new List<string>()
                    }
                });

                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = companyName + " API",
                    Version = "Versión 1",
                    Description = companyName + " API",
                    Contact = new OpenApiContact
                    {
                        Name = companyName,
                        // CompanyWebSite puede quedar vacío en config: new Uri("") revienta la activación
                        // de Swagger (y con ella cualquier request del host). Sin sitio => sin Url.
                        Url = string.IsNullOrWhiteSpace(companyWebSite) ? null : new Uri(companyWebSite)
                    }
                });
            });
        }

        public static void UseSwagger(IApplicationBuilder app)
        {
            // Enable middleware to serve generated Swagger as a JSON endpoint.
            app.UseSwagger();
            var id = Guid.NewGuid();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("swagger/v1/swagger.json", "Web API V1");
                c.DocumentTitle = "Documentación API";
                c.DefaultModelsExpandDepth(-1);

                c.InjectStylesheet("css/custom-ui.css");

                c.InjectJavascript("js/custom-ui.js", "text/javascript");

                c.InjectJavascript("js/jquery.js");
                c.InjectJavascript("js/auth.js?" + id);
                c.RoutePrefix = string.Empty;
            });
        }
    }
}
